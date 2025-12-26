using Microsoft.AspNetCore.Http;

namespace HeMaCupAICheck;

/// <summary>
/// Null implementation of IHttpContextAccessor for console apps
/// </summary>
public class NullHttpContextAccessor : IHttpContextAccessor
{
    public HttpContext? HttpContext { get; set; } = null;
}
