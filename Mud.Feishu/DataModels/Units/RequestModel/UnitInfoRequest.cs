namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位信息请求体
/// </summary>
public class UnitInfoRequest
{
    /// <summary>
    /// 自定义单位 ID，租户内唯一，创建后不可修改。
    /// <para>默认值：空，若不传值则由系统自动生成一个默认 ID。</para>
    /// <para>示例值："BU121"</para>
    /// </summary>
    [JsonPropertyName("unit_id")]
    public string? UnitId { get; set; }

    /// <summary>
    /// 单位名字。
    /// </summary>
    /// <remarks>
    /// <para>注意：在租户内，传入的 name 和 unit_type 不允许同时重复。例如，已存在一个名字 A、类型 A的单位，此时再创建一个名字 A、类型 A 的单位将会创建失败。</para>
    /// <para>示例值："消费者事业部"</para>
    /// </remarks>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// 自定义单位类型，创建后不可修改。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 注意：在租户内，传入的 name 和 unit_type 不允许同时重复。例如，已存在一个名字 A、类型 A的单位，此时再创建一个名字 A、类型 A 的单位将会创建失败。
    /// </para>
    /// <para>示例值："子公司"</para>
    /// </remarks>
    [JsonPropertyName("unit_type")]
    public required string UnitType { get; set; }
}
