// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Tools;
using Mud.AI.Agent.Tools.Errors;

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 飞书业务错误码表（<b>产品侧唯一真相源</b>）：业务码 → 分类段位 + "语义 + 行动"候选。
/// </summary>
/// <remarks>
/// <para>
/// <b>码表必须在飞书侧（产品线归属）</b>：中立的 <c>Mud.AI.Agent</c> 不得持有任何产品码语义——
/// 产品码表只能由"知道全部产品"的产品侧维护，中立层不得携带产品事实（否则中立包无法独立上游化）。
/// 码表经 <see cref="ToolApiFailure"/> 随解包结果传给中立层，中立层只消费已分类的读数。
/// </para>
/// <para>
/// <b>收录纪律（比官方更严）</b>：<b>只收本仓有实证的码</b>——即能在仓库内找到
/// "该码 = 某语义"的书面依据（SDK 接口文档 / 官方文档摘录 / 既有代码注释）。<b>不得</b>凭记忆或
/// 推测补码：猜错的码会把模型引向错误处置（例如把"系统繁忙"读成"权限不足"），而错误处置比
/// "没有码表"更糟——后者至少保持中性的 <c>upstream_error</c> 语义。未收录的码<b>不产出</b>候选，
/// 分类也不变。
/// </para>
/// <para>
/// <b>分类与元数据分开</b>：分类是<b>可分支的闭集</b>（模型按 <c>category</c>/<c>subtype</c> 机械决策）；
/// 元数据是<b>可增补的事实表</b>（新码只需加一行，不改变分支面）。两者混在一起会让"加一个码"
/// 变成"改契约"。
/// </para>
/// </remarks>
internal static class FeishuApiErrorVocabulary
{
    /// <summary>飞书权限类业务码（99991663=无权限操作 / 99991661=权限不足 / 99991672=应用未开通权限或调用方无访问权）。</summary>
    private static readonly int[] ForbiddenApiCodes = [99991663, 99991661, 99991672];

    /// <summary>
    /// 飞书<b>可重试</b>业务码：<c>99991400</c> = 平台系统繁忙（限流/过载）。
    /// </summary>
    /// <remarks>
    /// <b>证据</b>：<c>documents/ApiDocs</c> 的接口文档明确写"…（如 <b>99991400 系统繁忙</b>、
    /// 99991672 无权限等业务错误）"；语义是"服务端暂时无法处理"，与 429/5xx 同类。
    /// 该码可能以 <b>HTTP 200 + JSON 错误体</b>形态返回（见同一文档），即不会走 HTTP 段位判定——
    /// 若只靠 HTTP 段位，它会被误判为不可重试的 <c>api/upstream_error</c>，
    /// 模型因此放弃一个"本该退避重试"的调用。故按码补一条可重试路径。
    /// </remarks>
    private static readonly int[] RetryableApiCodes = [99991400];

    /// <summary>
    /// 码级元数据表（<b>仅收有实证的码</b>；新增必须同批补证据出处与守卫基线）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>证据出处（每条都必须能追溯到仓库内的书面依据）</b>：
    /// <list type="bullet">
    /// <item><c>99991400</c>（系统繁忙）——<c>documents/ApiDocs</c> 的 Lingo 文件下载接口文档：
    /// "HTTP 200 + JSON 错误体场景（如 <b>99991400 系统繁忙</b>、99991672 无权限等业务错误）"。</item>
    /// <item><c>99991672</c>（无权限）——同上文档句："…<b>99991672 无权限</b>等业务错误"。</item>
    /// <item><c>99991663</c> / <c>99991661</c>（权限类）——见上方 <c>ForbiddenApiCodes</c> 的既有实证。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>为什么不按域铺码</b>：本仓<b>没有任何</b>域专属码样本（全仓检索仅命中上述 4 个码）。
    /// 凭域铺码会产出"猜测性码表"，把模型引向错误处置。故按<b>实际拿到样本</b>的粒度收录，
    /// 并在《工具错误契约》文档中登记"域专属码表待样本"这一开放项——码表条目必须有证据来源。
    /// </para>
    /// </remarks>
    private static readonly Entry[] Entries =
    [
        new Entry(
            99991400,
            Domain: null,
            Meaning: "飞书平台系统繁忙（限流或服务端过载）",
            Action: "等待片刻后重试同一调用（可依据 retry_after_seconds 退避）；连续失败再如实报告用户"),

        new Entry(
            99991672,
            Domain: null,
            Meaning: "无权限：应用未开通该接口权限，或调用方对该数据无访问权",
            Action: "确认开放平台已为该应用开通对应权限并发布版本；不要重试同一调用"),

        new Entry(
            99991663,
            Domain: null,
            Meaning: "无权限操作：应用或用户不具备该操作权限",
            Action: "确认开放平台已开通所需权限并发布版本；不要重试同一调用"),

        new Entry(
            99991661,
            Domain: null,
            Meaning: "权限不足：缺少该接口所需权限点",
            Action: "确认开放平台已开通所需权限并发布版本；不要重试同一调用"),
    ];

