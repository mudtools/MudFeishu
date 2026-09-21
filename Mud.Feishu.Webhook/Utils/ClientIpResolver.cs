// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Utils;

/// <summary>
/// 客户端 IP 解析器（ADR-3 零信任模型）。
/// <para>从 <c>FeishuRateLimitMiddleware.GetClientIp</c> 下沉，供限流中间件和白名单中间件共享。</para>
/// <para>默认不信任任何转发头，仅在直连 IP 命中 TrustedProxies 时解析 X-Forwarded-For。</para>
/// </summary>
internal static class ClientIpResolver
{
    /// <summary>
    /// 获取客户端真实 IP（ADR-3 零信任模型）。
    /// 默认不信任任何转发头，仅在直连 IP 命中 TrustedProxies 时解析 X-Forwarded-For。
    /// </summary>
    /// <param name="context">HTTP 上下文</param>
    /// <param name="useForwardedHeaders">是否解析 X-Forwarded-For（仅在直连 IP 命中 TrustedProxies 时生效）</param>
    /// <param name="trustedProxies">可信反向代理地址集合（CIDR 或精确 IP）</param>
    /// <returns>客户端真实 IP；无法获取时返回 null</returns>
    public static string? GetClientIp(
        HttpContext context,
        bool useForwardedHeaders,
        HashSet<string> trustedProxies)
    {
        var directIp = context.Connection.RemoteIpAddress?.ToString();

        // 零信任默认：无可信代理配置 / 转发头关闭 / 直连 IP 非可信代理 → 只认 RemoteIpAddress
        if (!useForwardedHeaders
            || trustedProxies.Count == 0
            || string.IsNullOrEmpty(directIp)
            || !IpAddressHelper.IsIpAllowed(directIp, trustedProxies))
        {
            return directIp;
        }

        // 可信代理后：从右向左取第一个非可信 IP（标准代理链算法，防止最左值被客户端伪造）
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var xff))
        {
            var hops = xff.ToString().Split(',');
            for (var i = hops.Length - 1; i >= 0; i--)
            {
                var hop = hops[i].Trim();
                if (string.IsNullOrEmpty(hop)) continue;
                if (!IpAddressHelper.IsIpAllowed(hop, trustedProxies))
                    return hop;
            }
        }

        return directIp;
    }
}
