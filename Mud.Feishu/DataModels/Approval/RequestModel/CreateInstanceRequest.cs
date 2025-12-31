// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 创建审批实例请求体
/// </summary>
public class CreateInstanceRequest
{
    /// <summary>
    /// <para>审批定义 Code。获取方式：</para>
    /// <para>必填：是</para>
    /// <para>示例值：7C468A54-8745-2245-9675-08B7C63E7A85</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批发起人的 user_id，与 open_id 必须传入其中一个。如果传入了 user_id 则优先使用 user_id。获取方式参考[如何获取用户的 User ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批发起人的 open_id，与 user_id 必须传入其中一个。如果传入了 user_id 则优先使用 user_id。获取方式参考[如何获取用户的 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_3cda9c969f737aaa05e6915dce306cb9</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>审批发起人所属部门 ID。如果用户只属于一个部门，可以不填。如果用户属于多个部门，不填值则默认选择部门列表第一个部门。获取方式参见[部门 ID](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview#9c02ed7a)。</para>
    /// <para>**说明**：</para>
    /// <para>- 不支持填写根部门。</para>
    /// <para>- 需填写 department_id 类型的部门 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：9293493ccacbdb9a</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>填写的审批表单控件值，JSON 数组，传值时需要压缩转义为字符串。各控件值的参数说明参考[审批实例表单控件参数](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/approval-v4/instance/approval-instance-form-control-parameters)。</para>
    /// <para>必填：是</para>
    /// <para>示例值：[{\"id\":\"111\", \"type\": \"input\", \"value\":\"test\"}]</para>
    /// </summary>
    [JsonPropertyName("form")]
    public string Form { get; set; } = string.Empty;

    /// <summary>
    /// <para>如果审批定义的流程中，有节点需要发起人自选审批人，则需要通过本参数填写对应节点的审批人（通过用户 user_id 指定审批人）。</para>
    /// <para>**说明**：如果同时传入了 node_approver_user_id_list、node_approver_open_id_list，则取两个参数的并集生效审批人。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("node_approver_user_id_list")]
    public NodeApprover[]? NodeApproverUserIdLists { get; set; }


    /// <summary>
    /// <para>如果审批定义的流程中，有节点需要发起人自选审批人，则需要通过本参数填写对应节点的审批人（通过用户 open_id 指定审批人）。</para>
    /// <para>**说明**：如果同时传入了 node_approver_user_id_list、node_approver_open_id_list，则取两个参数的并集生效审批人。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("node_approver_open_id_list")]
    public NodeApprover[]? NodeApproverOpenIdLists { get; set; }

    /// <summary>
    /// <para>如果审批定义的流程中，有节点需要发起人自选抄送人，则需要通过本参数填写对应节点的抄送人（通过用户 user_id 指定审批人）。</para>
    /// <para>**说明**：如果同时传入了 node_cc_user_id_list、node_cc_open_id_list，则取两个参数的并集生效抄送人。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// </summary>
    [JsonPropertyName("node_cc_user_id_list")]
    public NodeCc[]? NodeCcUserIdLists { get; set; }



    /// <summary>
    /// <para>如果审批定义的流程中，有节点需要发起人自选抄送人，则需要通过本参数填写对应节点的抄送人（通过用户 open_id 指定审批人）。</para>
    /// <para>**说明**：如果同时传入了 node_cc_user_id_list、node_cc_open_id_list，则取两个参数的并集生效抄送人。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：20</para>
    /// </summary>
    [JsonPropertyName("node_cc_open_id_list")]
    public NodeCc[]? NodeCcOpenIdLists { get; set; }

    /// <summary>
    /// <para>审批实例 uuid，用于幂等操作，单个企业内的唯一 key。同一个 uuid 只能用于创建一个审批实例，如果冲突则创建失败并返回错误码 60012 ，格式建议为 XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX，不区分大小写。</para>
    /// <para>必填：否</para>
    /// <para>示例值：7C468A54-8745-2245-9675-08B7C63E7A87</para>
    /// <para>最大长度：64</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// <para>是否配置 **提交** 按钮，适用于任务的审批人退回审批单据后，审批提交人可以在同一个审批实例内点击 **提交**，提交单据。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_resubmit")]
    public bool? AllowResubmit { get; set; }

    /// <summary>
    /// <para>是否配置 **再次提交** 按钮，适用于周期性提单场景，按照当前表单内容再次发起一个新审批实例。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("allow_submit_again")]
    public bool? AllowSubmitAgain { get; set; }

    /// <summary>
    /// <para>取消指定的 Bot 推送通知。可选值有：</para>
    /// <para>- 1：取消审批实例通过推送。</para>
    /// <para>- 2：取消审批实例拒绝推送。</para>
    /// <para>- 4：取消审批实例取消推送。</para>
    /// <para>支持同时取消多个 bot 推送通知。位运算，即如需取消 1 和 2 两种通知，则需要传入加和值 3。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("cancel_bot_notification")]
    public string? CancelBotNotification { get; set; }

    /// <summary>
    /// <para>是否禁止撤销审批实例</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("forbid_revoke")]
    public bool? ForbidRevoke { get; set; }

    /// <summary>
    /// <para>国际化文案。目前只支持为表单的单行、多行文本控件赋值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public I18nResource[]? I18nResources { get; set; }


    /// <summary>
    /// <para>审批实例的展示名称。如果填写了该参数，则审批列表中的审批名称使用该参数，如果不填该参数，则审批名称使用审批定义的名称。</para>
    /// <para>**说明**：这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），必须以 @i18n@ 开头，还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@1</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>审批详情页 title 展示模式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：如果审批定义和审批实例都有 title，则全部展示，通过竖线分割。</item>
    /// <item>1：如果审批定义和审批实例都有 title，只展示审批实例的 title。</item>
    /// </list></para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("title_display_method")]
    public int? TitleDisplayMethod { get; set; }

    /// <summary>
    /// <para>设置自动通过的节点。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10</para>
    /// </summary>
    [JsonPropertyName("node_auto_approval_list")]
    public NodeAutoApproval[]? NodeAutoApprovalLists { get; set; }

}
