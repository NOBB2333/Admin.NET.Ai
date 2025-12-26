using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Admin.NET.Ai.Services.Processing;

/// <summary>
/// 批量处理服务 - 用于高效处理大批量AI请求
/// </summary>
public class BatchProcessingService
{
    private readonly ILogger<BatchProcessingService> _logger;
    private readonly int _maxConcurrency;
    private readonly int _batchSize;

    public BatchProcessingService(ILogger<BatchProcessingService> logger, int maxConcurrency = 5, int batchSize = 10)
    {
        _logger = logger;
        _maxConcurrency = maxConcurrency;
        _batchSize = batchSize;
    }

    /// <summary>
    /// 批量处理请求 (并行执行)
    /// </summary>
    public async Task<List<BatchResult<TResult>>> ProcessBatchAsync<TResult>(
        IChatClient client,
        IEnumerable<string> prompts,
        Func<string, Task<TResult>> processor,
        CancellationToken ct = default)
    {
        var promptList = prompts.ToList();
        var results = new List<BatchResult<TResult>>();
        var semaphore = new SemaphoreSlim(_maxConcurrency);

        _logger.LogInformation("开始批量处理 {Count} 个请求, 并发数: {Concurrency}", promptList.Count, _maxConcurrency);

        var tasks = promptList.Select(async (prompt, index) =>
        {
            await semaphore.WaitAsync(ct);
            try
            {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var result = await processor(prompt);
                sw.Stop();

                return new BatchResult<TResult>
                {
                    Index = index,
                    Input = prompt,
                    Result = result,
                    Success = true,
                    ProcessingTime = sw.Elapsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量处理项 {Index} 失败", index);
                return new BatchResult<TResult>
                {
                    Index = index,
                    Input = prompt,
                    Success = false,
                    Error = ex.Message
                };
            }
            finally
            {
                semaphore.Release();
            }
        });

        var batchResults = await Task.WhenAll(tasks);
        results.AddRange(batchResults.OrderBy(r => r.Index));

        var successCount = results.Count(r => r.Success);
        _logger.LogInformation("批量处理完成: {Success}/{Total} 成功", successCount, promptList.Count);

        return results;
    }

    /// <summary>
    /// 流式批量处理 (使用Channel实现背压控制)
    /// </summary>
    public async IAsyncEnumerable<BatchResult<TResult>> ProcessBatchStreamingAsync<TResult>(
        IEnumerable<string> prompts,
        Func<string, Task<TResult>> processor,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        var channel = Channel.CreateBounded<BatchResult<TResult>>(new BoundedChannelOptions(_batchSize)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var promptList = prompts.ToList();
        var producerTask = Task.Run(async () =>
        {
            var semaphore = new SemaphoreSlim(_maxConcurrency);
            var tasks = new List<Task>();

            foreach (var (prompt, index) in promptList.Select((p, i) => (p, i)))
            {
                await semaphore.WaitAsync(ct);

                var task = Task.Run(async () =>
                {
                    try
                    {
                        var result = await processor(prompt);
                        await channel.Writer.WriteAsync(new BatchResult<TResult>
                        {
                            Index = index,
                            Input = prompt,
                            Result = result,
                            Success = true
                        }, ct);
                    }
                    catch (Exception ex)
                    {
                        await channel.Writer.WriteAsync(new BatchResult<TResult>
                        {
                            Index = index,
                            Input = prompt,
                            Success = false,
                            Error = ex.Message
                        }, ct);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, ct);

                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
            channel.Writer.Complete();
        }, ct);

        await foreach (var result in channel.Reader.ReadAllAsync(ct))
        {
            yield return result;
        }

        await producerTask;
    }
}

/// <summary>
/// 批量处理结果
/// </summary>
public class BatchResult<T>
{
    public int Index { get; set; }
    public string Input { get; set; } = string.Empty;
    public T? Result { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public TimeSpan ProcessingTime { get; set; }
}
