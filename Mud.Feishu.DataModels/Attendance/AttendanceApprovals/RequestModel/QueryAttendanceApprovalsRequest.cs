// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceApprovals;

/// <summary>
/// 获取审批数据请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Attendance")]
public class QueryAttendanceApprovalsRequest
{
    /// <summary>
    /// <para>employee_no 或 employee_id 列表。传入的ID类型需要与employee_type的取值一致</para>
    /// <para>必填：是</para>
    /// <para>示例值：["abd754f7"]</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[] UserIds { get; set; } = [];

    /// <summary>
    /// <para>查询的起始日期。格式yyyyMMdd</para>
    /// <para>**注意**：传入的日期不能超过当天 +1 天，例如当天 20241010，则传入 20241011 支持查询，但传入 20241012 会报错。</para>
    /// <para>必填：是</para>
    /// <para>示例值：20190817</para>
    /// </summary>
    [JsonPropertyName("check_date_from")]
    public int CheckDateFrom { get; set; }

    /// <summary>
    /// <para>查询的结束日期，与 check_date_from 的时间间隔不超过 30 天。格式yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20190820</para>
    /// </summary>
    [JsonPropertyName("check_date_to")]
    public int CheckDateTo { get; set; }

    /// <summary>
    /// <para>查询依据的时间类型（不填默认依据PeriodTime）</para>
    /// <para>必填：否</para>
    /// <para>示例值：PeriodTime</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PeriodTime：单据作用时间</item>
    /// <item>CreateTime：单据创建时间</item>
    /// <item>UpdateTime：单据状态更新时间（灰度中，暂不开放）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_date_type")]
    public string? CheckDateType { get; set; }

    /// <summary>
    /// <para>查询状态（不填默认查询已通过状态）</para>
    /// <para>请假、加班：仅支持已通过和已撤回状态</para>
    /// <para>外出、出差：支持查询所有状态</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：待审批</item>
    /// <item>1：未通过</item>
    /// <item>2：已通过</item>
    /// <item>3：已撤回</item>
    /// <item>4：已撤销</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>查询的起始时间，精确到秒的时间戳（灰度中，暂不开放）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1566641088</para>
    /// </summary>
    [JsonPropertyName("check_time_from")]
    public string? CheckTimeFrom { get; set; }

    /// <summary>
    /// <para>查询的结束时间，精确到秒的时间戳（灰度中，暂不开放）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1592561088</para>
    /// </summary>
    [JsonPropertyName("check_time_to")]
    public string? CheckTimeTo { get; set; }
}
