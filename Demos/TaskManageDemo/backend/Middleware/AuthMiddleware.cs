// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Security.Claims;
using System.Text.Json;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services.Auth;

namespace TaskManageDemo.Backend.Middleware;

/// <summary>
/// 认证中间件
/// </summary>
public class AuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthenticationMiddleware> _logger;

    private static readonly HashSet<string> PublicPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/health",
        "/swagger",
        "/api/auth/login",
        "/api/auth/register",
        "/api/auth/feishu/url",
        "/api/auth/feishu/callback",
        "/api/auth/feishu/check",
        "/webhook/feishu"
    };

    public AuthenticationMiddleware(RequestDelegate next, ILogger<AuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        if (IsPublicPath(path))
        {
            await _next(context);
            return;
        }

        // 如果用户已经通过 JWT Bearer 认证，直接放行
        // JWT 认证由 ASP.NET Core 的 JWT Bearer 中间件处理
        if (context.User?.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // 仅处理非 JWT 认证的情况（如 X-Feishu-Id header）
        var feishuId = ExtractFeishuId(context);
        if (string.IsNullOrEmpty(feishuId))
        {
            await WriteUnauthorizedResponse(context, "未提供认证信息");
            return;
        }

        var userInfo = await authService.GetUserByFeishuIdAsync(feishuId);
        if (userInfo == null)
        {
            await WriteUnauthorizedResponse(context, "用户不存在");
            return;
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userInfo.UserId),
            new(ClaimTypes.Name, userInfo.UserName),
            new("FeishuId", userInfo.FeishuId),
            new(ClaimTypes.Role, userInfo.Role)
        };

        foreach (var permission in userInfo.Permissions)
        {
            claims.Add(new Claim("Permission", permission));
        }

        var identity = new ClaimsIdentity(claims, "FeishuAuth");
        context.User = new ClaimsPrincipal(identity);

        context.Items["UserInfo"] = userInfo;

        await _next(context);
    }

    private static bool IsPublicPath(string path)
    {
        foreach (var publicPath in PublicPaths)
        {
            if (path.StartsWith(publicPath, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private static string? ExtractFeishuId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Feishu-Id", out var feishuIdHeader))
        {
            return feishuIdHeader.ToString();
        }

        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authValue = authHeader.ToString();
            if (authValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return authValue.Substring(7);
            }
        }

        if (context.Request.Query.TryGetValue("feishu_id", out var feishuIdQuery))
        {
            return feishuIdQuery.ToString();
        }

        return null;
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// 授权中间件
/// </summary>
public class AuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthorizationMiddleware> _logger;

    public AuthorizationMiddleware(RequestDelegate next, ILogger<AuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // 检查角色要求
        var requireRoleAttributes = endpoint.Metadata
            .GetOrderedMetadata<RequireRoleAttribute>();

        if (requireRoleAttributes != null && requireRoleAttributes.Count > 0)
        {
            var userRoles = context.User.Claims
                .Where(c => c.Type == ClaimTypes.Role)
                .Select(c => c.Value)
                .ToHashSet();

            var requiredRoles = requireRoleAttributes
                .SelectMany(a => a.Roles)
                .ToHashSet();

            if (requiredRoles.Count > 0 && !requiredRoles.Any(r => userRoles.Contains(r)))
            {
                _logger.LogWarning("用户 {UserId} 缺少所需角色: {Roles}",
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    string.Join(", ", requiredRoles));

                await WriteForbiddenResponse(context, "权限不足：缺少所需角色");
                return;
            }
        }

        // 检查权限要求
        var requirePermissionAttributes = endpoint.Metadata
            .GetOrderedMetadata<RequirePermissionAttribute>();

        if (requirePermissionAttributes != null && requirePermissionAttributes.Count > 0)
        {
            // 同时检查 "permission"（JWT token）和 "Permission"（自定义认证）
            var userPermissions = context.User.Claims
                .Where(c => string.Equals(c.Type, "permission", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Value)
                .ToHashSet();

            var requiredPermissions = requirePermissionAttributes
                .SelectMany(a => a.Permissions)
                .ToHashSet();

            if (requiredPermissions.Count > 0 && !requiredPermissions.Any(p => userPermissions.Contains(p)))
            {
                _logger.LogWarning("用户 {UserId} 缺少所需权限: {Permissions}",
                    context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                    string.Join(", ", requiredPermissions));

                await WriteForbiddenResponse(context, "权限不足：缺少所需权限");
                return;
            }
        }

        await _next(context);
    }

    private static async Task WriteForbiddenResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message
        };

        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// 权限要求特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : Attribute
{
    /// <summary>
    /// 所需权限列表
    /// </summary>
    public string[] Permissions { get; }

    public RequirePermissionAttribute(params string[] permissions)
    {
        Permissions = permissions;
    }
}

/// <summary>
/// 角色要求特性
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequireRoleAttribute : Attribute
{
    /// <summary>
    /// 所需角色列表
    /// </summary>
    public string[] Roles { get; }

    public RequireRoleAttribute(params string[] roles)
    {
        Roles = roles;
    }
}

/// <summary>
/// 认证中间件扩展
/// </summary>
public static class AuthMiddlewareExtensions
{
    /// <summary>
    /// 使用认证中间件
    /// </summary>
    public static IApplicationBuilder UseFeishuAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthenticationMiddleware>();
    }

    /// <summary>
    /// 使用授权中间件
    /// </summary>
    public static IApplicationBuilder UseFeishuAuthorization(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<AuthorizationMiddleware>();
    }
}
