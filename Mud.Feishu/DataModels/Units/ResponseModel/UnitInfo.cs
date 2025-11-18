namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位信息响应模型，包含单位的基本信息和类型。
/// 用于获取单位详情或单位列表时的响应数据格式。
/// </summary>
public class UnitInfo
{
    /// <summary>
    /// 单位ID。
    /// <para>用于唯一标识一个单位。</para>
    /// <para>示例值："6991111111111111111"</para>
    /// </summary>
    [JsonPropertyName("unit_id")]
    public string UnitId { get; set; } = string.Empty;

    /// <summary>
    /// 单位名称。
    /// <para>表示单位的显示名称。</para>
    /// <para>示例值："字节跳动"</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 单位类型。
    /// <para>表示单位的组织类型或性质。</para>
    /// <para>常见值："department"（部门）、"company"（公司）等。</para>
    /// <para>示例值："company"</para>
    /// </summary>
    [JsonPropertyName("unit_type")]
    public string UnitType { get; set; } = string.Empty;
}
