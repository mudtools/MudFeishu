// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>工具错误语义分类（AI-FD-D12 P1D-2b）。</summary>
internal enum ToolErrorKind
{
    /// <summary>可重试：HTTP 5xx / 429 / 网络异常——模型可稍后重试同一调用。</summary>
    Retryable,

    /// <summary>参数错误：参数缺失/类型错/filter、sort 文法错——模型应换参数而非重试。</summary>
    InvalidArgs,

    /// <summary>权限不足：授权拒绝 / code=99991663 等权限类——模型应放弃或改用只读方案。</summary>
    Forbidden,

    /// <summary>
    /// 待用户确认（HITL）：授权器返回 <c>NeedsUserConfirmation</c>——既不是"权限被拒"（放弃）
    /// 也不是"参数错"（改参），而是"需人工介入"。AT-B12 新增：本语义原先被混入 <see cref="Forbidden"/>/
    /// <see cref="InvalidArgs"/> 的文案，会让模型做出错误的自愈动作（原文案含"请修正参数"）。
    /// </summary>
    NeedsConfirmation,

    /// <summary>其余业务错误（code != 0 等）——现行为语义。</summary>
    ApiError,
}

/// <summary>
/// 工具错误分类器（AI-FD-D12 P1D-2b）：纯函数——从异常类型 / 飞书 API code 段位映射到
/// <see cref="ToolErrorKind"/>，无配置。<b>授权拒绝与参数错误必须可区分</b>——这是模型
/// 自我修正（重试 or 换参数 or 放弃）的前提（路线图 12.1 风险「入参推断不稳」的对症缓解）。
/// </summary>
internal static class ToolErrorClassifier
{
    /// <summary>飞书权限类业务码（99991663=无权限操作 / 99991661=权限不足，控制台权限未开通同段位）。</summary>
    private static readonly int[] ForbiddenApiCodes = [99991663, 99991661];

    /// <summary>从异常类型分类（执行链 catch 路径；取消异常由调用方先行放行，不入此分类）。</summary>
    public static ToolErrorKind Classify(Exception exception) => exception switch
    {
        // ApiException 派生自 HttpRequestException，必须先于其判定。
        Mud.HttpUtils.ApiException api => ClassifyHttpStatus((int?)api.StatusCode),
        ArgumentException => ToolErrorKind.InvalidArgs,
        System.Net.Http.HttpRequestException => ToolErrorKind.Retryable,
        TimeoutException => ToolErrorKind.Retryable,
        _ => ToolErrorKind.ApiError,
    };

    /// <summary>从飞书 API code 分类（<c>FeishuApiOutcome</c> 解包路径）。</summary>
    public static ToolErrorKind ClassifyCode(int? code) => code switch
    {
        null => ToolErrorKind.ApiError,
        { } value when ForbiddenApiCodes.Contains(value) => ToolErrorKind.Forbidden,
        _ => ToolErrorKind.ApiError,
    };

    /// <summary>HTTP 状态段位：5xx / 429 可重试，401/403 权限类，其余业务错误。</summary>
    private static ToolErrorKind ClassifyHttpStatus(int? statusCode) => statusCode switch
    {
        null => ToolErrorKind.ApiError,
        (int)System.Net.HttpStatusCode.Forbidden or (int)System.Net.HttpStatusCode.Unauthorized => ToolErrorKind.Forbidden,
        >= 500 => ToolErrorKind.Retryable,
        429 => ToolErrorKind.Retryable,
        _ => ToolErrorKind.ApiError,
    };
}
