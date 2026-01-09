// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>任务级别的快捷审批操作配置。</para>
/// <para>**注意**：快捷审批目前仅支持在飞书移动端操作。</para>
/// </summary>
public class ExternalActionConfig
{
    /// <summary>
    /// <para>操作类型。每个任务都可以配置两个操作（同意、拒绝或任意中的两个），操作会展示审批列表中。当用户操作时，回调请求会包含该字段，三方审批可接受到审批人的操作数据。</para>
    /// <para>必填：是</para>
    /// <para>示例值：APPROVE</para>
    /// <para>可选值：<list type="bullet">
    /// <item>APPROVE：同意</item>
    /// <item>REJECT：拒绝</item>
    /// <item>{KEY}：任意字符串。如果使用任意字符串，则需要提供 action_name</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("action_type")]
    public string ActionType { get; set; } = string.Empty;

    /// <summary>
    /// <para>操作名称。如果 action_type 不等于 APPROVAL 或 REJECT，则必须提供该字段，用于展示特定的操作名称。</para>
    /// <para>**说明**：</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- Key 需要以 @i18n@ 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@5</para>
    /// </summary>
    [JsonPropertyName("action_name")]
    public string? ActionName { get; set; }

    /// <summary>
    /// <para>是否需要审批意见。取值为 true 时，审批人在审批中心操作任务后，还需要跳转填写审批意见。</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_need_reason")]
    public bool? IsNeedReason { get; set; }

    /// <summary>
    /// <para>审批意见是否必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_reason_required")]
    public bool? IsReasonRequired { get; set; }

    /// <summary>
    /// <para>审批意见是否支持上传附件</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_need_attachment")]
    public bool? IsNeedAttachment { get; set; }
}