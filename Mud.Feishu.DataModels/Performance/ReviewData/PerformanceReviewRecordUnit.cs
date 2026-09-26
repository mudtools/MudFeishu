// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效详情数据（v2）中评估记录的评估内容明细
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewRecordUnit
{
    /// <summary>
    /// <para>评估内容 ID</para>
    /// <para>示例值：7343513161666707459</para>
    /// </summary>
    [JsonPropertyName("unit_id")]
    public string? UnitId { get; set; }

    /// <summary>
    /// <para>是否为不了解；当评估人选不了解时会返回为 true，其他时候不返回</para>
    /// </summary>
    [JsonPropertyName("is_unknown")]
    public bool? IsUnknown { get; set; }

    /// <summary>
    /// <para>评估题列表，指评估内容中的每个题，可能是评估项或者填写项</para>
    /// </summary>
    [JsonPropertyName("data")]
    public PerformanceReviewDetail[]? Data { get; set; }
}
