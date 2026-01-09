// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// <para>同步的实例数据</para>
/// </summary>
public class ExternalInstance
{
    /// <summary>
    /// <para>审批定义 Code</para>
    /// <para>必填：是</para>
    /// <para>示例值：81D31358-93AF-92D6-7425-01A5D67C4E71</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例状态</para>
    /// <para>必填：是</para>
    /// <para>示例值：PENDING</para>
    /// <para>可选值：<list type="bullet">
    /// <item>PENDING：审批中</item>
    /// <item>APPROVED：审批流程结束，结果为同意</item>
    /// <item>REJECTED：审批流程结束，结果为拒绝</item>
    /// <item>CANCELED：审批发起人撤回</item>
    /// <item>DELETED：审批被删除</item>
    /// <item>HIDDEN：状态隐藏（不显示状态）</item>
    /// <item>TERMINATED：审批终止</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例扩展 JSON。单据编号通过传 business_key 字段来实现。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"xxx\":\"xxx\",\"business_key\":\"xxx\"}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>审批实例唯一标识，与请求时传入的 instance_id 一致。</para>
    /// <para>必填：是</para>
    /// <para>示例值：24492654</para>
    /// </summary>
    [JsonPropertyName("instance_id")]
    public string InstanceId { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例链接信息。设置的链接用于在审批中心 **已发起** 列表内点击跳转，跳回三方审批系统查看审批详情。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("links")]
    public ExternalInstanceLink Links { get; set; } = new();

    /// <summary>
    /// <para>审批展示名称。</para>
    /// <para>**说明**：</para>
    /// <para>- 如果请求时传入了 title 参数，则审批列表中的审批名称使用该参数值。如果请求时未传入 title 参数，则审批名称使用审批定义的名称。</para>
    /// <para>- 这里返回的是 i18n_resources.texts 参数的 key，对应的取值需要参见返回的 i18n_resources.texts.value。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@1</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>用户提交审批时填写的表单数据，用于所有审批列表中展示。可传多个值，最多展示前 3 个。</para>
    /// <para>必填：否</para>
    /// <para>示例值：[{ "name": "@i18n@2", "value": "@i18n@3" }]</para>
    /// </summary>
    [JsonPropertyName("form")]
    public ExternalInstanceForm[]? Forms { get; set; }


    /// <summary>
    /// <para>审批发起人 user_id。发起人可在审批中心的 **已发起** 列表中看到所有已发起的审批。在 **待办**、**已办**、**抄送我** 列表中，该字段用来展示审批的发起人。</para>
    /// <para>必填：否</para>
    /// <para>示例值：a987sf9s</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>审批发起人的用户名。如果发起人不是真实的用户（例如是某个部门），没有 user_id，则可以使用该参数传入一个名称。</para>
    /// <para>**说明**：这里返回的是 i18n_resources.texts 参数的 key，对应的取值需要参见返回的 i18n_resources.texts.value。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@9</para>
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// <para>审批发起人 open_id。发起人可在审批中心的 **已发起** 列表中看到所有已发起的审批。在 **待办**、**已办**、**抄送我** 列表中，该字段用来展示审批的发起人。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_be73cbc0ee35eb6ca54e9e7cc14998c1</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>发起人的部门 ID，用于在审批中心列表中展示发起人的所属部门，不传值则不展示。</para>
    /// <para>**说明**：如果用户没加入任何部门，请求时传 `""` 默认展示企业名称。如果请求时传入 department_name 参数，则展示对应的部门名称。</para>
    /// <para>必填：否</para>
    /// <para>示例值：od-8ec33278bc2</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }

    /// <summary>
    /// <para>审批发起人的部门名称。如果发起人不是真实的用户或没有部门，则可以使用该参数传入部门名称。</para>
    /// <para>**说明**：这里返回的是 i18n_resources.texts 参数的 key，对应的取值需要参见返回的 i18n_resources.texts.value。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@10</para>
    /// </summary>
    [JsonPropertyName("department_name")]
    public string? DepartmentName { get; set; }

