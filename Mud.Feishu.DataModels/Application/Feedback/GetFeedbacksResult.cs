// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 获取应用反馈列表响应体数据。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class GetFeedbacksResult
{
    /// <summary>
    /// <para>应用反馈列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_list")]
    public List<FeedbackItem>? FeedbackList { get; set; }

    /// <summary>
    /// <para>是否还有更多项</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

    /// <summary>
    /// <para>分页标记，当 has_more 为 true 时，会同时返回新的 page_token，否则不返回 page_token</para>
    /// <para>必填：否</para>
    /// <para>示例值：7064688334618378259</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }
}

/// <summary>
/// 应用反馈条目。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class FeedbackItem
{
    /// <summary>
    /// <para>反馈 ID，唯一标识</para>
    /// <para>必填：是</para>
    /// <para>示例值：7057888018203574291</para>
    /// </summary>
    [JsonPropertyName("feedback_id")]
    public string? FeedbackId { get; set; }

    /// <summary>
    /// <para>应用 App ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </summary>
    [JsonPropertyName("app_id")]
    public string? AppId { get; set; }

    /// <summary>
    /// <para>反馈时间</para>
    /// <para>必填：是</para>
    /// <para>示例值：2022-01-30 11:30:12</para>
    /// </summary>
    [JsonPropertyName("feedback_time")]
    public string? FeedbackTime { get; set; }

    /// <summary>
    /// <para>反馈所属租户名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：bytedance</para>
    /// </summary>
    [JsonPropertyName("tenant_name")]
    public string? TenantName { get; set; }

    /// <summary>
    /// <para>反馈类型。1：故障反馈；2：产品建议</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("feedback_type")]
    public int? FeedbackType { get; set; }

    /// <summary>
    /// <para>反馈处理状态。0：未处理；1：已处理；2：处理中；3：已关闭</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }

    /// <summary>
    /// <para>故障类型列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：[1, 2, 3]</para>
    /// </summary>
    [JsonPropertyName("fault_type")]
    public List<int>? FaultType { get; set; }

    /// <summary>
    /// <para>故障发生时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022-01-30 11:30:12</para>
    /// </summary>
    [JsonPropertyName("fault_time")]
    public string? FaultTime { get; set; }

    /// <summary>
    /// <para>反馈来源</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("source")]
    public int? Source { get; set; }

    /// <summary>
    /// <para>反馈者联系方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：wang@bytedance.com</para>
    /// </summary>
    [JsonPropertyName("contact")]
    public string? Contact { get; set; }

    /// <summary>
    /// <para>反馈处理时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：2022-01-30 11:30:12</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>反馈描述</para>
    /// <para>必填：是</para>
    /// <para>示例值：反馈描述</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>反馈者的用户 ID，类型由 user_id_type 指定</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_9565b69967831233761cc2f11b4c089f</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>反馈处理人的用户 ID，类型由 user_id_type 指定</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// <para>反馈图片链接列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("images")]
    public List<string>? Images { get; set; }

    /// <summary>
    /// <para>反馈页面路径</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feedback_path")]
    public string? FeedbackPath { get; set; }
}
