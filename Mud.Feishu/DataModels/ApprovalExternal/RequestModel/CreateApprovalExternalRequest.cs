// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// 创建三方审批定义请求体
/// </summary>
public class CreateApprovalExternalRequest
{
    /// <summary>
    /// <para>三方审批定义名称。</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- 该参数需要以 @i18n@ 开头，长度不得少于 9 个字符。</para>
    /// <para>必填：是</para>
    /// <para>示例值：@i18n@demoname</para>
    /// </summary>
    [JsonPropertyName("approval_name")]
    public string ApprovalName { get; set; } = string.Empty;

    /// <summary>
    /// <para>应用自定义Code，最大支持128字符，用于唯一关联三方审批定义，具体说明：</para>
    /// <para>- 如果传入的值系统可以匹配到已存在的审批定义 approval_code，则调用该接口会更新相应的审批定义。</para>
    /// <para>- 如果传入的值系统匹配不到任何审批定义 approval_code，则会新建一个审批定义，并返回新建的审批定义真实的 approval_code（并非通过该参数传入的值）。</para>
    /// <para>必填：是</para>
    /// <para>示例值：F46EB460-9476-4789-9524-ECD564291234</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批定义所属审批分组，用户自定义。具体说明：</para>
    /// <para>- 如果传入的 group_code 当前不存在，则会新建审批分组。</para>
    /// <para>- 如果 group_code 已经存在，则会使用 group_name 更新审批分组名称。</para>
    /// <para>- 更新审批定义时可以不传该字段，会继续使用当前绑定的分组。</para>
    /// <para>必填：否</para>
    /// <para>示例值：work_group</para>
    /// </summary>
    [JsonPropertyName("group_code")]
    public string? GroupCode { get; set; }

    /// <summary>
    /// <para>审批分组名称，审批发起页的审批定义分组名称来自该字段。具体说明：</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- 该参数需要以 @i18n@ 开头。</para>
    /// <para>- 如果 group_code 当前不存在，则该 group_name 必填，表示新建审批分组时设置分组名称。</para>
    /// <para>- 如果 group_code 存在，则会更新分组名称，不填则不更新分组名称。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@2</para>
    /// </summary>
    [JsonPropertyName("group_name")]
    public string? GroupName { get; set; }

    /// <summary>
    /// <para>审批定义的说明，后续企业员工发起审批时，该说明会在审批发起页展示。</para>
    /// <para>- 这里传入的是国际化文案 Key（即 i18n_resources.texts 参数中的 Key），还需要在 i18n_resources.texts 参数中以 Key:Value 格式进行赋值。</para>
    /// <para>- 该参数需要以 @i18n@ 开头。</para>
    /// <para>必填：否</para>
    /// <para>示例值：@i18n@2</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>三方审批相关信息。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("external")]
    public ApprovalCreateExternal External { get; set; } = new();

    /// <summary>
    /// <para>审批可见人列表，列表长度上限 200，只有在审批可见人列表内的用户，才可以在审批发起页看到该审批。若该参数不传值，则表示任何人不可见。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("viewers")]
    public ApprovalCreateViewers[]? Viewers { get; set; } = [];

    /// <summary>
    /// <para>国际化文案</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public I18nResource[]? I18nResources { get; set; }

    /// <summary>
    /// <para>设置审批流程管理员的用户 ID，最多支持设置 200 个。ID 类型与查询参数 user_id_type 取值一致。</para>
    /// <para>必填：否</para>
    /// <para>示例值：19a294c2</para>
    /// </summary>
    [JsonPropertyName("managers")]
    public string[]? Managers { get; set; }
}
