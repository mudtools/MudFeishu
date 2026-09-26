// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 获取绩效结果（v1）请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class QueryReviewDataRequest
{
    /// <summary>
    /// <para>周期开始时间最小值，毫秒时间戳，小于该时间开始的周期会被过滤掉；填写了 semester_id_list 时本参数无效</para>
    /// <para>必填：是</para>
    /// <para>示例值：1430425599999</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>周期结束时间最大值，毫秒时间戳，大于该时间结束的周期会被过滤掉；填写了 semester_id_list 时本参数无效</para>
    /// <para>必填：是</para>
    /// <para>示例值：1630425599999</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>环节类型，目前仅支持终评环节、结果沟通环节、查看绩效结果环节（最大长度 50）：leader_review（终评环节）/ communication_and_open_result（结果沟通环节）/ view_result（查看绩效结果环节）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["leader_review"]</para>
    /// </summary>
    [JsonPropertyName("stage_types")]
    public string[]? StageTypes { get; set; }

    /// <summary>
    /// <para>环节状态，填写时按照指定状态获取绩效结果，不填查询所有状态的绩效结果（最大长度 50）：0（未开始）/ 1（待完成）/ 2（已截止）/ 3（已完成）/ 4（已复议）</para>
    /// <para>必填：否</para>
    /// <para>示例值：[1]</para>
    /// </summary>
    [JsonPropertyName("stage_progress")]
    public int[]? StageProgress { get; set; }

    /// <summary>
    /// <para>评估周期 ID 列表，可通过获取周期接口获取（最大长度 50）；填写时 start_time / end_time 无效</para>
    /// <para>必填：否</para>
    /// <para>示例值：["6992035450862224940"]</para>
    /// </summary>
    [JsonPropertyName("semester_id_list")]
    public string[]? SemesterIdList { get; set; }

    /// <summary>
    /// <para>被评估人 ID 列表，与入参 user_id_type 类型一致（最大长度 50）</para>
    /// <para>必填：是</para>
    /// <para>示例值：["ou_3245842393d09e9428ad4655da6e30b3"]</para>
    /// </summary>
    [JsonPropertyName("reviewee_user_id_list")]
    public string[]? RevieweeUserIdList { get; set; }

    /// <summary>
    /// <para>环节更新时间最早时间，毫秒时间戳，可筛选出在此时间之后有内容提交的环节数据</para>
    /// <para>必填：否</para>
    /// <para>示例值：1630425599999</para>
    /// </summary>
    [JsonPropertyName("updated_later_than")]
    public string? UpdatedLaterThan { get; set; }
}
