// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ApprovalComments;

/// <summary>
/// <para>评论的回复数据</para>
/// </summary>
public class CommentReply
{
    /// <summary>
    /// <para>评论 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：7081516611634741268</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// <para>评论内容</para>
    /// <para>必填：是</para>
    /// <para>示例值：{\"text\":\"x@某某来自小程序的评论，这是一条回复\"}</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// <para>评论创建时间，毫秒时间戳。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1648803677000</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>评论更新时间，毫秒时间戳。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1648803677000</para>
    /// </summary>
    [JsonPropertyName("update_time")]
    public string? UpdateTime { get; set; }

    /// <summary>
    /// <para>是否删除。可能值有：</para>
    /// <para>- 0：未删除</para>
    /// <para>- 1：已删除</para>
    /// <para>必填：是</para>
    /// <para>示例值：0</para>
    /// </summary>
    [JsonPropertyName("is_delete")]
    public int IsDelete { get; set; }

    /// <summary>
    /// <para>评论中艾特人信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("at_info_list")]
    public CommentAtInfo[]? AtInfoLists { get; set; }

    /// <summary>
    /// <para>评论创建人</para>
    /// <para>必填：是</para>
    /// <para>示例值：893g4c45</para>
    /// </summary>
    [JsonPropertyName("commentator")]
    public string Commentator { get; set; } = string.Empty;

    /// <summary>
    /// <para>附加字段</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"a\":\"a\"}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }
}
