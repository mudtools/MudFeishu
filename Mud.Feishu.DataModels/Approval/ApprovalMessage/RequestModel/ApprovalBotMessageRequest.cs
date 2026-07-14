// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;

namespace Mud.Feishu.DataModels.ApprovalMessage;

/// <summary>
/// 发送审批 Bot 消息通用模板请求体
/// </summary>
public class ApprovalBotMessageRequest
{
    /// <summary>
    /// 模板编号，具体参考模板列表的 模板编号 列。
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
    /// 对应模板标题的 {approval_name} 参数。
    /// </summary>
    [JsonPropertyName("approval_name")]
    public string? ApprovalName { get; set; }

    /// <summary>
    /// 对应模板标题的 {title_user_id}，用来指定审批的申请人、审批人、评论人或者抄送人等。
    /// <para>需传入用户 ID，ID 类型与 title_user_id_type 取值保持一致。</para>
    /// </summary>
    [JsonPropertyName("title_user_id")]
    public string? TitleUserId { get; set; }

    /// <summary>
    /// 指定 title_user_id 传入的用户 ID 类型。可选值有：user_id、open_id
    /// </summary>
    [JsonPropertyName("title_user_id_type")]
    public string TitleUserIdType { get; set; } = Consts.User_Id_Type;

    /// <summary>
    /// 评论区内容。
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// 审批 Bot 消息的内容。当模板的内容存在 {user_id}、{department_id} 或 {summaries} 等参数时，可以通过当前参数配置对应的参数值。
    /// </summary>
    [JsonPropertyName("content")]
    public ContentData? Content { get; set; }

    /// <summary>
    /// 备注。内容用于标注审批来源、访问限制等信息。
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>
    /// 操作区，最多可设置 2 个操作按钮。
    /// </summary>
    [JsonPropertyName("actions")]
    public List<ActionItem> Actions { get; set; } = [];

    /// <summary>
    /// 快捷审批的操作配置。
    /// <para>说明：仅样式为橘黄色的模板支持快捷审批参数。</para>
    /// </summary>
    [JsonPropertyName("action_configs")]
    public List<ActionConfig>? ActionConfigs { get; set; }

    /// <summary>
    /// 快捷审批的回调配置。
    /// <para>说明：仅样式为橘黄色的模板支持快捷审批参数。</para>
    /// </summary>
    [JsonPropertyName("action_callback")]
    public ActionCallback? ActionCallback { get; set; }

    /// <summary>
    /// 国际化文案。
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public List<I18nDictResource> I18nResources { get; set; } = [];
}
