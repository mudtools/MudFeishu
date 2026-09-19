// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 证书校验模式（C3/R3）。
/// </summary>
public enum CertificateValidationMode
{
    /// <summary>生产默认：校验证书，拒绝自签名/名称不匹配</summary>
    Strict = 0,

    /// <summary>开发：允许自签名与名称不匹配（生产环境 Validate 失败）</summary>
    Dev = 1,

    /// <summary>自定义：必须提供 CustomCallback</summary>
    Custom = 2
}

/// <summary>
/// WebSocket 证书与协议安全嵌套配置（C3/R3）。
/// </summary>
/// <remarks>
/// 安全默认：Mode=Strict、AllowInsecureWebSocket=false、ValidateServerCertificate=true。
/// 不得回退安全默认。
/// </remarks>
public class WebSocketCertificateOptions
{
    /// <summary>证书校验模式，默认 Strict</summary>
    public CertificateValidationMode Mode { get; set; } = CertificateValidationMode.Strict;

    /// <summary>是否允许 ws://（协议层），默认 false</summary>
    public bool AllowInsecureWebSocket { get; set; } = false;

    /// <summary>是否校验 SSL 证书，默认 true</summary>
    /// <remarks>
    /// R5/X5 优先级链：<c>CustomCallback</c> &gt; <c>ValidateServerCertificate=false</c>（完全关闭校验）
    /// &gt; <c>Mode=Dev</c> &gt; <c>Mode=Strict</c>。即「完全关闭校验」的能力优先于一切模式。
    /// </remarks>
    public bool ValidateServerCertificate { get; set; } = true;

    /// <summary>是否允许自签名，默认 false</summary>
    /// <remarks>
    /// R5/X5 起 <c>Mode</c> 是运行时主开关：<c>Mode=Dev</c> 无条件放宽「自签名根」与「名称不匹配」
    /// （本属性不参与 Dev 分支判定）；<c>Mode=Strict</c> 时本属性必须为 false
    /// （由 <c>ValidateCertificateOptions</c> 强制）。保留本属性仅用于 Strict 的一致性校验与
    /// 运行期防御性读取，计划下个 major 删除——新配置请改用 <c>Mode=Dev</c>。
    /// </remarks>
    public bool AllowSelfSignedCertificates { get; set; } = false;

    /// <summary>是否允许证书名称不匹配，默认 false</summary>
    /// <remarks>语义与 <see cref="AllowSelfSignedCertificates"/> 相同（R5/X5 后由 <c>Mode</c> 主导）。</remarks>
    public bool AllowCertificateNameMismatch { get; set; } = false;

    /// <summary>自定义证书回调（仅代码配置；Mode=Custom 时必须提供）</summary>
    public System.Net.Security.RemoteCertificateValidationCallback? CustomCallback { get; set; }
}

/// <summary>
/// WebSocket 重连嵌套配置（C3/R3）。
/// </summary>
public class WebSocketReconnectOptions
{
    /// <summary>是否自动重连，默认 true</summary>
    public bool Auto { get; set; } = true;

    /// <summary>最大重连次数，0=无限（受 TotalBudget 限制），默认 5</summary>
    public int MaxAttempts { get; set; } = 5;

    /// <summary>基础重连延迟（毫秒），默认 5000；非法值由 FeishuWebSocketOptions.Validate 拒绝</summary>
    public int BaseDelayMs { get; set; } = 5000;

    /// <summary>最大重连延迟（毫秒），默认 30000；须 ≥ BaseDelayMs，由 Validate 拒绝</summary>
    public int MaxDelayMs { get; set; } = 30000;

    /// <summary>重连总时间预算，默认 30 分钟</summary>
    public TimeSpan TotalBudget { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>重连冷却，默认 5 秒</summary>
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>认证最大重试次数，默认 5</summary>
    public int MaxAuthRetryAttempts { get; set; } = 5;
}
