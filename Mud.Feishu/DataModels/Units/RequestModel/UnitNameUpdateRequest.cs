namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位名称更新请求体
/// </summary>
public class UnitNameUpdateRequest
{
    /// <summary>
    /// 单位名字。
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
}
