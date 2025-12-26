using Admin.NET.Ai.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace Admin.NET.Ai.Services.Rag;

/// <summary>
/// 文档分块器实现
/// 支持多种分块策略
/// </summary>
public class DocumentChunker : IDocumentChunker
{
    private readonly ILogger<DocumentChunker> _logger;

    public DocumentChunker(ILogger<DocumentChunker> logger)
    {
        _logger = logger;
    }

    public IEnumerable<DocumentChunk> ChunkDocument(string content, ChunkingOptions? options = null)
    {
        var doc = new RawDocument { Content = content };
        return ChunkDocuments(new[] { doc }, options);
    }

    public IEnumerable<DocumentChunk> ChunkDocuments(IEnumerable<RawDocument> documents, ChunkingOptions? options = null)
    {
        options ??= new ChunkingOptions();
        
        foreach (var doc in documents)
        {
            var chunks = options.Strategy switch
            {
                ChunkingStrategy.FixedSize => ChunkByFixedSize(doc, options),
                ChunkingStrategy.SentenceBoundary => ChunkBySentence(doc, options),
                ChunkingStrategy.Paragraph => ChunkByParagraph(doc, options),
                ChunkingStrategy.Semantic => ChunkByFixedSize(doc, options), // 简化: 语义分块回退到固定大小
                _ => ChunkByFixedSize(doc, options)
            };

            foreach (var chunk in chunks)
            {
                yield return chunk;
            }
        }
    }

    /// <summary>
    /// 固定大小分块
    /// </summary>
    private IEnumerable<DocumentChunk> ChunkByFixedSize(RawDocument doc, ChunkingOptions options)
    {
        var content = doc.Content;
        var chunkSize = options.MaxChunkSize;
        var overlap = options.Overlap;
        var chunkIndex = 0;

        for (int i = 0; i < content.Length; i += (chunkSize - overlap))
        {
            var length = Math.Min(chunkSize, content.Length - i);
            var chunkContent = content.Substring(i, length);

            yield return CreateChunk(doc, chunkContent, chunkIndex++, i, i + length, options);

            if (i + length >= content.Length) break;
        }

        _logger.LogDebug("📄 [Chunker] 固定大小分块完成: {DocId}, 块数: {Count}", doc.Id, chunkIndex);
    }

    /// <summary>
    /// 按句子边界分块
    /// </summary>
    private IEnumerable<DocumentChunk> ChunkBySentence(RawDocument doc, ChunkingOptions options)
    {
        // 句子分割正则 (中英文)
        var sentencePattern = new Regex(@"(?<=[。！？.!?])\s*", RegexOptions.Compiled);
        var sentences = sentencePattern.Split(doc.Content).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();

        var currentChunk = new List<string>();
        var currentLength = 0;
        var chunkIndex = 0;
        var startPos = 0;

        foreach (var sentence in sentences)
        {
            if (currentLength + sentence.Length > options.MaxChunkSize && currentChunk.Count > 0)
            {
                // 输出当前块
                var chunkContent = string.Join(" ", currentChunk);
                yield return CreateChunk(doc, chunkContent, chunkIndex++, startPos, startPos + chunkContent.Length, options);
                
                // 处理重叠
                if (options.Overlap > 0 && currentChunk.Count > 0)
                {
                    var lastSentence = currentChunk.Last();
                    currentChunk.Clear();
                    currentChunk.Add(lastSentence);
                    currentLength = lastSentence.Length;
                }
                else
                {
                    currentChunk.Clear();
                    currentLength = 0;
                }
                startPos += chunkContent.Length;
            }

            currentChunk.Add(sentence);
            currentLength += sentence.Length;
        }

        // 最后一块
        if (currentChunk.Count > 0)
        {
            var chunkContent = string.Join(" ", currentChunk);
            yield return CreateChunk(doc, chunkContent, chunkIndex, startPos, startPos + chunkContent.Length, options);
        }

        _logger.LogDebug("📄 [Chunker] 句子边界分块完成: {DocId}, 块数: {Count}", doc.Id, chunkIndex + 1);
    }

    /// <summary>
    /// 按段落分块
    /// </summary>
    private IEnumerable<DocumentChunk> ChunkByParagraph(RawDocument doc, ChunkingOptions options)
    {
        var paragraphs = doc.Content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);
        var chunkIndex = 0;
        var position = 0;

        var currentChunk = new List<string>();
        var currentLength = 0;

        foreach (var para in paragraphs)
        {
            var trimmed = para.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (currentLength + trimmed.Length > options.MaxChunkSize && currentChunk.Count > 0)
            {
                // 输出当前块
                var chunkContent = string.Join("\n\n", currentChunk);
                yield return CreateChunk(doc, chunkContent, chunkIndex++, position, position + chunkContent.Length, options);
                position += chunkContent.Length;
                currentChunk.Clear();
                currentLength = 0;
            }

            currentChunk.Add(trimmed);
            currentLength += trimmed.Length;
        }

        // 最后一块
        if (currentChunk.Count > 0)
        {
            var chunkContent = string.Join("\n\n", currentChunk);
            yield return CreateChunk(doc, chunkContent, chunkIndex, position, position + chunkContent.Length, options);
        }

        _logger.LogDebug("📄 [Chunker] 段落分块完成: {DocId}, 块数: {Count}", doc.Id, chunkIndex + 1);
    }

    private DocumentChunk CreateChunk(RawDocument doc, string content, int index, int start, int end, ChunkingOptions options)
    {
        var chunk = new DocumentChunk
        {
            DocumentId = doc.Id,
            Content = content,
            ChunkIndex = index,
            StartPosition = start,
            EndPosition = end,
            SourceName = doc.SourceName,
            SourceUri = doc.SourceUri
        };

        if (options.PreserveMetadata)
        {
            foreach (var kv in doc.Metadata)
            {
                chunk.Metadata[kv.Key] = kv.Value;
            }
            chunk.Metadata["DocumentId"] = doc.Id;
            chunk.Metadata["ChunkIndex"] = index;
        }

        return chunk;
    }
}
