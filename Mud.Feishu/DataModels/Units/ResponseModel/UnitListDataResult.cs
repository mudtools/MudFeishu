// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Units;

/// <summary>
/// 单位列表数据响应结果，包含单位列表的完整信息。
/// 用于获取系统中的所有单位或指定条件下的单位列表。
/// </summary>
public class UnitListDataResult : ApiPageListResult
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
