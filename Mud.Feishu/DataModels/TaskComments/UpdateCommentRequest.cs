// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TaskComments;

/// <summary>
/// 更新评论请求体。
/// </summary>
public class UpdateCommentRequest
{
    /// <summary>
    /// <para>要更新的评论数据。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public CommentContent Comment { get; set; } = new();

    /// <summary>
    /// <para>要更新的字段，支持</para>
    /// <para>必填：是</para>
    /// <para>可选值：<list type="bullet">
    /// <item>content：评论内容</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("update_fields")]
    public string[] UpdateFields { get; set; } = [];
}


/// <summary>
/// <para>要更新的评论数据。</para>
/// </summary>
public class CommentContent
{
    /// <summary>
    /// <para>要更新的评论内容。如果更新该字段，不允许设为空，最大支持3000个utf8字符。</para>
    /// <para>必填：否</para>
    /// <para>示例值：举杯邀明月，对影成三人</para>
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }
}