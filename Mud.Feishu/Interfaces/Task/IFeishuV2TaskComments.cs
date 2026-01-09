// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskComments;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 评论接口可以实现评论创建、回复、更新、删除、获取详情等功能。
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
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
    [Post("/open-apis/task/v2/comments")]
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
    [Get("/open-apis/task/v2/comments/{comment_id}")]
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
    [Patch("/open-apis/task/v2/comments/{comment_id}")]
    Task<FeishuApiResult<CommentOpreationResult>?> UpdateCommentByIdAsync(
         [Path] string comment_id,
         [Body] UpdateCommentRequest updateCommentRequest,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>删除一条评论。</para>
    /// <para>评论被删除后，将无法进行任何操作，也无法恢复。</para>
    /// </summary>
    /// <param name="comment_id">要删除评论详情的评论ID。示例值："7198104824246747156"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/task/v2/comments/{comment_id}")]
    Task<FeishuNullDataApiResult?> DeleteCommentByIdAsync(
       [Path] string comment_id,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 给定一个资源，返回该资源的评论列表。
    /// <para>支持分页。评论可以按照创建时间的正序（asc, 从最老到最新），或者逆序（desc，从最老到最新），返回数据。</para>
    /// </summary>
    /// <param name="direction">返回数据的排序方式。"asc"表示从最老到最新顺序返回；"desc"表示从最新到最老顺序返回。默认为"asc"。</param>
    /// <param name="resource_id">要获取评论的资源ID。例如要获取任务的评论列表，此处应该填写任务全局唯一ID
    /// <para>示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"</para></param>
    /// <param name="resource_type">要获取评论列表的资源类型，目前只支持"task"，默认为"task"。
    /// <para>示例值："task"</para></param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/comments")]
    Task<FeishuApiPageListResult<TaskCommentInfo>?> GetCommentPageListAsync(
         [Query("resource_id")] string? resource_id = null,
         [Query("resource_type")] string? resource_type = "task",
         [Query("direction")] string? direction = "asc",
         [Query("page_size")] int page_size = Consts.PageSize,
         [Query("page_token")] string? page_token = null,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);
}
