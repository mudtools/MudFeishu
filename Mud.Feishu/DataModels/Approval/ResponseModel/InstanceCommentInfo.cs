// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// 审批实例评论信息
/// </summary>
public class InstanceCommentInfo
{
    /// <summary>
    /// <para>评论 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：1234</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>发表评论的用户 user_id</para>
    /// <para>必填：是</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// <para>发表评论的用户 open_id</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_123456</para>
    /// </summary>
    [JsonPropertyName("open_id")]
    public string OpenId { get; set; } = string.Empty;

    /// <summary>
    /// <para>评论内容</para>
    /// <para>必填：是</para>
    /// <para>示例值：ok</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// <para>评论时间，毫秒级时间戳。</para>
    /// <para>必填：是</para>
    /// <para>示例值：1564590532967</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>评论附件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("files")]
    public ApprovalFileInfo[]? Files { get; set; }
}
