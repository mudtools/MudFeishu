// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 创建审批定义请求体
/// </summary>
public class CreateApprovalRequest
{
    /// <summary>
    /// <para>审批名称的国际化文案 Key，以 `@i18n@` 开头，长度不得少于 9 个字符。</para>
    /// <para>必填：是</para>
    /// <para>示例值：@i18n@approval_name</para>
    /// </summary>
    [JsonPropertyName("approval_name")]
    public string ApprovalName { get; set; } = string.Empty;

    /// <summary>
    /// <para> 审批定义 Code。使用说明：</para>
    /// <para>- 该参数不传值时，表示新建审批定义，最终响应结果会返回由系统自动生成的审批定义 Code。</para>
    /// <para>- 该参数传入指定审批定义 Code 时，表示调用该接口更新该审批定义内容，更新方式为覆盖原定义内容的全量更新。</para>
    /// <para>审批定义 Code。获取方式：</para>
    /// <para>必填：否</para>
    /// <para>示例值：7C468A54-8745-2245-9675-08B7C63E7A85</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// <para>审批描述的国际化文案 Key，以 `@i18n@` 开头，长度不得少于 9 个字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@description</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>viewers 字段指定了哪些人能从审批应用的前台发起该审批。使用说明：</para>
    /// <para>- 当 viewer_type 为 USER，需要填写 viewer_user_id</para>
    /// <para>- 当 viewer_type 为 DEPARTMENT，需要填写 viewer_department_id</para>
    /// <para>- 当 viewer_type 为 TENANT 或 NONE 时，无需填写 viewer_user_id 和 viewer_department_id</para>
    /// <para>**注意**：列表最大长度为 200。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("viewers")]
    public ApprovalCreateViewers[] Viewers { get; set; } = [];


    /// <summary>
    /// <para>审批定义表单</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("form")]
    public ApprovalForm Form { get; set; } = new();



    /// <summary>
    /// <para>审批定义节点列表，用于设置审批流程所需要的各个节点，审批流程的始末固定为开始节点和结束节点，因此传值时需要将开始节点作为 list 第一个元素，结束节点作为 list 最后一个元素。</para>
    /// <para>**说明**：API 方式不支持设置条件分支，如需设置条件分支请前往[飞书审批后台](https://www.feishu.cn/approval/admin/approvalList?devMode=on)创建审批定义。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("node_list")]
    public ApprovalNode[] NodeLists { get; set; } = [];


    /// <summary>
    /// <para>审批定义其他设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("settings")]
    public ApprovalSetting? Settings { get; set; }


    /// <summary>
    /// <para>审批定义配置项，用于配置对应审批定义是否可以由用户在[审批后台]进行修改。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("config")]
    public ApprovalConfig? Config { get; set; }

    /// <summary>
    /// <para>审批图标枚举，默认为 0。下图从左至右，从上到下依次为 0~24 号图标。</para>
    /// <para>必填：否</para>
    /// <para>示例值：0</para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("icon")]
    public int? Icon { get; set; }

    /// <summary>
    /// <para>国际化文案</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public I18nResource[] I18nResources { get; set; } = [];

    /// <summary>
    /// <para>审批流程管理员的用户 ID 列表。</para>
    /// <para>- ID 类型与查询参数 user_id_type 取值一致</para>
    /// <para>- 列表最大长度为 200</para>
    /// <para>必填：否</para>
    /// <para>示例值：["1c5ea995"]</para>
    /// </summary>
    [JsonPropertyName("process_manager_ids")]
    public string[]? ProcessManagerIds { get; set; }
}
