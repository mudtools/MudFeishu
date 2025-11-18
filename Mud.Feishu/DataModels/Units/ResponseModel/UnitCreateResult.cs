namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位创建结果响应模型，包含创建成功后的单位标识。
/// 用于创建单位操作成功后返回的单位ID信息。
/// </summary>
public class UnitCreateResult
{
    /// <summary>
    /// 创建成功的单位ID。
    /// <para>系统生成的唯一标识符，用于后续的引用和操作。</para>
    /// <para>在创建成功后返回，可用于查询、更新或删除该单位。</para>
    /// <para>示例值："6991111111111111111"</para>
    /// </summary>
    [JsonPropertyName("unit_id")]
    public string UnitId { get; set; } = string.Empty;
}