    /// <summary>
    /// 码级元数据表（测试守卫消费：断言每条都有证据来源、域标注（若有）是真实工具域）。
    /// </summary>
    internal static IReadOnlyList<Entry> All => Entries;

    /// <summary>
    /// 读出一个业务码的<b>完整读数</b>（分类段位 + 码级候选）。
    /// </summary>
    /// <remarks>
    /// 判定顺序：<b>权限类 → 可重试类 → 其余</b>（权限与可重试互斥，顺序只影响可读性）。
    /// 真正的纪律是<b>未收录的码一律落到中性的 <c>api/upstream_error</c></b>——不猜语义；
    /// 但码级元数据（若有）仍随读数透出（分类不变、只做增强）。
    /// </remarks>
    /// <param name="code">飞书业务码（可能为 null——无 code 场景）。</param>
    /// <returns>读数（供 <c>ToolApiOutcome.Failure</c> 使用）。</returns>
    internal static ToolApiFailure Classify(int? code)
    {
        var suggestions = BuildSuggestions(code);

        if (code is { } forbidden && ForbiddenApiCodes.Contains(forbidden))
        {
            return new ToolApiFailure(
                ToolErrorCategory.Authorization, ToolErrorSubtype.AuthorizationDenied, suggestions);
        }

        if (code is { } retryable && RetryableApiCodes.Contains(retryable))
        {
            return new ToolApiFailure(
                ToolErrorCategory.Retryable, ToolErrorSubtype.RateLimited, suggestions);
        }

        return suggestions is null
            ? ToolApiFailure.Unclassified
            : new ToolApiFailure(ToolErrorCategory.Api, ToolErrorSubtype.UpstreamError, suggestions);
    }

    /// <summary>
    /// 由码表派生<b>恢复候选</b>（供载荷 <c>suggestions</c> 与 hint 行共用）；未收录返回 <see langword="null"/>。
    /// </summary>
    /// <param name="code">飞书业务码。</param>
    /// <returns>形如 <c>"{Meaning}。建议：{Action}"</c> 的单条候选；未收录为 <see langword="null"/>。</returns>
    private static IReadOnlyList<string>? BuildSuggestions(int? code)
    {
        var entry = Find(code);
        return entry is null ? null : [$"{entry.Meaning}。建议：{entry.Action}"];
    }

    /// <summary>按码查元数据（未收录返回 <see langword="null"/>——<b>不得</b>回落猜测）。</summary>
    private static Entry? Find(int? code)
    {
        if (code is null)
        {
            return null;
        }

        foreach (var entry in Entries)
        {
            if (entry.Code == code.Value)
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>
    /// 单个错误码的元数据。
    /// </summary>
    /// <param name="Code">飞书业务码。</param>
    /// <param name="Domain">
    /// 适用域（工具名首个 <c>.</c> 之前的部分）；<c>null</c> = <b>跨域通用码</b>
    /// （如平台级限流/权限码，不归属任何单一业务域）。
    /// </param>
    /// <param name="Meaning">该码的含义（一句中文；<b>只陈述实证事实</b>）。</param>
    /// <param name="Action">建议的下一步（可执行动作；进 <c>suggestions</c> 与 hint 行）。</param>
    internal sealed record Entry(int Code, string? Domain, string Meaning, string Action);
}
