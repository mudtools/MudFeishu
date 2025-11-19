namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位列表数据响应结果，包含单位列表的完整信息。
/// 用于获取系统中的所有单位或指定条件下的单位列表。
/// </summary>
public class UnitListDataResult : ApiListResult
{
    /// <summary>
    /// 单位列表。
    /// <para>包含所有符合条件的单位详细信息。</para>
    /// <para>每个单位包含ID、名称、类型等基本信息。</para>
    /// <para>支持分页查询和条件筛选。</para>
    /// </summary>
    [JsonPropertyName("unitlist")]
    public List<UnitInfo> UnitList { get; set; } = new List<UnitInfo>();
}
