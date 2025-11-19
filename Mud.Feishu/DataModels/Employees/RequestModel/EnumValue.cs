namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 枚举字段值
/// </summary>
public class EnumValue
{
    /// <summary>
    /// 选项结果ID  示例值：["1"]
    /// </summary>
    [JsonPropertyName("enum_ids")]
    public List<string> EnumIds { get; set; } = [];

    /// <summary>
    /// 选项类型 可选值有：1：文本  2：图片
    /// </summary>
    [JsonPropertyName("enum_type")]
    public required string EnumType { get; set; }
}
