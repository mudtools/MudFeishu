// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalMessage;

/// <summary>
/// 更新审批 Bot 消息 请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class ApprovalBotMessageUpdateRequest
{
    /// <summary>
    /// <para>待更新的审批 Bot 消息 ID。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; } = string.Empty;

    /// <summary>
    /// <para>状态类型，用于更新消息内第一个 `action` 的文字内容。可选值有：</para>
    /// <para>- APPROVED：已同意</para>
    /// <para>- REJECTED：已拒绝</para>
    /// <para>- CANCELLED：已撤回</para>
    /// <para>- FORWARDED：已转交</para>
    /// <para>- ROLLBACK：已回退</para>
    /// <para>- ADD：已加签</para>
    /// <para>- DELETED：已删除</para>
    /// <para>- PROCESSED：已处理</para>
    /// <para>- CUSTOM：自定义按钮状态</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>当 status 取值 CUSTOM 时，可以自定义审批同意或拒绝后 title 内容。</para>
    /// <para>**注意**:</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key：Value 格式进行赋值。</para>
    /// <para>- Key 需要以 `@i18n@` 开头。</para>
    /// <para>**示例值**：@i18n@1</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status_name")]
    public string? StatusName { get; set; }

    /// <summary>
    /// <para>当 status 取值 CUSTOM 时，可以自定义审批同意或拒绝后 **查看详情** 按钮名称。</para>
    /// <para>**注意**:</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key：Value 格式进行赋值。</para>
    /// <para>- Key 需要以 `@i18n@` 开头。</para>
    /// <para>**示例值**：@i18n@2</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("detail_action_name")]
    public string? DetailActionName { get; set; }

    /// <summary>
    /// <para>国际化文案。status_name、detail_action_name 参数设置了国际化文案 Key 后，需要通过 i18n_resources 设置 Key：value 关系为参数赋值。</para>
    /// <para>例如，status_name取值为 @i18n@1，则需要在 i18n_resources.texts 中传入 `@i18n@1： 已废弃` 为参数赋值。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public I18nDictResource[]? I18nResources { get; set; }

}
