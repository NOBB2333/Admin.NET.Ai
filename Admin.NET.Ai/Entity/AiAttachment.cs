namespace Admin.NET.Ai.Models;

/// <summary>
/// AI 文件/附件 (用于多模态输入)
/// </summary>
public class AiAttachment
{
    /// <summary>
    /// 文件名
    /// </summary>
    public string? FileName { get; set; }

    /// <summary>
    /// 文件类型 (MIME type)
    /// e.g. "image/png", "audio/wav"
    /// </summary>
    public string? MediaType { get; set; }

    /// <summary>
    /// 文件流 (优先使用)
    /// </summary>
    public Stream? Stream { get; set; }

    /// <summary>
    /// 文件字节 (如果流不可用)
    /// </summary>
    public byte[]? Bytes { get; set; }

    /// <summary>
    /// 文件 URL (如果是远程文件)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// 构造函数 (流)
    /// </summary>
    public AiAttachment(Stream stream, string? fileName = null, string? mediaType = null)
    {
        Stream = stream;
        FileName = fileName;
        MediaType = mediaType;
    }

    /// <summary>
    /// 构造函数 (字节)
    /// </summary>
    public AiAttachment(byte[] bytes, string? fileName = null, string? mediaType = null)
    {
        Bytes = bytes;
        FileName = fileName;
        MediaType = mediaType;
    }

    /// <summary>
    /// 构造函数 (URL)
    /// </summary>
    public AiAttachment(string url, string? mediaType = null)
    {
        Url = url;
        MediaType = mediaType;
    }
}
