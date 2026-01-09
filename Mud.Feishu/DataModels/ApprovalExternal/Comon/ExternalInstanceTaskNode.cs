// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;


/// <summary>
/// <para>任务列表</para>
/// </summary>
public class ExternalInstanceTaskNode
{
    /// <summary>
    /// <para>审批实例内，审批任务的唯一标识，用于更新审批任务时定位数据。</para>
    /// <para>必填：是</para>
    /// <para>示例值：112534</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批人 user_id，获取方式参见[如何获取用户的 User ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)。</para>
    /// <para>**说明**：</para>
    /// <para>- 该任务会出现在审批人的飞书审批中心 **待办** 或 **已办** 的列表中。</para>
    /// <para>- user_id 与 open_id 需至少传入一个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：a987sf9s</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批人 open_id，获取方式参见[如何获取用户的 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)。</para>
    /// <para>**说明**：</para>
    /// <para>- 该任务会出现在审批人的飞书审批中心 **待办** 或 **已办** 的列表中。</para>
    /// <para>- user_id 与 open_id 需至少传入一个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_be73cbc0ee35eb6ca54e9e7cc14998c1</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>审批任务名称。</para>
    /// <para>**说明**：</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- Key 需要以 @i18n@ 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@4</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>在审批中心 **待办**、**已办** 中使用的三方审批跳转链接，用于跳转回三方审批系统查看任务详情。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("links")]
    public InstanceLink Links { get; set; } = new();

    /// <summary>
    /// <para>任务状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：PENDING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：待审批</item>
    /// <item>APPROVED：任务同意</item>
    /// <item>REJECTED：任务拒绝</item>
    /// <item>TRANSFERRED：任务转交</item>
    /// <item>DONE：任务通过但审批人未操作。审批人看不到该任务时，如需查看可抄送至该审批人。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>扩展字段。JSON 格式，传值时需要压缩转义为字符串。</para>
    /// <para>任务结束原因需传 complete_reason 参数，枚举值说明：</para>
    /// <para>- approved：同意</para>
    /// <para>- rejected：拒绝</para>
    /// <para>- node_auto_reject：因逻辑判断产生的自动拒绝</para>
    /// <para>- specific_rollback：退回（包括退回到发起人、退回到中间任一审批人）</para>
    /// <para>- add：并加签（添加新审批人，与我一起审批）</para>
    /// <para>- add_pre：前加签（添加新审批人，在我之前审批）</para>
    /// <para>- add_post：后加签（添加新审批人，在我之后审批）</para>
    /// <para>- delete_assignee：减签</para>
    /// <para>- forward: 手动转交</para>
    /// <para>- forward_resign：离职自动转交</para>
    /// <para>- recall：撤销（撤回单据，单据失效）</para>
    /// <para>- delete ：删除审批单</para>
    /// <para>- admin_forward：管理员在后台操作转交</para>
    /// <para>- system_forward：系统自动转交</para>
    /// <para>- auto_skip：自动通过</para>
    /// <para>- manual_skip：手动跳过</para>
    /// <para>- submit_again：重新提交任务</para>
    /// <para>- restart：重新启动流程</para>
    /// <para>- others：其他</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"xxx\":\"xxx\",\"complete_reason\":\"approved\"}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>任务创建时间，Unix 毫秒时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务完成时间。未结束的审批为 0，Unix 毫秒时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>任务最近更新时间，Unix 毫秒时间戳，用于推送数据版本控制。如果 update_mode 值为 UPDATE，则仅当传过来的 update_time 有变化时（变大），才会更新审批中心中的审批任务信息。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>操作上下文。当用户操作审批时，回调请求中会包含该参数，用于传递该任务的上下文数据。</para>
    /// <para>必填：否</para>
    /// <para>示例值：123456</para>
    /// </summary>
    [JsonPropertyName("action_context")]
    public string? ActionContext { get; set; }

    /// <summary>
    /// <para>任务级别的快捷审批操作配置。</para>
    /// <para>**注意**：快捷审批目前仅支持在飞书移动端操作。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action_configs")]
    public ExternalActionConfig[]? ActionConfigs { get; set; }



    /// <summary>
    /// <para>审批中心列表页打开审批任务的方式。</para>
    /// <para>必填：否</para>
    /// <para>示例值：BROWSER</para>
    /// <para>可选值：<list type="bullet">
    /// <item>BROWSER：跳转系统默认浏览器打开</item>
    /// <item>SIDEBAR：飞书中侧边抽屉打开</item>
    /// <item>NORMAL：飞书内嵌页面打开</item>
    /// <item>TRUSTEESHIP：以托管模式打开</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("display_method")]
    public string? DisplayMethod { get; set; }

    /// <summary>
    /// <para>三方审批任务是否不纳入效率统计。可选值有：</para>
    /// <para>- true：不纳入效率统计</para>
    /// <para>- false：纳入效率统计</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("exclude_statistics")]
    public bool? ExcludeStatistics { get; set; }

    /// <summary>
    /// <para>审批节点 ID。必须同时满足：</para>
    /// <para>- 一个审批流程内，每个节点 ID 唯一。例如，一个流程下直属上级、隔级上级等节点的 node_id 均不一样。</para>
    /// <para>- 同一个三方审批定义内，不同审批实例中的相同节点，node_id 要保持不变。例如，用户 A 和用户 B 分别发起了请假申请，这两个审批实例中的直属上级节点的 node_id 应该保持一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：node</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// <para>节点名称。</para>
    /// <para>**说明**：</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- Key 需要以 @i18n@ 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：i18n@name</para>
    /// </summary>
    [JsonPropertyName("node_name")]
    public string? NodeName { get; set; }

    /// <summary>
    /// <para>任务生成类型，可不填， 但是不要填空字符串</para>
    /// <para>必填：否</para>
    /// <para>示例值：EXTERNAL_CONSIGN</para>
    /// <para>可选值：<list type="bullet">
    /// <item>EXTERNAL_CONSIGN：给代理人生成的任务</item>
    /// <item>DEFAULT：系统生成的默认任务</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("generate_type")]
    public string? GenerateType { get; set; }
}
