// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Moments;

/// <summary>
/// 发布评论
/// <para>公司圈用户发布评论时触发此事件。</para>
/// <para>事件类型:moments.comment.created_v1</para>
/// <para>使用时请继承：<see cref="MomentsCommentCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/moments-v1/comment/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.MomentsCommentCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Moments")]
public class MomentsCommentCreatedResult : IEventResult
{
    /// <summary>
    /// <para>用户 ID（仅实名下有值）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public UserIdInfo? UserId { get; set; }

    /// <summary>
    /// <para>评论 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>评论创建时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>评论所属帖子的ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("post_id")]
    public string? PostId { get; set; }

    /// <summary>
    /// <para>回复的评论ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("reply_comment_id")]
    public string? ReplyCommentId { get; set; }

    /// <summary>
    /// <para>根评论ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("root_comment_id")]
    public string? RootCommentId { get; set; }

    /// <summary>
    /// <para>评论人类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：实名</para>
    /// <para>- 2：花名</para>
    /// <para>- 3：匿名</para>
    /// <para>- 4：官方号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_type")]
    public int? UserType { get; set; }
}
