// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalExternal;

/// <summary>
/// 查看指定三方审批定义响应体
/// </summary>
public class GetApprovalExternalResult
{
    /// <summary>
    /// <para>审批定义名称。当前参数返回的是 @i18n@ 开头的 key，需要通过 i18n_resources.texts 参数值查阅当前 key 对应的取值（value）。</para>
    /// <para>示例值：@i18n@1</para>
    /// </summary>
    [JsonPropertyName("approval_name")]
    public string ApprovalName { get; set; } = string.Empty;

    /// <summary>
    /// <para>创建三方审批定义时传入的 approval_code。</para>
    /// <para>**注意**：[创建三方审批定义](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/approval-v4/external_approval/create)接口的请求参数 approval_code 与响应参数 approval_code 不一定相同，当前参数所返回的是作为请求参数的 approval_code 的值。</para>
    /// <para>示例值：permission_test</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string ApprovalCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>审批定义所属分组</para>
    /// <para>示例值：work_group</para>
    /// </summary>
    [JsonPropertyName("group_code")]
    public string GroupCode { get; set; } = string.Empty;

    /// <summary>
    /// <para>分组名称。当前参数返回的是 @i18n@ 开头的 key，需要通过 i18n_resources.texts 参数值查阅当前 key 对应的取值（value）。</para>
    /// <para>示例值：@i18n@2</para>
    /// </summary>
    [JsonPropertyName("group_name")]
    public string? GroupName { get; set; }

    /// <summary>
    /// <para>审批定义的说明。当前参数返回的是 @i18n@ 开头的 key，需要通过 i18n_resources.texts 参数值查阅当前 key 对应的取值（value）。</para>
    /// <para>示例值：@i18n@2</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>三方审批定义相关信息。</para>
    /// </summary>
    [JsonPropertyName("external")]
    public ApprovalCreateExternal? External { get; set; }

    /// <summary>
    /// <para>可见人列表，在可见范围内的用户可在审批发起页看到当前审批。</para>
    /// </summary>
    [JsonPropertyName("viewers")]
    public ApprovalCreateViewers[]? Viewers { get; set; }

    /// <summary>
    /// <para>国际化文案</para>
    /// </summary>
    [JsonPropertyName("i18n_resources")]
    public I18nResource[]? I18nResources { get; set; }

    /// <summary>
    /// <para>审批流程管理员列表，列表内包含的是用户 ID，ID 类型与查询参数 user_id_type 取值一致。</para>
    /// </summary>
    [JsonPropertyName("managers")]
    public string[]? Managers { get; set; }
}
