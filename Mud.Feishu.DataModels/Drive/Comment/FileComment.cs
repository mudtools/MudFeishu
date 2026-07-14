// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// <para>评论列表</para>
/// </summary>
public class FileComment
{
    /// <summary>
    /// <para>评论 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6916106822734512356</para>
    /// </summary>
    [JsonPropertyName("comment_id")]
    public string? CommentId { get; set; }

    /// <summary>
    /// <para>用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_cc19b2bfb93f8a44db4b4d6eababcef</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>创建时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1610281603</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public long? CreateTime { get; set; }

    /// <summary>
    /// <para>更新时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1610281603</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public long? UpdateTime { get; set; }

    /// <summary>
    /// <para>是否已解决</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_solved")]
    public bool? IsSolved { get; set; }

    /// <summary>
    /// <para>解决评论时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1610281603</para>
    /// </summary>
    [JsonPropertyName("solved_time")]
    public int? SolvedTime { get; set; }

    /// <summary>
    /// <para>解决评论者的用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_cc19b2bfb93f8a44db4b4d6eababcef</para>
    /// </summary>
    [JsonPropertyName("solver_user_id")]
    public string? SolverUserId { get; set; }

    /// <summary>
    /// <para>是否有更多回复</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("has_more")]
    public bool? HasMore { get; set; }

    /// <summary>
    /// <para>回复分页标记</para>
    /// <para>必填：否</para>
    /// <para>示例值：6916106822734512356</para>
    /// </summary>
    [JsonPropertyName("page_token")]
    public string? PageToken { get; set; }

    /// <summary>
    /// <para>是否是全文评论</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_whole")]
    public bool? IsWhole { get; set; }

    /// <summary>
    /// <para>局部评论的引用字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：""</para>
    /// </summary>
    [JsonPropertyName("quote")]
    public string? Quote { get; set; }

    /// <summary>
    /// <para>评论里的回复列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reply_list")]
    public FileCommentReplyList? ReplyList { get; set; }
}