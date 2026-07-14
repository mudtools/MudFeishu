// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;

/// <summary>
/// <para>新增参与人列表。</para>
/// <para>**注意**：</para>
/// <para>- 单次请求可设置的参与人数量（含会议室）上限为 1000。</para>
/// <para>- 单次请求可设置的会议室数量上限为 100。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Calendar")]
public class CalendarEventAttendeeData
{
    /// <summary>
    /// <para>参与人类型。</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// <item>chat：群组</item>
    /// <item>resource：会议室</item>
    /// <item>third_party：外部邮箱</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>参与人是否为可选参加。</para>
    /// <para>**可选值有**：</para>
    /// <para>- true：是</para>
    /// <para>- false：否</para>
    /// <para>**注意**：无法编辑会议室类型参与人的此字段。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>默认值：false</para>
    /// </summary>
    [JsonPropertyName("is_optional")]
    public bool? IsOptional { get; set; }

    /// <summary>
    /// <para>用户 ID。当选择用户类型参与人（type 取值为 user）时，需要传入该参数。传入的用户 ID 类型需要和 user_id_type 的值保持一致。关于用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>群组 ID。当选择群组类型参与人（type 取值为 chat）时，需要传入该参数。关于群组 ID 可参见[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：oc_xxxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("chat_id")]
    public string? ChatId { get; set; }

    /// <summary>
    /// <para>会议室 ID。当选择会议室类型参与人（type 取值为 resource）时，需要传入该参数。</para>
    /// <para>你可以通过以下接口获取指定会议室 ID：</para>
    /// <para>- [查询会议室列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/room/list)</para>
    /// <para>- [搜索会议室](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/room/search)</para>
    /// <para>必填：否</para>
    /// <para>示例值：omm_xxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("room_id")]
    public string? RoomId { get; set; }

    /// <summary>
    /// <para>邮箱地址。当选择外部邮箱类型参与人（type 取值为 third_party）时，需要传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：wangwu@email.com</para>
    /// </summary>
    [JsonPropertyName("third_party_email")]
    public string? ThirdPartyEmail { get; set; }

    /// <summary>
    /// <para>会议室联系人 ID。传入的用户 ID 类型需要和 user_id_type 的值保持一致。关于用户 ID 可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>**说明**：如果当前日程是基于应用身份创建的，则在添加会议室类型参与人时，需要通过该参数指定会议室的联系人，该联系人会在日程会议室信息中展示。</para>
    /// <para>**默认值**：空</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxxxx</para>
    /// </summary>
    [JsonPropertyName("operate_id")]
    public string? OperateId { get; set; }

    /// <summary>
    /// <para>会议室的个性化配置。</para>
    /// <para>- 在选择会议室类型参与人时，如果会议室有预定表单，则可以通过该参数配置表单信息。</para>
    /// <para>- 当前添加的参与人不涉及会议室个性化配置时，无需设置该参数。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("resource_customization")]
    public CalendarAttendeeResourceCustomization[]? ResourceCustomizations { get; set; }


    /// <summary>
    /// <para>申请预定审批会议室的原因。参数配置说明：</para>
    /// <para>- 仅使用用户身份（user_access_token）预定审批会议室时，该字段生效。</para>
    /// <para>- 对于申请预定审批会议室的场景，不传该值会直接预约失败。</para>
    /// <para>- 如果使用应用身份（tenant_access_token）预定审批会议室，会直接失败。</para>
    /// <para>**默认值**：空</para>
    /// <para>必填：否</para>
    /// <para>示例值：申请原因</para>
    /// <para>最大长度：200</para>
    /// </summary>
    [JsonPropertyName("approval_reason")]
    public string? ApprovalReason { get; set; }
}
