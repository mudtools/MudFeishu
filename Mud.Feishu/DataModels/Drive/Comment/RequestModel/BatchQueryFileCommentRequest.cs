// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// <para>批量获取评论请求体</para>
/// </summary>
public class BatchQueryFileCommentRequest
{
    /// <summary>
    /// <para>需要获取数据的评论 ID ，可通过调用获取云文档所有评论接口获取 comment_id</para>
    /// <para>必填：是</para>
    /// <para>示例值：1654857036541812356</para>
    /// </summary>
    [JsonPropertyName("comment_ids")]
    public string[] CommentIds { get; set; } = [];

    /// <summary>
    /// 是否需要获取评论卡片上挂载的Reaction数据，默认值为false
    /// </summary>
    [JsonPropertyName("need_reaction")]
    public bool NeedReaction { get; set; } = false;
}