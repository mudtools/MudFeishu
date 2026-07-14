// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceRemedys;

/// <summary>
/// 获取补卡记录请求体
/// </summary>
public class QueryUserRemedysRequest
{
    /// <summary>
    /// <para>employee_no 或 employee_id 列表。传入的ID类型需要与employee_type的取值一致。最多支持50个</para>
    /// <para>必填：是</para>
    /// <para>示例值：["abd754f7"]</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[] UserIds { get; set; } = [];

    /// <summary>
    /// <para>查询的起始时间，精确到秒的时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1566641088</para>
    /// </summary>
    [JsonPropertyName("check_time_from")]
    public string CheckTimeFrom { get; set; } = string.Empty;

    /// <summary>
    /// <para>查询的结束时间，精确到秒的时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1592561088</para>
    /// </summary>
    [JsonPropertyName("check_time_to")]
    public string CheckTimeTo { get; set; } = string.Empty;

    /// <summary>
    /// <para>查询依据的时间类型（默认依据PeriodTime，如果使用非默认的，非特定租户不支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：PeriodTime</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PeriodTime：单据作用时间</item>
    /// <item>CreateTime：单据创建时间（目前暂不支持）</item>
    /// <item>UpdateTime：单据状态更新时间</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("check_date_type")]
    public string? CheckDateType { get; set; }

    /// <summary>
    /// <para>查询状态（不填默认查询已通过状态）</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：待审批</item>
    /// <item>1：未通过</item>
    /// <item>2：已通过</item>
    /// <item>3：已取消</item>
    /// <item>4：已撤回</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }
}
