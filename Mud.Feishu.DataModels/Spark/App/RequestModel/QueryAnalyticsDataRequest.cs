// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spark;

/// <summary>
/// 获取妙搭应用运营数据趋势请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Spark")]
public class QueryAnalyticsDataRequest
{
    /// <summary>
    /// <para>指标名列表，长度范围 1～20</para>
    /// <para>必填：是</para>
    /// <para>示例值：["TOTAL_USER", "ACTIVE_USER", "NEW_USER", "PAGE_VIEW"]</para>
    /// <para>可选值：<list type="bullet">
    /// <item>TOTAL_USER：累计用户数</item>
    /// <item>ACTIVE_USER：活跃用户数</item>
    /// <item>NEW_USER：新增用户数</item>
    /// <item>PAGE_VIEW：页面访问数</item>
    /// <item>API_REQUEST：API 请求数（暂不支持）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("metric_types")]
    public string[] MetricTypes { get; set; } = [];

    /// <summary>
    /// <para>起始时间戳，单位：纳秒，长度 1～20 字符</para>
    /// <para>必填：是</para>
    /// <para>示例值：1782132931498000000</para>
    /// </summary>
    [JsonPropertyName("start_timestamp_ns")]
    public string StartTimestampNs { get; set; } = string.Empty;

    /// <summary>
    /// <para>结束时间戳，单位：纳秒，长度 1～20 字符</para>
    /// <para>必填：是</para>
    /// <para>示例值：1782132931498000000</para>
    /// </summary>
    [JsonPropertyName("end_timestamp_ns")]
    public string EndTimestampNs { get; set; } = string.Empty;

    /// <summary>
    /// <para>时间聚合单元</para>
    /// <para>必填：是</para>
    /// <para>示例值：DAY</para>
    /// <para>可选值：DAY（天）、WEEK（周）、MONTH（月）</para>
    /// </summary>
    [JsonPropertyName("time_aggregation_unit")]
    public string TimeAggregationUnit { get; set; } = string.Empty;

    /// <summary>
    /// <para>其他过滤条件，key 为匹配字段名称（所有指标共享）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("filter")]
    public AnalyticsFilter? Filter { get; set; }

    /// <summary>
    /// <para>页面路径，长度 1～10000 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：/home</para>
    /// </summary>
    [JsonPropertyName("page")]
    public string? Page { get; set; }

    /// <summary>
    /// <para>终端类型，长度范围 1～10</para>
    /// <para>必填：否</para>
    /// <para>示例值：["mobile"]</para>
    /// </summary>
    [JsonPropertyName("device_types")]
    public string[]? DeviceTypes { get; set; }

    /// <summary>
    /// <para>是否需要补点：true 时对缺失的时间点补 0；false 时只返回原始数据点</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("need_pack_lack_point")]
    public bool? NeedPackLackPoint { get; set; }

    /// <summary>
    /// <para>按字段聚合，当前只支持 device_type，长度 1～1000 字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：device_type</para>
    /// </summary>
    [JsonPropertyName("group_by")]
    public string? GroupBy { get; set; }
}
