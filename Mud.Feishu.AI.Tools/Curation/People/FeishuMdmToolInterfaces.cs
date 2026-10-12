// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Curation;

// ─────────────────────────── MDM · 国家/地区（F-1 首个补域） ───────────────────────────

/// <summary>
/// 工具接口：mdm.get_countries（映射 <c>IFeishuTenantV3MDMCountryRegion.GetBatchCountryRegionAsync</c>）。
/// </summary>
/// <remarks>
/// <para>
/// <b>F-1 补域首个切片</b>：MDM（主数据）此前零覆盖。本域先只策展<b>只读</b>的"国家/地区主数据"查询——
/// 它是飞书内的权威地区口径（行政编码 + 多语言名称 + ISO 代码 + 大洲），
/// 用于把 <c>MDCT</c> 编码或地区口径换成可读名称（多语言回复、地区归类、时区/区号辅助判断）。
/// </para>
/// <para>
/// <b>非 PII</b>：国家/地区属<b>公共参考数据</b>，不含员工个人信息 ⇒ 不进入
/// 《工具权限对照表》的 PII 登记（<c>scope-authority.json</c> 的 <c>piiTools</c>）。
/// </para>
/// <para>
/// <b>有意不策展</b>：同域的 <c>IFeishuTenantV1MDMUserAuthDataRelation</c>（用户数据维度绑定/解绑）
/// 是<b>写类</b>且涉及用户 ID，按"写面需宿主显式授权 + 默认不启用"的纪律另立批次；
/// <c>ListCountryRegionsAsync</c>（带 filter 表达式体的分页查询）留待下一片（先验证本切片流水线）。
/// </para>
/// </remarks>
[FeishuTool("mdm.get_countries",
    Description = "按主数据编码批量查询国家/地区主数据（多语言名称、ISO 两位/三位/数字代码、国际电话区号、所属大洲、状态）——把 MDCTxxxx 编码或地区口径变成可读名称时用它。ids 最多 100 个；languages 决定返回语言（zh-CN / en-US / ja-JP），name/full_name 取其中排序第一的语言。只读，需 mdm:country_region:read。",
    RequiredScopes = ["mdm:country_region:read"],
    Source = nameof(IFeishuTenantV3MDMCountryRegion) + "." + nameof(IFeishuTenantV3MDMCountryRegion.GetBatchCountryRegionAsync))]
public interface IFeishuTenantMdmGetCountriesTool
{
    /// <summary>批量查询国家/地区主数据。</summary>
    /// <returns>白名单投影后的 JSON 文本（total + items：mdm_code/name/full_name/alpha_2_code/alpha_3_code/numeric_code/global_code/continents/status），超长截断并标记 truncated。</returns>
    Task<string> GetCountriesAsync(
        [ToolParameter("ids", "主数据编码数组（必填，1~100 个，格式 MDCT + 8 位数字，如 [\"MDCT00000001\"]）", Required = true)] string[] ids,
        [ToolParameter("fields", "需要的查询字段集（必填，1~100 个；只回填需要的事实，如 [\"name\",\"alpha_2_code\"]）", Required = true)] string[] fields,
        [ToolParameter("languages", "希望返回的语言种类（必填，0~100 个：zh-CN / en-US / ja-JP；排序第一的语言进 name/full_name）", Required = true)] string[] languages,
        CancellationToken cancellationToken = default);
}
