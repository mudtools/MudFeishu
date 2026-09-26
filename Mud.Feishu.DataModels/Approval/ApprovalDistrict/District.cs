// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalDistrict;

/// <summary>
/// 审批地理库区域信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class District
{
    /// <summary>
    /// <para>区域的唯一标识</para>
    /// <para>示例值：115618457</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>示例值：敏斯特</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>层级</para>
    /// <para>可选值：</para>
    /// <para>- country：国家</para>
    /// <para>- province：省</para>
    /// <para>- city：市</para>
    /// <para>- district：区县</para>
    /// </summary>
    [JsonPropertyName("level")]
    public string? Level { get; set; }

    /// <summary>
    /// <para>是否有子区域</para>
    /// </summary>
    [JsonPropertyName("has_sub_district")]
    public bool? HasSubDistrict { get; set; }

    /// <summary>
    /// <para>父区域列表。仅遍历方式（list_type）为 leaf_level 时返回。</para>
    /// </summary>
    [JsonPropertyName("parent_districts")]
    public DistrictBaseInfo[]? ParentDistricts { get; set; }
}
