// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 查询背调信息列表请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BatchQueryBackgroundCheckOrderRequest
{
    /// <summary>
    /// <para>背调订单 ID 列表，最多 20 个；传入后其余查询字段失效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("background_check_order_id_list")]
    public string[]? BackgroundCheckOrderIdList { get; set; }

    /// <summary>
    /// <para>最早更新时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_start_time")]
    public string? UpdateStartTime { get; set; }

    /// <summary>
    /// <para>最晚更新时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_end_time")]
    public string? UpdateEndTime { get; set; }

    /// <summary>
    /// <para>最早创建时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("begin_start_time")]
    public string? BeginStartTime { get; set; }

    /// <summary>
    /// <para>最晚创建时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("begin_end_time")]
    public string? BeginEndTime { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>背调订单状态（字符串形式）："2" 已安排 / "3" 已完成 / "4" 已终止 / "5" 审批中 / "6" 审批已撤回 / "8" 审批通过 / "9" 审批未通过</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("order_status")]
    public string? OrderStatus { get; set; }
}
