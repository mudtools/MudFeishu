// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 用户令牌就绪探测模式。
/// </summary>
/// <remarks>
/// <para>
/// 配置键 <c>FeishuAgent:UserTokenPrecheck</c>；消费点 <c>UserTokenPrecheck</c>（AI.Tools）。
/// </para>
/// <para>
/// <b>设计约束（零差异）</b>：默认 <see cref="CacheOnly"/>——只调
/// <c>HasValidTokenAsync</c>（内存镜像命中即零 IO），不产生网络往返。
/// </para>
/// <para>
/// 与 <see cref="FeishuToolRiskNames"/> / <see cref="ContentSafetyModes"/> 同款治理：
/// 闭集校验防拼写错误静默降级。
/// </para>
/// </remarks>
public static class UserTokenPrecheckModes
{
    /// <summary>关闭探测：user 身份工具直接下发，由下游 401 + TokenRecoveryExecutor 兜底。</summary>
    public const string Off = "off";

    /// <summary><b>默认</b>：只调 <c>HasValidTokenAsync</c>（内存镜像命中即零 IO）；不调 <c>CanRefreshTokenAsync</c>。</summary>
    public const string CacheOnly = "cache_only";

    /// <summary>完整探测：先 <c>HasValidTokenAsync</c>，未命中再 <c>CanRefreshTokenAsync</c>（可能触发 store 读穿透）。</summary>
    public const string Full = "full";

    /// <summary>合法取值清单（错误消息用）。</summary>
    public const string AllowedValuesText = $"{Off} / {CacheOnly} / {Full}";

    /// <summary>是否合法取值。</summary>
    /// <param name="mode">模式字面量。</param>
    public static bool IsValid(string? mode) => mode is Off or CacheOnly or Full;
}
