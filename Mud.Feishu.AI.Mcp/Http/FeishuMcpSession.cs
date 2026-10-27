// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// 一条 HTTP 侧的 MCP 会话：<b>握手状态 + 租户绑定</b>，随会话标识一起驻留在
/// <see cref="FeishuMcpSessionDirectory"/>。
/// </summary>
/// <remarks>
/// <b>为什么租户绑定必须落在会话上而不是每条请求重新取</b>：会话标识是服务端随机发的、
/// 客户端无法自选，绑定后逐请求校验「认证给出的 appKey == 会话绑定的 appKey」，
/// 一个泄漏的会话标识就只能在它自己的租户里用。若允许按请求改绑，
/// 「拿到 A 租户的会话标识」就等于「切到 B 租户」——那正是 stdio 用进程边界避免的事。
/// </remarks>
internal sealed class FeishuMcpSession
{
    /// <summary>初始化一条会话（由 <see cref="FeishuMcpSessionDirectory.Create"/> 调用）。</summary>
    /// <param name="id">会话标识（服务端生成）。</param>
    /// <param name="appKey">宿主认证层给出的租户标识。</param>
    /// <param name="userId">宿主认证层给出的用户标识（可空）。</param>
    /// <param name="protocolVersion">本次握手协商出的协议版本。</param>
    /// <param name="now">当前 UTC 时刻。</param>
    public FeishuMcpSession(string id, string appKey, string? userId, string protocolVersion, DateTimeOffset now)
    {
        Id = id;
        AppKey = appKey;
        UserId = userId;
        ProtocolVersion = protocolVersion;
        LastSeenUtc = now;
    }

    /// <summary>会话标识（下发在 <c>Mcp-Session-Id</c> 响应头）。</summary>
    public string Id { get; }

    /// <summary>绑定的租户标识（构造后不变）。</summary>
    public string AppKey { get; }

    /// <summary>绑定的用户标识（可空，构造后不变）。</summary>
    public string? UserId { get; }

    /// <summary>本次会话协商出的协议版本。</summary>
    public string ProtocolVersion { get; }

    /// <summary>是否已完成 <c>initialize</c>。</summary>
    public bool IsInitialized { get; private set; }

    /// <summary>最近一次被访问的 UTC 时刻（空闲回收判据）。</summary>
    public DateTimeOffset LastSeenUtc { get; private set; }

    /// <summary>标记握手完成。</summary>
    public void MarkInitialized() => IsInitialized = true;

    /// <summary>刷新访问时刻。</summary>
    /// <param name="now">当前 UTC 时刻。</param>
    public void Touch(DateTimeOffset now) => LastSeenUtc = now;

    /// <summary>是否已空闲超过阈值。</summary>
    /// <param name="idle">空闲上限。</param>
    /// <param name="now">当前 UTC 时刻。</param>
    /// <returns>是否应回收。</returns>
    public bool IsExpired(TimeSpan idle, DateTimeOffset now) => now - LastSeenUtc > idle;
}
