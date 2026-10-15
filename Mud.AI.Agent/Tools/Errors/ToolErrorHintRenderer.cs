// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tools.Errors;

/// <summary>
/// <b>恢复提示渲染层</b>——把结构化错误载荷渲染成<b>一行可执行的中文指引</b>。
/// </summary>
/// <remarks>
/// <b>对齐官方 <c>internal/recovery/hint.go</c></b>：官方在错误信封里带"typed error → 下一步做什么"的 hint 文本，
/// 本类型是它在工具面的落地形态。
/// <para>
/// <b>与 <c>suggestions</c> 的分工</b>（两者都要，不是二选一）：<c>suggestions</c> 是<b>结构化候选</b>（JSON 数组，机器消费，
/// 宿主 UI 可直接列按钮），本 hint 是<b>行级散文</b>（人类与模型都能零解析成本地读懂"现在该做什么"）——载荷给"是什么错了"，
/// hint 给"接下来怎么做"，后者的价值在于<b>不要求消费者解析载荷结构</b>。
/// </para>
/// <para>
/// <b>为什么插在载荷之后、正文之前</b>：正文可能被预算截断，而 hint 只有一行、位置靠前，因此几乎不会被截掉
/// （载荷本身有 ≤512 字节不变量与首行保证）。
/// </para>
/// <para>
/// <b>事实纪律</b>：hint 只陈述<b>已确定的语义</b>——权限类只回"工具声明的 scope"（不宣称"平台没开通这些"）；
/// 码表未收录的 <c>api_code</c> 不产出码级行动（保持中性文案）。
/// </para>
/// </remarks>
public static class ToolErrorHintRenderer
{
    /// <summary>hint 行的前缀（模型/宿主可用它单独识别这一行）。</summary>
    public const string HintPrefix = "[next] ";

    /// <summary>
    /// 渲染一行提示；<b>任何分类都产出非空行</b>（"未知分类"也必须给出保守指引——
    /// 空 hint 会让模型退回到解析正文，等于该能力不存在）。
    /// </summary>
    /// <param name="error">结构化错误载荷。</param>
    /// <returns>一行中文指引（<b>不含换行</b>）。</returns>
    public static string Render(ToolError error)
    {
        if (error is null)
        {
            return HintPrefix + "请如实报告用户该调用失败，并提供追踪号（如有）";
        }

        return HintPrefix + error.Category switch
        {
            "retryable" => RenderRetryable(error),
            "validation" => RenderValidation(error),
            "authorization" => RenderAuthorization(error),
            "policy" => "宿主策略已禁止该调用：请放弃该工具，或改用其他（通常只读的）方案；不要重试同一调用",
            "confirmation" => "该调用需要用户确认后才执行：请等待用户批准，不要自行重复发起",
            "content_safety" => "结果被内容安全规则阻断：请放弃该结果，不要改写参数重试",
            "api" => RenderApi(error),
            _ => "调用未成功：请如实报告用户（可附追踪号），不要臆造结果",
        };
    }

    /// <summary>可重试类：把 <c>retry_after_seconds</c> 变成"等多久再试"。</summary>
    private static string RenderRetryable(ToolError error)
    {
        if (error.RetryAfterSeconds is { } seconds && seconds > 0)
        {
            return $"等待约 {seconds.ToString(CultureInfo.InvariantCulture)} 秒后重试同一调用（同一参数）";
        }

        return error.Subtype switch
        {
            ToolErrorSubtype.RateLimited => "触达平台频率限制：请等待片刻后重试同一调用；不要立即反复重试",
            ToolErrorSubtype.Timeout => "调用超时：可重试一次；仍失败则改用更小的查询范围或减少预算",
            _ => "平台暂时不可用：可稍后重试同一调用；连续失败再报告用户",
        };
    }

    /// <summary>参数类：明确"改参数而不是重试"。</summary>
    private static string RenderValidation(ToolError error)
    {
        if (error.Suggestions is { Count: > 0 })
        {
            return "不要重试同一请求：请按 suggestions 给出的合法取值修正参数后重试";
        }

        return error.Subtype switch
        {
            ToolErrorSubtype.MissingRequired => "缺少必填参数：请补齐参数后重试（重试同一请求不会成功）",
            ToolErrorSubtype.ShapeMismatch => "参数形态不符合该工具契约：请按工具 Schema 的字段类型修正后重试",
            _ => "参数不合法：请核对工具 Schema 后修正参数重试（不要原样重试）",
        };
    }

    /// <summary>
    /// 授权类：投射<b>工具声明的 scope</b>（事实）并给出宿主侧动作——
    /// <b>不</b>断言平台是否已开通（无可靠判据）。
    /// </summary>
    private static string RenderAuthorization(ToolError error)
    {
        if (error.Subtype == ToolErrorSubtype.UserAuthRequired)
        {
            return "该调用需要用户本人完成授权：请转告用户授权后重试同一调用；不要换工具替代";
        }

        if (error.Subtype == ToolErrorSubtype.UserTokenRefreshFailed)
        {
            return "用户令牌续期失败：请转告用户重新完成授权后再试；不要重试同一调用";
        }

        if (error.RequiredScopes is { Count: > 0 } scopes)
        {
            return "该调用被授权拒绝：请宿主确认应用已开通该工具声明的权限"
                + $"（{string.Join("、", scopes)}）；模型侧不要重试同一调用";
        }

        return "该调用被授权拒绝：请宿主确认应用权限与授权器配置；模型侧不要重试同一调用";
    }

    /// <summary>
    /// 下游业务错误：有<b>码级候选</b>时直接沿用（产品侧按实证码表给出的"语义 + 行动"），
    /// 否则保持中性（<b>不猜测</b>）。
    /// </summary>
    /// <remarks>
    /// <b>为什么复用 <see cref="ToolError.Suggestions"/> 而不是自带码表</b>：产品码语义只存在于产品侧
    /// （独立产品线纪律，2026-10-14）——码级候选已随载荷一并传入，本渲染层据此成行即可；
    /// 中立包因此不再持有任何产品码。
    /// </remarks>
    private static string RenderApi(ToolError error)
        => error.Suggestions is { Count: > 0 } suggestions
            ? suggestions[0]
            : "下游返回业务错误：请如实报告用户（可附 api_code 与追踪号）；"
                + "不要原样重试——先核对参数或对象是否存在/可见";
}
