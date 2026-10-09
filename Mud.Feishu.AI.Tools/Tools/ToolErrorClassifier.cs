// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// 工具错误大类（B2 错误契约，8 类闭集）。
/// </summary>
/// <remarks>
/// <para>
/// <b>R7-评审修正 RV-1</b>：项目未发布，<c>ToolErrorKind</c> 直接改名为 <c>ToolErrorCategory</c>，
/// 不做 <c>[Obsolete]</c> 双轨——减少同义双轨。
/// </para>
/// <para>
/// 对齐官方 <c>errs/ERROR_CONTRACT.md</c> 的 9 类语义，但收敛为工具面所需的 8 类。
/// </para>
/// </remarks>
internal enum ToolErrorCategory
{
    /// <summary>参数验证失败：参数缺失/类型错/filter、sort 文法错——模型应换参数而非重试。</summary>
    Validation,

    /// <summary>策略拒绝：风险超限/身份不匹配/工具未启用——模型应放弃或换工具。</summary>
    Policy,

    /// <summary>授权拒绝：授权器拒绝/缺少 scope——模型应放弃或改用只读方案。</summary>
    Authorization,

    /// <summary>待用户确认（HITL）：需人工介入，既不是"权限被拒"也不是"参数错"。</summary>
    Confirmation,

    /// <summary>可重试：HTTP 5xx / 429 / 网络异常——模型可稍后重试同一调用。</summary>
    Retryable,

    /// <summary>飞书业务错误（code != 0 且非权限段位）——模型应报告或换参数。</summary>
    Api,

    /// <summary>内部错误：未预期异常/净化器拒绝——模型应报告追踪号。</summary>
    Internal,

    /// <summary>内容安全阻断：工具结果命中内容安全规则——模型应放弃。</summary>
    ContentSafety,
}

/// <summary>
/// 工具错误子类常量（B2 错误契约）：与 <see cref="ToolErrorCategory"/> 配对使用，
/// 提供更细粒度的错误原因。全部为 <see langword="internal"/> 常量——新增需评审 + 守卫断言。
/// </summary>
internal static class ToolErrorSubtype
{
    // Validation
    public const string InvalidArgs = "invalid_args";
    public const string MissingRequired = "missing_required";
    public const string ShapeMismatch = "shape_mismatch";

    // Policy
    public const string RiskExceeded = "risk_exceeded";
    public const string IdentityMismatch = "identity_mismatch";
    public const string ToolNotAllowed = "tool_not_allowed";

    // Authorization
    public const string AuthorizationDenied = "authorization_denied";
    public const string MissingScope = "missing_scope";

    // Confirmation
    public const string NeedsUserConfirmation = "needs_user_confirmation";

    // Retryable
    public const string RateLimited = "rate_limited";
    public const string Transient = "transient";
    public const string Timeout = "timeout";

    // Api
    public const string UpstreamError = "upstream_error";

    // Internal
    public const string Unexpected = "unexpected";
    public const string SanitizerRejected = "sanitizer_rejected";

    // ContentSafety
    public const string InjectedContentBlocked = "injected_content_blocked";
}

/// <summary>
/// 工具错误分类器（B2 错误契约）：纯函数——从异常类型 / 飞书 API code 段位映射到
/// <see cref="ToolErrorCategory"/> 与 <see cref="ToolErrorSubtype"/>，无配置。
/// </summary>
/// <remarks>
/// <b>授权拒绝与参数错误必须可区分</b>——这是模型自我修正（重试 or 换参数 or 放弃）的前提。
/// </remarks>
internal static class ToolErrorClassifier
{
    /// <summary>飞书权限类业务码（99991663=无权限操作 / 99991661=权限不足，控制台权限未开通同段位）。</summary>
    private static readonly int[] ForbiddenApiCodes = [99991663, 99991661];

    /// <summary>从异常类型分类（执行链 catch 路径；取消异常由调用方先行放行，不入此分类）。</summary>
    public static (ToolErrorCategory Category, string Subtype) Classify(Exception exception) => exception switch
    {
        // ApiException 派生自 HttpRequestException，必须先于其判定。
        Mud.HttpUtils.ApiException api => ClassifyHttpStatus((int?)api.StatusCode),
        ArgumentException => (ToolErrorCategory.Validation, ToolErrorSubtype.InvalidArgs),
        System.Net.Http.HttpRequestException => (ToolErrorCategory.Retryable, ToolErrorSubtype.Transient),
        TimeoutException => (ToolErrorCategory.Retryable, ToolErrorSubtype.Timeout),
        _ => (ToolErrorCategory.Internal, ToolErrorSubtype.Unexpected),
    };

    /// <summary>从飞书 API code 分类（<c>FeishuApiOutcome</c> 解包路径）。</summary>
    public static (ToolErrorCategory Category, string Subtype) ClassifyCode(int? code) => code switch
    {
        null => (ToolErrorCategory.Api, ToolErrorSubtype.UpstreamError),
        { } value when ForbiddenApiCodes.Contains(value) => (ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied),
        _ => (ToolErrorCategory.Api, ToolErrorSubtype.UpstreamError),
    };

    /// <summary>HTTP 状态段位：5xx/429 可重试，401/403 权限类，其余业务错误。</summary>
    private static (ToolErrorCategory Category, string Subtype) ClassifyHttpStatus(int? statusCode) => statusCode switch
    {
        null => (ToolErrorCategory.Api, ToolErrorSubtype.UpstreamError),
        (int)System.Net.HttpStatusCode.Forbidden or (int)System.Net.HttpStatusCode.Unauthorized
            => (ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied),
        429 => (ToolErrorCategory.Retryable, ToolErrorSubtype.RateLimited),
        >= 500 => (ToolErrorCategory.Retryable, ToolErrorSubtype.Transient),
        _ => (ToolErrorCategory.Api, ToolErrorSubtype.UpstreamError),
    };
}
