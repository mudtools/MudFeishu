// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Drive;

/// <summary>
/// 文件协作者权限申请
/// <para>当用户发起申请文件协作者权限时将触发此事件，协作者权限包括阅读、编辑和管理权限。</para>
/// <para>事件类型:drive.file.permission_member_applied_v1</para>
/// <para>使用时请继承：<see cref="DriveFilePermissionMemberAppliedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/docs/drive-v1/event/list/permission_member_applied</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.DriveFilePermissionMemberApplied, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class DriveFilePermissionMemberAppliedResult : IEventResult
{
    /// <summary>
    /// <para>文件对应的类型，与文件的 file_token 相匹配。</para>
    /// <para>**可选值有**：</para>
    /// <para>doc:旧版文档,sheet:电子表格,bitable:多维表格,docx:新版文档,slides:幻灯片,file:文件</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `50` 字符</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>docx：新版文档</item>
    /// <item>slides：幻灯片</item>
    /// <item>file：文件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("file_type")]
    public string? FileType { get; set; }

    /// <summary>
    /// <para>文件的 token，获取方式见 [如何获取云文档资源相关 token](https://open.feishu.cn/document/ukTMukTMukTM/uczNzUjL3czM14yN3MTN#08bb5df6)</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`22` ～ `27` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("file_token")]
    public string? FileToken { get; set; }

    /// <summary>
    /// <para>发起权限申请的操作人的 ID，可以是替操作人自己申请权限，也可以是替其他人申请权限</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_id")]
    public UserIdInfo? OperatorId { get; set; }

    /// <summary>
    /// <para>审批人 ID。即收到协作者权限申请、负责处理该申请的用户 ID，一般是文件的所有者</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approver_id")]
    public UserIdInfo? ApproverId { get; set; }

    /// <summary>
    /// <para>申请授权的用户 ID 列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `100`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_user_list")]
    public UserIdInfo[]? ApplicationUserList { get; set; }

    /// <summary>
    /// <para>申请授权的群 open_chat_id 列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `100`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_chat_list")]
    public string[]? ApplicationChatList { get; set; }

    /// <summary>
    /// <para>申请授权的组织架构 open_department_id 列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `100`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_department_list")]
    public string[]? ApplicationDepartmentList { get; set; }

    /// <summary>
    /// <para>权限申请备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_remark")]
    public string? ApplicationRemark { get; set; }

    /// <summary>
    /// <para>申请的协作者权限</para>
    /// <para>**可选值有**：</para>
    /// <para>view:可阅读权限角色,edit:可编辑权限角色,full_access:可管理权限角色</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `27` 字符</para>
    /// <para>必填：否</para>
    /// <para>可选值：<list type="bullet">
    /// <item>view：可阅读权限角色</item>
    /// <item>edit：可编辑权限角色</item>
    /// <item>full_access：可管理权限角色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("permission")]
    public string? Permission { get; set; }

    /// <summary>
    /// <para>订阅用户 ID 列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `100`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("subscriber_ids")]
    public UserIdInfo[]? SubscriberIds { get; set; }
}
