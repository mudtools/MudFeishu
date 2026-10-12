// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 获取应用使用概览请求体。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppUsageOverviewRequest
{
    /// <summary>
    /// <para>查询日期，格式 yyyy-mm-dd。cycle_type 为 1 时可为任意自然日；为 2 时须为周一；为 3 时须为当月第一天</para>
    /// <para>必填：是</para>
    /// <para>示例值：2022-08-01</para>
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// <para>统计周期类型。1：日；2：周；3：月</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("cycle_type")]
    public int? CycleType { get; set; }

    /// <summary>
    /// <para>查询的部门 ID，不传表示查看租户整体使用情况</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>应用能力类型。可选值：app（应用）、mp（小程序）、h5（网页应用）、bot（机器人）</para>
    /// <para>必填：否</para>
    /// <para>示例值：app</para>
    /// </summary>
    [JsonPropertyName("ability")]
    public string? Ability { get; set; }
}

/// <summary>
/// 获取消息推送概览请求体。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class MessagePushOverviewRequest
{
    /// <summary>
    /// <para>查询日期，格式 yyyy-mm-dd。cycle_type 为 1 时可为任意自然日；为 2 时须为周一；为 3 时须为当月第一天</para>
    /// <para>必填：是</para>
    /// <para>示例值：2022-08-01</para>
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// <para>统计周期类型。1：日；2：周；3：月</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("cycle_type")]
    public int? CycleType { get; set; }

    /// <summary>
    /// <para>查询的部门 ID，不传表示查看租户整体使用情况</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }
}

/// <summary>
/// 获取多部门应用使用概览请求体。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class DepartmentUsageOverviewRequest
{
    /// <summary>
    /// <para>查询日期，格式 yyyy-mm-dd。cycle_type 为 1 时可为任意自然日；为 2 时须为周一；为 3 时须为当月第一天</para>
    /// <para>必填：是</para>
    /// <para>示例值：2022-08-01</para>
    /// </summary>
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    /// <summary>
    /// <para>统计周期类型。1：日；2：周；3：月</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("cycle_type")]
    public int? CycleType { get; set; }

    /// <summary>
    /// <para>查询的部门 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>是否递归查询子部门数据。0：不递归；1：递归</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("recursion")]
    public int? Recursion { get; set; }

    /// <summary>
    /// <para>分页大小</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_size")]
    public int? PageSize { get; set; }

    /// <summary>
    /// <para>分页标记，第一次请求不填，表示从头开始遍历</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }
}
