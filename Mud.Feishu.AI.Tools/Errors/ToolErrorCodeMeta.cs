// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Errors;

/// <summary>
/// <b>E-2：飞书业务错误码的域专属元数据（codemeta）</b>——把一个裸 <c>code</c> 变成
/// "它是什么意思 + 下一步该做什么"。
/// </summary>
/// <remarks>
/// <para>
/// <b>对齐官方 <c>internal/errclass/codemeta_*.go</c> 的本地等价物</b>：官方按域维护了一张
/// "错误码 → 语义 + 处置"表，本类型是它在工具面的落地形态。
/// </para>
/// <para>
/// <b>⚠️ 收录纪律（DP-R7-4 的延续，比官方更严）</b>：<b>只收本仓有实证的码</b>——
/// 即能在仓库内找到"该码 = 某语义"的书面依据（SDK 接口文档 / 官方文档摘录 / 既有代码注释）。
/// <b>不得</b>凭记忆或推测补码：猜错的码会把模型引向错误的处置（例如把"系统繁忙"读成"权限不足"），
/// 而错误处置比"没有元数据"更糟——后者至少保持 <c>upstream_error</c> 的中性语义。
/// </para>
/// <para>
/// <b>未收录的码保持 <c>upstream_error</c></b>（分类不变），本表<b>只做增强、不做重分类</b>：
/// 分类由 <see cref="ToolErrorClassifier"/> 决定，本表只补"语义 + 行动"。
/// </para>
/// <para>
/// <b>为什么分类与元数据分开</b>：分类是<b>可分支的闭集</b>（模型按 <c>category</c>/<c>subtype</c>
/// 机械决策）；元数据是<b>可增补的事实表</b>（新码只需加一行，不改变分支面）。
/// 两者混在一起会让"加一个码"变成"改契约"。
/// </para>
/// </remarks>
internal static class ToolErrorCodeMeta
{
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
    public sealed record Entry(int Code, string? Domain, string Meaning, string Action);

    /// <summary>
    /// 码表（<b>仅收有实证的码</b>；新增必须同批补证据出处与守卫基线）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>证据出处（每条都必须能追溯到仓库内的书面依据）</b>：
    /// <list type="bullet">
    /// <item><c>99991400</c>（系统繁忙）——<c>documents/ApiDocs</c> 的 Lingo 文件下载接口文档：
    /// "HTTP 200 + JSON 错误体场景（如 <b>99991400 系统繁忙</b>、99991672 无权限等业务错误）"；
    /// 且 R7 方案文档以它作为 <c>rate_limited</c> 载荷示例。</item>
    /// <item><c>99991672</c>（无权限）——同上文档句："…<b>99991672 无权限</b>等业务错误"。</item>
    /// <item><c>99991663</c> / <c>99991661</c>（权限类）——<see cref="ToolErrorClassifier"/> 的
    /// <c>ForbiddenApiCodes</c> 既有实证（"无权限操作 / 权限不足，控制台权限未开通同段位"）。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>为什么不按方案点名的那 8 个域（calendar/drive/mail/task/vc/wiki/spark/minutes）铺码</b>：
    /// 本仓<b>没有任何</b>这些域的具体码样本（全仓检索仅命中上述 4 个码）。凭域铺码会产出
    /// "猜测性码表"——正是方案 §10 风险登记里"codemeta 码表猜测性错误 → 误导模型"的那一条。
    /// 故本表按<b>实际拿到样本</b>的粒度收录，并在《工具错误契约》文档中登记"域专属码表待样本"
    /// 这一开放项（<c>ToolErrorCodeMetaContractGuards</c> 锁定"条目必须有证据来源"）。
    /// </para>
    /// </remarks>
    public static IReadOnlyList<Entry> All { get; } =
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

    /// <summary>按码查元数据（未收录返回 <see langword="null"/>——<b>不得</b>回落猜测）。</summary>
    /// <param name="code">飞书业务码。</param>
    public static Entry? Find(int? code)
    {
        if (code is null)
        {
            return null;
        }

        foreach (var entry in All)
        {
            if (entry.Code == code.Value)
            {
                return entry;
            }
        }

        return null;
    }

    /// <summary>
    /// 由码表派生<b>恢复候选</b>（供 <c>suggestions</c> 使用）；未收录返回 <see langword="null"/>。
    /// </summary>
    /// <param name="code">飞书业务码。</param>
    /// <returns>形如 <c>"{Meaning}。建议：{Action}"</c> 的单条候选。</returns>
    public static IReadOnlyList<string>? BuildSuggestions(int? code)
    {
        var entry = Find(code);
        return entry is null
            ? null
            : [$"{entry.Meaning}。建议：{entry.Action}"];
    }
}
