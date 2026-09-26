// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批实例详情（以用户身份查询）中的审批动态信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Approval")]
public class InstanceDetailTimelineInfo
{
    /// <summary>
    /// <para>事件类型</para>
    /// <para>可选值：<list type="bullet">
    /// <item>START：审批开始</item>
    /// <item>PASS：通过</item>
    /// <item>REJECT：拒绝</item>
    /// <item>AUTO_PASS：自动通过</item>
    /// <item>AUTO_REJECT：自动拒绝</item>
    /// <item>TRANSFER：转交</item>
    /// <item>ADD_APPROVER_BEFORE：前加签</item>
    /// <item>ADD_APPROVER：并加签</item>
    /// <item>ADD_APPROVER_AFTER：后加签</item>
    /// <item>DELETE_APPROVER：减签</item>
    /// <item>ROLLBACK_SELECTED：指定回退</item>
    /// <item>ROLLBACK：全部回退</item>
    /// <item>CANCEL：撤回</item>
    /// <item>DELETE：删除</item>
    /// <item>CC：抄送</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>发生时间，毫秒时间戳</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>动态产生用户</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>被抄送人列表</para>
    /// </summary>
    [JsonPropertyName("cc_user_ids")]
    public string[]? CcUserIds { get; set; }

    /// <summary>
    /// <para>产生动态关联的 task_id</para>
    /// </summary>
    [JsonPropertyName("task_id")]
    public string? TaskId { get; set; }

    /// <summary>
    /// <para>理由</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>产生 task 的节点 key</para>
    /// </summary>
    [JsonPropertyName("node_id")]
    public string? NodeId { get; set; }

    /// <summary>
    /// <para>审批附件</para>
    /// </summary>
    [JsonPropertyName("files")]
    public ApprovalFileInfo[]? Files { get; set; }
}
