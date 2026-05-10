// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceStats;

/// <summary>
/// 查询统计数据请求体
/// </summary>
public class QueryStatsDatasRequest
{
    /// <summary>
    /// <para>语言类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：zh</para>
    /// <para>可选值：<list type="bullet">
    /// <item>en：英语</item>
    /// <item>ja：日语</item>
    /// <item>zh：中文</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("locale")]
    public string Locale { get; set; } = string.Empty;

    /// <summary>
    /// <para>统计类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：month</para>
    /// <para>可选值：<list type="bullet">
    /// <item>daily：日度统计</item>
    /// <item>month：月度统计</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("stats_type")]
    public string StatsType { get; set; } = string.Empty;

    /// <summary>
    /// <para>开始时间，格式yyyyMMdd</para>
    /// <para>必填：是</para>
    /// <para>示例值：20210316</para>
    /// </summary>
    [JsonPropertyName("start_date")]
    public int StartDate { get; set; }

    /// <summary>
    /// <para>结束时间，格式yyyyMMdd</para>
    /// <para>（时间间隔不超过 31 天）</para>
    /// <para>必填：是</para>
    /// <para>示例值：20210323</para>
    /// </summary>
    [JsonPropertyName("end_date")]
    public int EndDate { get; set; }

    /// <summary>
    /// <para>查询的用户 ID 列表，与employee_type对应</para>
    /// <para>（用户数量不超过 200）</para>
    /// <para>* 必填字段(已全部升级到新系统，新系统要求必填)</para>
    /// <para>必填：否</para>
    /// <para>示例值：[ "ec8ddg56", "4dbb52f2", "4167842e" ]</para>
    /// </summary>
    [JsonPropertyName("user_ids")]
    public string[]? UserIds { get; set; }

    /// <summary>
    /// <para>是否包含离职人员和转出人员，默认为false不包含</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("need_history")]
    public bool? NeedHistory { get; set; }

    /// <summary>
    /// <para>* `true`：只展示员工当前所属考勤组数据</para>
    /// <para>* `false`：展示员工所有考勤组数据</para>
    /// <para>默认值：false</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("current_group_only")]
    public bool? CurrentGroupOnly { get; set; }

    /// <summary>
    /// <para>操作者的 user_id。与employee_type对应</para>
    /// <para>* 不同的操作者（管理员）的每个报表可能有不同的字段设置，系统将根据 user_id 查询指定报表的统计数据。</para>
    /// <para>* 必填字段（已全部升级到新系统，新系统要求该字段必填）。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ec8ddg56</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
