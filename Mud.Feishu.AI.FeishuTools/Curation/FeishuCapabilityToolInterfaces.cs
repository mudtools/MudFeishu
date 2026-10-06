// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Curation;

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
public interface IFeishuCapabilityLookupTool
{
    /// <summary>按关键字检索能力分组。</summary>
    /// <returns>白名单投影后的 JSON 文本（sdk_method_count / curated_tool_count / matched_groups / curated_tools），超长截断并标记 truncated。</returns>
    Task<string> LookupAsync(
        [ToolParameter("keyword", "能力分组关键字（如 Calendar / Task / Docx / Bitable；大小写不敏感）", Required = true)] string keyword,
        CancellationToken cancellationToken = default);
}
