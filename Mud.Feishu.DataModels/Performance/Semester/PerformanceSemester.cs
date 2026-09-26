// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效周期（semester）信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceSemester
{
    /// <summary>
    /// <para>周期 ID</para>
    /// <para>示例值：6992035450862224940</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>周期年份</para>
    /// <para>示例值：2024</para>
    /// </summary>
    [JsonPropertyName("year")]
    public int? Year { get; set; }

    /// <summary>
    /// <para>周期类型分组（Annual/Semi-annual/Quarter/Bimonth/Month/Non-standard）</para>
    /// <para>示例值：Quarter</para>
    /// </summary>
    [JsonPropertyName("type_group")]
    public string? TypeGroup { get; set; }

    /// <summary>
    /// <para>周期类型（Annual/H1/H2/Q1~Q4/月份/Custom 等）</para>
    /// <para>示例值：Q1</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>周期名称（中划线形态 zh-CN / en-US）</para>
    /// </summary>
    [JsonPropertyName("name")]
    public PerformanceSemesterName? Name { get; set; }

    /// <summary>
    /// <para>周期状态：initiating（初始化）/ enabled（已启动）</para>
    /// <para>示例值：enabled</para>
    /// </summary>
    [JsonPropertyName("progress")]
    public string? Progress { get; set; }

    /// <summary>
    /// <para>周期开始时间，毫秒时间戳</para>
    /// <para>示例值：1625068800000</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>周期结束时间，毫秒时间戳</para>
    /// <para>示例值：1640966399999</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>周期创建时间，毫秒时间戳</para>
    /// <para>示例值：1625068800000</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>周期更新时间，毫秒时间戳</para>
    /// <para>示例值：1625068800000</para>
    /// </summary>
    [JsonPropertyName("modify_time")]
    public string? ModifyTime { get; set; }

    /// <summary>
    /// <para>周期创建人 ID，与入参 user_id_type 类型一致</para>
    /// <para>示例值：ou_ce613028fe74745421f5dc320bb9c709</para>
    /// </summary>
    [JsonPropertyName("create_user_id")]
    public string? CreateUserId { get; set; }

    /// <summary>
    /// <para>周期更新人 ID，与入参 user_id_type 类型一致</para>
    /// <para>示例值：ou_ce613028fe74745421f5dc320bb9c709</para>
    /// </summary>
    [JsonPropertyName("modify_user_id")]
    public string? ModifyUserId { get; set; }
}
