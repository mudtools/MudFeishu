// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceStats;

/// <summary>
/// 查询统计设置请求体
/// </summary>
public class QueryStatsViewsRequest
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
    /// <para>示例值：daily</para>
    /// <para>可选值：<list type="bullet">
    /// <item>daily：日度统计</item>
    /// <item>month：月度统计</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("stats_type")]
    public string StatsType { get; set; } = string.Empty;

    /// <summary>
    /// <para>操作者的用户id，对应employee_type</para>
    /// <para>* 必填字段(系统升级后，新系统要求必填)</para>
    /// <para>必填：否</para>
    /// <para>示例值：dd31248a</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}
