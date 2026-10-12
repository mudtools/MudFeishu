// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 查询应用租户下的付费订单响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetPaidOrdersResult
{
    /// <summary>
    /// <para>总订单数</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("total")]
    public int? Total { get; set; }

    /// <summary>
    /// <para>是否还有更多数据。true：还有；false：没有</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }

    /// <summary>
    /// <para>下一页数据的标识，has_more 为 false 时该字段为空</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>订单信息列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("order_list")]
    public List<PaidOrder>? OrderList { get; set; }
}

/// <summary>
/// 应用付费订单信息。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class PaidOrder
{
    /// <summary>
    /// <para>订单 ID，唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：6708978506916697671</para>
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// <para>价格方案 ID，唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：price_9daf66c96968c003</para>
    /// </summary>
    [JsonPropertyName("price_plan_id")]
    public string? PricePlanId { get; set; }

    /// <summary>
    /// <para>价格方案类型。可选值：trial（试用）、permanent（免费）、two_year（两年固定付费）、per_year（企业年付费）、per_month（企业月付费）、per_seat_two_year（按人按两年付费）、per_seat_per_year（按人按年付费）、per_seat_per_month（按人按月付费）、permanent_count（按次付费）</para>
    /// <para>必填：否</para>
    /// <para>示例值：per_seat_per_year</para>
    /// </summary>
    [JsonPropertyName("price_plan_type")]
    public string? PricePlanType { get; set; }

    /// <summary>
    /// <para>实际购买人数，仅 price_plan_type 为 per_seat_per_year 或 per_seat_per_month 时有效</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// </summary>
    [JsonPropertyName("seats")]
    public int? Seats { get; set; }

    /// <summary>
    /// <para>购买数量，总是为 1</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("buy_count")]
    public int? BuyCount { get; set; }

    /// <summary>
    /// <para>订单创建时间戳（秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1565003215</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>订单支付时间戳（秒）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1565003216</para>
    /// </summary>
    [JsonPropertyName("pay_time")]
    public string? PayTime { get; set; }

    /// <summary>
    /// <para>订单当前状态。normal：正常；refund：已退款</para>
    /// <para>必填：否</para>
    /// <para>示例值：normal</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>购买类型。buy：普通购买；upgrade：升级购买；renew：续费购买</para>
    /// <para>必填：否</para>
    /// <para>示例值：buy</para>
    /// </summary>
    [JsonPropertyName("buy_type")]
    public string? BuyType { get; set; }

    /// <summary>
    /// <para>源订单 ID，当前订单为升级购买（buy_type 为 upgrade）时记录源订单的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("src_order_id")]
    public string? SrcOrderId { get; set; }

    /// <summary>
    /// <para>升级后的新订单 ID，当前订单做过升级购买时记录升级后生成的新订单 ID，当前订单仍然有效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dst_order_id")]
    public string? DstOrderId { get; set; }

    /// <summary>
    /// <para>订单实际支付金额，单位：分</para>
    /// <para>必填：否</para>
    /// <para>示例值：100</para>
    /// </summary>
    [JsonPropertyName("order_pay_price")]
    public int? OrderPayPrice { get; set; }

    /// <summary>
    /// <para>租户唯一标识</para>
    /// <para>必填：否</para>
    /// <para>示例值：2e5c3a3ae38f175f</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }
}

/// <summary>
/// 查询订单详情响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetOrderResult
{
    /// <summary>
    /// <para>订单信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("order")]
    public PaidOrder? Order { get; set; }
}
