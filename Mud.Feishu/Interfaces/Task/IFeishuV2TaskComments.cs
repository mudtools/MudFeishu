// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskComments;

namespace Mud.Feishu;

/// <summary>
/// 评论接口可以实现评论创建、回复、更新、删除、获取详情等功能。
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header("Authorization")]
public interface IFeishuV2TaskComments
{
    /// <summary>
    /// <para>为一个任务创建评论，或者回复该任务的某个评论。</para>
    /// <para>若要创建一个回复评论，需要在创建时设置reply_to_comment_id字段。被回复的评论和新建的评论必须属于同一个任务。</para>
    /// </summary>
    /// <param name="createCommentRequest">创建评论请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/task/v2/comments")]
    Task<FeishuApiResult<CommentOpreationResult>?> CreateCommentAsync(
           [Body] CreateCommentRequest createCommentRequest,
           [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>给定一个评论的ID，返回评论的详情，包括内容，创建人，创建时间和更新时间等信息。</para>
    /// </summary>
    /// <param name="comment_id">要获取评论详情的评论ID。示例值："7198104824246747156"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/task/v2/comments/{comment_id}")]
    Task<FeishuApiResult<CommentOpreationResult>?> GetCommentByIdAsync(
          [Path] string comment_id,
          [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>更新一条评论。</para>
    /// <para>更新时，将update_fields字段中填写所有要修改的评论的字段名，同时在comment字段中填写要修改的字段的新值即可。</para>
    /// </summary>
    /// <param name="updateCommentRequest">更新评论请求体。</param>
    /// <param name="comment_id">要更新评论详情的评论ID。示例值："7198104824246747156"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/task/v2/comments/{comment_id}")]
    Task<FeishuApiResult<CommentOpreationResult>?> UpdateCommentByIdAsync(
         [Path] string comment_id,
         [Body] UpdateCommentRequest updateCommentRequest,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);
}
