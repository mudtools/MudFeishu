// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 背调订单信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BackgroundCheckOrder
{
    /// <summary>
    /// <para>背调订单 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("order_id")]
    public string? OrderId { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>背调订单状态：2 已安排 / 3 已完成 / 4 已终止 / 5 审批中 / 6 审批已撤回 / 8 审批通过 / 9 审批未通过</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("order_status")]
    public int? OrderStatus { get; set; }

    /// <summary>
    /// <para>背调供应商类型：1 八方锦程 / 2 i背调 / 3 轩渡 / 127 自定义供应商</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("account_third_type")]
    public int? AccountThirdType { get; set; }

    /// <summary>
    /// <para>背调套餐名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("package")]
    public string? Package { get; set; }

    /// <summary>
    /// <para>背调名称（仅手动录入的背调有值）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>背调报告列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_info_list")]
    public BackgroundCheckOrderFeedbackInfo[]? FeedbackInfoList { get; set; }

    /// <summary>
    /// <para>背调进度列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_info_list")]
    public BackgroundCheckOrderProcessInfo[]? ProcessInfoList { get; set; }

    /// <summary>
    /// <para>录入时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("upload_time")]
    public string? UploadTime { get; set; }

    /// <summary>
    /// <para>候选人联系信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("candidate_info")]
    public BackgroundCheckUserContactInfo? CandidateInfo { get; set; }

    /// <summary>
    /// <para>背调发起人信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("creator_info")]
    public BackgroundCheckOrderCreator? CreatorInfo { get; set; }

    /// <summary>
    /// <para>背调联系人信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("contactor_info")]
    public BackgroundCheckUserContactInfo? ContactorInfo { get; set; }

    /// <summary>
    /// <para>背调开始时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("begin_time")]
    public string? BeginTime { get; set; }

    /// <summary>
    /// <para>背调结束时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }

    /// <summary>
    /// <para>背调结论</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    /// <summary>
    /// <para>供应商信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("provider_info")]
    public BackgroundCheckProviderInfo? ProviderInfo { get; set; }

    /// <summary>
    /// <para>自定义字段模板列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_field_list")]
    public BackgroundCheckCustomFieldData[]? CustomFieldList { get; set; }

    /// <summary>
    /// <para>自定义字段值列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_data_list")]
    public BackgroundCheckCustomFieldValue[]? CustomDataList { get; set; }

    /// <summary>
    /// <para>背调调查附加项列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ext_item_info_list")]
    public BackgroundCheckItemInfo[]? ExtItemInfoList { get; set; }

    /// <summary>
    /// <para>更新时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>背调适用地区：cn 中国大陆 / sg 新加坡 / us 美东 / jp 日本</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("geo")]
    public string? Geo { get; set; }

    /// <summary>
    /// <para>预计入职地点编码</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("location_code")]
    public string? LocationCode { get; set; }

    /// <summary>
    /// <para>备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("remark")]
    public string? Remark { get; set; }
}
