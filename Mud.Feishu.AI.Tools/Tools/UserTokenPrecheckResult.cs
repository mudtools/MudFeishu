// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 用户令牌就绪探测结果（T1 / D1）。
/// </summary>
/// <remarks>
/// <para>
/// 四态闭集：<see cref="Ready"/> / <see cref="Refreshable"/> / <see cref="AuthRequired"/> / <see cref="Skipped"/>。
/// </para>
/// <para>
/// <b>零令牌持有</b>：探测只读令牌状态，不缓存结果（授权/登出随时改变状态）。
/// </para>
/// </remarks>
public readonly record struct UserTokenPrecheckResult
{
    /// <summary>探测结果枚举字面量。</summary>
    public string Outcome { get; private init; }

    /// <summary>探测异常时的错误描述（仅 <see cref="Error"/> 模式有值；fail-open 放行时记录）。</summary>
    public string? Error { get; private init; }

    /// <summary>令牌就绪：可直接下发下游调用。</summary>
    public static UserTokenPrecheckResult Ready { get; } = new() { Outcome = Outcomes.Ready };

    /// <summary>令牌可续期：access token 过期但 refresh token 有效——可下发，下游 401 由 TokenRecoveryExecutor 自动续期。</summary>
    public static UserTokenPrecheckResult Refreshable { get; } = new() { Outcome = Outcomes.Refreshable };

    /// <summary>需要用户授权：无有效令牌且无 refresh token——返回 <c>user_auth_required</c>，零调用下游。</summary>
    public static UserTokenPrecheckResult AuthRequired { get; } = new() { Outcome = Outcomes.AuthRequired };

    /// <summary>探测关闭（<see cref="UserTokenPrecheckModes.Off"/>）：跳过探测，直接下发。</summary>
    public static UserTokenPrecheckResult Skipped { get; } = new() { Outcome = Outcomes.Skipped };

    /// <summary>探测异常（fail-open 放行 + 标记）：Redis 抖动等不应放大为全员拒绝。</summary>
    public static UserTokenPrecheckResult FromError { get; } = new() { Outcome = Outcomes.Error };

    /// <summary>是否为 <see cref="AuthRequired"/>（调用方据此短路返回结构化错误）。</summary>
    public bool IsAuthRequired => Outcome is Outcomes.AuthRequired;

    /// <summary>是否为跳过或错误（fail-open）——调用方据此放行。</summary>
    public bool ShouldProceed => Outcome is Outcomes.Ready or Outcomes.Refreshable or Outcomes.Skipped or Outcomes.Error;

    /// <summary>探测结果字面量闭集。</summary>
    public static class Outcomes
    {
        /// <summary>令牌有效（<c>ready</c>）。</summary>
        public const string Ready = "ready";

        /// <summary>令牌可续期（<c>refreshable</c>）。</summary>
        public const string Refreshable = "refreshable";

        /// <summary>需要用户授权（<c>auth_required</c>）。</summary>
        public const string AuthRequired = "auth_required";

        /// <summary>探测被关闭（<c>skipped</c>）。</summary>
        public const string Skipped = "skipped";

        /// <summary>探测异常、fail-open 放行（<c>error</c>；载荷另有 <see cref="UserTokenPrecheckResult.Error"/> 描述）。</summary>
        public const string Error = "error";
    }
}
