// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalMessage;

/// <summary>
/// 快捷审批的操作配置。
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class ActionConfig
{
    /// <summary>
    /// 操作类型。可选值有：
    /// <list type="bullet">
    /// <item>APPROVE：同意</item>
    /// <item>REJECT：拒绝</item>
    /// <item>KEY：任意英文字符串，设置该值时，需要设置 action_name 参数。</item>
    /// </list>
    /// </summary>
    [JsonPropertyName("action_type")]
    public string ActionType { get; set; } = "APPROVE";

    /// <summary>
    /// 操作名称。
    /// </summary>
    [JsonPropertyName("action_name")]
    public string? ActionName { get; set; }

    /// <summary>
    /// 是否需要填写审核意见。可选值有：true：需要、false：不需要
    /// </summary>
    [JsonPropertyName("is_need_reason")]
    public bool? IsNeedReason { get; set; }

    /// <summary>
    /// 意见是否为必填。
    /// </summary>
    [JsonPropertyName("is_reason_required")]
    public bool? IsReasonRequired { get; set; }

    /// <summary>
    /// 意见是否支持上传附件。
    /// </summary>
    [JsonPropertyName("is_need_attachment")]
    public bool? IsNeedAttachment { get; set; }

    /// <summary>
    /// 如果回调成功后，审批 Bot 消息会更新成的状态。如果指定则飞书审批会在用户操作后，把消息卡片的状态更新为 {next_status}。如果不指定则飞书审批不会主动更新消息卡片，需自行更新卡片。
    /// <para>示例值：APPROVED</para>
    /// </summary>
    [JsonPropertyName("next_status")]
    public string? NextStatus { get; set; }
}
