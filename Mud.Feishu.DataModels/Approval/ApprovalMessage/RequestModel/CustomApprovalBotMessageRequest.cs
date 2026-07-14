// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalMessage;

/// <summary>
/// 发送审批 Bot 消息自定义模板请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class CustomApprovalBotMessageRequest
{
    /// <summary>
    /// 模板编号，自定义模板编号为 1021。
    /// </summary>
    [JsonPropertyName("template_id")]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// 接收审批 Bot 消息的目标用户的 user_id。示例值：b85s39b
    /// <para>注意：user_id 和 open_id 需至少传入一个。</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// 接收审批 Bot 消息的目标用户的 open_id。示例值：b85s39b
    /// <para>注意：user_id 和 open_id 需至少传入一个。</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    /// 自定义的幂等 ID，最大长度为 64。你可以传入唯一的 UUID 以保证审批 Bot 消息只发送一次。示例值：1234567
    /// <para>说明：UUID 相同的请求，1 小时内只会发送一次审批 Bot 消息。</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// 模板标题。
    /// <para>这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key：Value 格式进行赋值。</para>
    /// </summary>
    [JsonPropertyName("custom_title")]
    public string CustomTitle { get; set; } = string.Empty;

    /// <summary>
    /// 模板内容。
    /// <para>这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key：Value 格式进行赋值。</para>
    /// </summary>
    [JsonPropertyName("custom_content")]
    public string CustomContent { get; set; } = string.Empty;

    /// <summary>
    /// 备注。内容用于标注审批来源、访问限制等信息。
    /// <para>这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key：Value 格式进行赋值。</para>
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>
    /// 操作区，用于设置操作按钮，最多可设置 2 个按钮。
    /// </summary>
    [JsonPropertyName("actions")]
    public List<ActionItem> Actions { get; set; } = [];

    /// <summary>
    /// 国际化文案。
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public List<I18nDictResource> I18nResources { get; set; } = [];
}
