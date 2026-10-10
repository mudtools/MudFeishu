// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// <summary>
// 能力出处元工具（AT-F12 / R3 D1 的第三层「能力出路」）。
// </summary>
// <remarks>
// <para>
// <b>为什么需要它</b>：本仓的能力目录（Tier R）在编译期已全量可见，但暴露面是<b>策展</b>的
// （22 个工具 vs SDK 上千方法）。两者之间的鸿沟对模型<b>完全不可见</b>——模型遇到未覆盖的能力时
// 既不知道"没有"，也不知道"该放弃"，于是产生幻觉调用。本工具是唯一系统性的对冲。
// </para>
// <para>
// <b>边界（R3 D4，评审 C-3 收窄）</b>：只回答<b>能力分组级</b>的元数据（分组名 / 方法数 /
// 该分组所属模块是否已有策展工具），<b>不</b>返回方法名（生成器产物中本就没有方法名，
// 见 <c>CapabilityCatalogEmitter</c>）、<b>不</b>返回请求构造信息、<b>不</b>接受任意路径。
// 它不是"第 1156 个业务工具"，也不是通用裸 <c>api</c> 工具（决策①/⑤）。
// </para>
// </remarks>

/// <summary>工具接口：feishu.capability_lookup（能力分组检索；无 SDK 源，只读编译期目录）。</summary>
/// <remarks>
/// <b>无 <c>Source</c></b>：该工具不映射任何飞书 API（数据源是编译期能力目录常量），
/// 故与 <c>knowledge.search</c> 同属"绑定本地面"的一类——生成器对无 <c>Source</c> 的工具
/// 取 <c>risk=read</c> / <c>identity=tenant</c>，符合其真实语义。
/// </remarks>
[FeishuTool("feishu.capability_lookup",
    Description = "查询飞书开放平台能力在 SDK 中是否存在（按能力分组回答：组名 / 方法数 / 该组所属模块是否已有可用工具）。当所需能力不在当前工具集内时用它判断\"是没有还是没启用\"，避免凭空猜测调用不存在的接口。只返回元数据，不返回请求构造。",
    RequiredScopes = [])]
public interface IFeishuTenantCapabilityLookupTool
{
    /// <summary>按关键字检索能力分组。</summary>
    /// <returns>白名单投影后的 JSON 文本（sdk_method_count / curated_tool_count / matched_groups / curated_tools），超长截断并标记 truncated。</returns>
    Task<string> LookupAsync(
        [ToolParameter("keyword", "能力分组关键字（如 Calendar / Task / Docx / Bitable；大小写不敏感）", Required = true)] string keyword,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：feishu.guidance_read（读取 L2 references 资产；R5 / F-9）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么归入既有 <c>feishu</c> 域而非新开 <c>guidance</c> 域</b>（评审点④）：
/// 新开域需同步 <c>FeishuGuidanceComposer</c> 优先级表、<c>GuidanceAssetContractGuards</c>
/// 域清单与 golden 三处，且它本身不属于任何业务域——与 <c>feishu.capability_lookup</c>
/// 同属"元工具"，放在同一域语义一致。
/// </para>
/// <para>
/// <b>为什么需要 L2（按需读取）</b>：L1 是常驻 guidance，受
/// <c>FeishuGuidanceComposer</c> 预算（B-12 → R6/S1 已升到 16384）硬约束；16 个域的避坑文本全量预拼
/// 仍会逼近上限。L2 把深层文本（命令级避坑、完整示例）挪到按需读取，<b>由模型在调用失败后主动拉取</b>——
/// 这也是官方 <c>lark-cli skills read</c> 的等价物。
/// </para>
/// </remarks>
[FeishuTool("feishu.guidance_read",
    Description = "按需读取某个域的深层避坑/示例文本（如 im/topics-and-replies、docx/block-editing）。常驻 guidance 只放要点；当某个工具连续失败或你不确定某域的约束时，先读它再重试。只读，需 feishu:base。",
    RequiredScopes = ["feishu:base"])]
// Source 刻意留空：本工具读取的是**编译期内嵌的 guidance 资产**，不调用任何 SDK 接口。
// 编造一个 SDK 方法名（如 capability_lookup 那样）会让 MUDFT019「工具面不得与 SDK 脱钩」的门禁形同虚设。
public interface IFeishuTenantGuidanceReadTool
{
    /// <summary>读取 L2 guidance。</summary>
    /// <returns>L2 正文文本；键不存在时返回<b>可执行的候选清单</b>而非空结果。</returns>
    Task<string> GuidanceReadAsync(
        [ToolParameter("domain", "域（如 im / docx / approval），即工具名前缀", Required = true)] string domain,
        [ToolParameter("topic", "主题（如 topics-and-replies / block-editing / decisions）", Required = true)] string topic,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具接口：feishu.tool_search（B5：已策展工具检索；无 SDK 源，只读注册表/契约表）。
/// </summary>
/// <remarks>
/// <para>
/// <b>与既有元工具的分工</b>：
/// <list type="table">
/// <item><term><c>feishu.capability_lookup</c></term><description>回答"SDK 里有没有这个能力（几个分组）"——编译期能力目录（L1）</description></item>
/// <item><term><c>feishu.tool_search</c>（本工具）</term><description>回答"已策展的工具里哪个能干这事，启用了吗"——注册表/契约表（L2/L3）</description></item>
/// <item><term><c>feishu.schema_read</c></term><description>回答"某个 SDK 方法怎么调（HTTP/参数）"——编译期方法目录（1228 方法）</description></item>
/// </list>
/// </para>
/// <para>
/// <b>边界</b>：只返回<b>已注册</b>工具（含未启用），不返回 SDK 方法名（那是 <c>schema_read</c> 的职责），
/// 避免"变相通用调用"。
/// </para>
/// <para>
/// 默认启用（元工具，只读，<c>feishu:base</c> scope，与 <c>capability_lookup</c> 同档）。
/// </para>
/// </remarks>
[FeishuTool("feishu.tool_search",
    Description = "在已策展的飞书工具中按关键字/域/读写过滤搜索。返回工具名、域、是否写操作、风险等级、所需参数、是否已启用。当工具变多后用它快速定位能用哪个工具，以及该工具是否在当前宿主已启用（未启用的会给出原因提示）。只读，需 feishu:base。",
    RequiredScopes = ["feishu:base"])]
public interface IFeishuTenantToolSearchTool
{
    /// <summary>搜索已策展的工具。</summary>
    /// <returns>JSON 文本（工具列表，含 name/domain/is_write/risk/identity/required_params/enabled/hint），超长截断并标记 truncated。</returns>
    Task<string> SearchAsync(
        [ToolParameter("keyword", "按工具名/描述/域关键字搜索（大小写不敏感）；为空时返回全部（受 limit 约束）")] string? keyword = null,
        [ToolParameter("domain", "限定域（如 bitable / docx / drive；为空时不限域）")] string? domain = null,
        [ToolParameter("write_only", "只返回写类工具（默认 false）")] bool? write_only = null,
        [ToolParameter("read_only", "只返回只读工具（默认 false）")] bool? read_only = null,
        [ToolParameter("limit", "返回上限（默认 10，上限 30）")] int? limit = null,
        CancellationToken cancellationToken = default);
}