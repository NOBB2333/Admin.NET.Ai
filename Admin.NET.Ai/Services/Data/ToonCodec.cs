using ToonSharp;

namespace Admin.NET.Ai.Services.Data;

/// <summary>
/// TOON (Token-Optimized Object Notation) 编解码器
/// 集成 ToonSharp 库
/// </summary>
public static class ToonCodec
{
    // Serialize Object to TOON format
    public static string Serialize(object? obj)
    {
        if (obj == null) return "null";
        // 使用 ToonSharp 进行序列化
        return ToonSerializer.Serialize(obj);
    }
}
