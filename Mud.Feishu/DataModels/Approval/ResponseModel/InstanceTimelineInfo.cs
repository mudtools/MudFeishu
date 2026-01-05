// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批动态信息
/// </summary>
public class InstanceTimelineInfo
{
    /// <summary>
    /// <para>动态类型。不同的动态类型，对应 ext 返回值也不同，具体参考以下各枚举值描述。</para>
    /// <para>示例值：PASS</para>
    /// <para>可选值：<list type="bullet">
    /// <item>START：审批开始。对应的 ext 参数不会返回值。</item>
    /// <item>PASS：通过。对应的 ext 参数不会返回值。</item>
    /// <item>REJECT：拒绝。对应的 ext 参数不会返回值。</item>
    /// <item>AUTO_PASS：自动通过。对应的 ext 参数不会返回值。</item>
    /// <item>AUTO_REJECT：自动拒绝。对应的 ext 参数不会返回值。</item>
    /// <item>REMOVE_REPEAT：去重。对应的 ext 参数不会返回值。</item>
    /// <item>TRANSFER：转交。对应的 ext 参数返回的 user_id_list 包含被转交人的用户 ID。</item>
    /// <item>ADD_APPROVER_BEFORE：前加签。对应的 ext 参数返回的 user_id_list 包含被加签人的用户 ID。</item>
    /// <item>ADD_APPROVER：并加签。对应的 ext 参数返回的 user_id_list 包含被加签人的用户 ID。</item>
    /// <item>ADD_APPROVER_AFTER：后加签。对应的 ext 参数返回的 user_id_list 包含被加签人的用户 ID。</item>
    /// <item>DELETE_APPROVER：减签。对应的 ext 参数返回的 user_id_list 包含被加签人的用户 ID。</item>
    /// <item>ROLLBACK_SELECTED：指定回退。对应的 ext 参数不会返回值。</item>
    /// <item>ROLLBACK：全部回退。对应的 ext 参数不会返回值。</item>
    /// <item>CANCEL：撤回。对应的 ext 参数不会返回值。</item>
    /// <item>DELETE：删除。对应的 ext 参数不会返回值。</item>
    /// <item>CC：抄送。对应的 ext 参数返回的 user_id 包含抄送人的用户 ID。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>发生时间，毫秒级时间戳。</para>
    /// <para>示例值：1564590532967</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>产生该动态的用户 user_id</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>产生该动态的用户 open_id</para>
    /// <para>示例值：ou_123456</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// <para>被抄送人列表，列表内包含的是用户 user_id。</para>
    /// </summary>
    [JsonPropertyName("user_id_list")]
    public string[]? UserIdList { get; set; }

    /// <summary>
    /// <para>被抄送人列表，列表内包含的是用户 open_id。</para>
    /// </summary>
    [JsonPropertyName("open_id_list")]
    public string[]? OpenIdList { get; set; }

    /// <summary>
    /// <para>产生动态关联的任务 ID</para>
    /// <para>示例值：1234</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>理由</para>
    /// <para>示例值：ok</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>抄送人列表</para>
    /// </summary>
    [JsonPropertyName("cc_user_list")]
    public InstanceCcUserInfo[]? CcUserLists { get; set; }


    /// <summary>
    /// <para>其他信息，JSON 格式，目前包括 user_id_list, user_id，open_id_list，open_id</para>
    /// <para>必填：是</para>
    /// <para>示例值：{\"user_id\":\"62d4a44c\",\"open_id\":\"ou_123456\"}</para>
    /// </summary>
    [JsonPropertyName("ext")]
    public string Ext { get; set; } = string.Empty;

    /// <summary>
    /// <para>产生审批任务的节点 key</para>
    /// <para>示例值：APPROVAL_240330_4058663</para>
    /// </summary>
    [JsonPropertyName("node_key")]
    public string? NodeKey { get; set; }

    /// <summary>
    /// <para>审批附件</para>
    /// </summary>
    [JsonPropertyName("files")]
    public ApprovalFileInfo[]? Files { get; set; }
}