    /// <summary>
    /// <para>审批发起时间，Unix 毫秒时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例结束时间。未结束的审批为 0，Unix 毫秒时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批实例最近更新时间，Unix 毫秒时间戳，用于推送数据版本控制。如果 update_mode 值为 UPDATE，则仅当传过来的 update_time 有变化时（变大），才会更新审批中心中的审批实例信息。</para>
    /// <para>**说明**：使用该参数主要用来避免并发时，旧数据更新了新数据。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1556468012678</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string UpdateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>列表页打开审批实例的方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：BROWSER</para>
    /// <para>可选值：<list type="bullet">
    /// <item>BROWSER：跳转系统默认浏览器打开</item>
    /// <item>SIDEBAR：飞书中侧边抽屉打开</item>
    /// <item>NORMAL：飞书内嵌页面打开</item>
    /// <item>TRUSTEESHIP：以托管打开</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("display_method")]
    public string? DisplayMethod { get; set; }

    /// <summary>
    /// <para>更新方式。</para>
    /// <para>- 当 update_mode 取值为 REPLACE 时，每次都以当前推送的数据为最终数据，会删掉审批中心中，不在本次推送数据中的多余的任务、抄送数据。</para>
    /// <para>- 当 update_mode 取值为 UPDATE 时，不会删除审批中心的数据，而只进行新增、更新实例与任务数据。</para>
    /// <para>必填：否</para>
    /// <para>示例值：UPDATE</para>
    /// <para>可选值：<list type="bullet">
    /// <item>REPLACE：全量替换</item>
    /// <item>UPDATE：增量更新</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("update_mode")]
    public string? UpdateMode { get; set; }

    /// <summary>
    /// <para>任务列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：300</para>
    /// </summary>
    [JsonPropertyName("task_list")]
    public ExternalInstanceTaskNode[]? TaskLists { get; set; }

    /// <summary>
    /// <para>抄送列表</para>
    /// <para>必填：否</para>
    /// <para>最大长度：200</para>
    /// </summary>
    [JsonPropertyName("cc_list")]
    public ExternalCcNode[]? CcLists { get; set; }

    /// <summary>
    /// <para>国际化文案</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public I18nResource[] I18nResources { get; set; } = [];

    /// <summary>
    /// <para>单据托管认证 token，托管回调会附带此 token，帮助业务认证。</para>
    /// <para>必填：否</para>
    /// <para>示例值：788981c886b1c28ac29d1e68efd60683d6d90dfce80938ee9453e2a5f3e9e306</para>
    /// </summary>
    [JsonPropertyName("trusteeship_url_token")]
    public string? TrusteeshipUrlToken { get; set; }

    /// <summary>
    /// <para>用户的类型，会影响请求参数用户标识域的选择，包括加签操作回传的目标用户， 目前仅支持 user_id。</para>
    /// <para>必填：否</para>
    /// <para>示例值：user_id</para>
    /// </summary>
    [JsonPropertyName("trusteeship_user_id_type")]
    public string? TrusteeshipUserIdType { get; set; }

    /// <summary>
    /// <para>单据托管回调接入方的接口 URL 地址</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trusteeship_urls")]
    public ExternalInstancesTrusteeshipUrls? TrusteeshipUrls { get; set; }

    /// <summary>
    /// <para>托管预缓存策略</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("trusteeship_cache_config")]
    public ExternalTrusteeshipInstanceCacheConfig? TrusteeshipCacheConfig { get; set; }

    /// <summary>
    /// <para>资源所在地区， 内部统计用字段， 不需要填</para>
    /// <para>必填：否</para>
    /// <para>示例值：cn</para>
    /// </summary>
    [JsonPropertyName("resource_region")]
    public string? ResourceRegion { get; set; }
}
