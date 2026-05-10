// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Middleware;

/// <summary>
/// API版本控制中间件
/// </summary>
public class ApiVersioningMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiVersioningMiddleware> _logger;

    public ApiVersioningMiddleware(RequestDelegate next, ILogger<ApiVersioningMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        var apiVersion = ExtractApiVersion(context);

        context.Items["ApiVersion"] = apiVersion;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-API-Version"] = apiVersion;
            context.Response.Headers["X-API-Supported-Versions"] = "v1";
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string ExtractApiVersion(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-API-Version", out var versionHeader))
        {
            return versionHeader.ToString();
        }

        if (context.Request.Query.TryGetValue("version", out var versionQuery))
        {
            return versionQuery.ToString();
        }

        var path = context.Request.Path.Value ?? string.Empty;
        var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length >= 2 && segments[0].Equals("api", StringComparison.OrdinalIgnoreCase))
        {
            var versionSegment = segments[1];
            if (versionSegment.StartsWith('v') && char.IsDigit(versionSegment[1]))
            {
                return versionSegment;
            }
        }

        return "v1";
    }
}

/// <summary>
/// API版本特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ApiVersionAttribute : Attribute
{
    /// <summary>
    /// 版本号
    /// </summary>
    public string Version { get; }

    /// <summary>
    /// 是否已弃用
    /// </summary>
    public bool Deprecated { get; set; }

    public ApiVersionAttribute(string version)
    {
        Version = version;
    }
}

/// <summary>
/// 版本控制中间件扩展
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// 使用API版本控制
    /// </summary>
    public static IApplicationBuilder UseApiVersioning(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ApiVersioningMiddleware>();
    }
}
