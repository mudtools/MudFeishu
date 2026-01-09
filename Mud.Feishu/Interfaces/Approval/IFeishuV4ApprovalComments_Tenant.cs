// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalComments;

namespace Mud.Feishu;

/// <summary>
/// 原生审批实例内，支持员工进行评论、回复评论。评论内容支持文本、@用户以及添加附件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/instance-comment/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Approval")]
[Header(Consts.Authorization)]
public interface IFeishuTenantV4ApprovalComments
{
    /// <summary>
    /// 在指定审批实例下创建、修改评论或回复评论（不包含审批同意、拒绝、转交等附加的理由或意见）。
    /// </summary>
    /// <param name="instance_id">审批实例 Code。说明：支持传入自定义审批实例 ID。示例值："6A123516-FB88-470D-A428-9AF58B71B3C0"</param>
    /// <param name="user_id">用户 ID，ID 类型与 user_id_type 取值一致。示例值："e5286g26"</param>
    /// <param name="createApprovalRequest">创建评论请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/{instance_id}/comments")]
    Task<FeishuApiResult<CommentOperationResult>?> CreateCommentAsync(
        [Path] string instance_id,
        [Query("user_id")] string user_id,
        [Body] CreateCommentRequest createApprovalRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除某审批实例下的一条评论或评论回复（不包含审批同意、拒绝、转交等附加的理由或意见），删除后在审批中心的审批实例内不再显示评论内容，而是显示 评论已删除。
    /// </summary>
    /// <param name="instance_id">审批实例 Code。说明：支持传入自定义审批实例 ID。示例值："6A123516-FB88-470D-A428-9AF58B71B3C0"</param>
    /// <param name="user_id">用户 ID，ID 类型与 user_id_type 取值一致。示例值："e5286g26"</param>
    /// <param name="comment_id">评论 ID。示例值："7081516627711606803"</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/approval/v4/instances/{instance_id}/comments/{comment_id}")]
    Task<FeishuApiResult<CommentOperationResult>?> DeleteCommentByIdAsync(
       [Path] string instance_id,
       [Path] string comment_id,
       [Query("user_id")] string user_id,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 清空某审批实例下的全部评论与评论回复，包括显示为已删除的评论。
    /// </summary>
    /// <param name="instance_id">审批实例 Code。说明：支持传入自定义审批实例 ID。示例值："6A123516-FB88-470D-A428-9AF58B71B3C0"</param>
    /// <param name="user_id">用户 ID，ID 类型与 user_id_type 取值一致。示例值："e5286g26"</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/approval/v4/instances/{instance_id}/comments/remove")]
    Task<FeishuApiResult<CommentsRemoveResult>?> RemoveCommentsAsync(
      [Path] string instance_id,
      [Query("user_id")] string user_id,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 根据审批实例 Code 获取某个审批实例下，全部评论与评论回复（不包含审批同意、拒绝、转交等附加的理由或意见）。
    /// </summary>
    /// <param name="instance_id">审批实例 Code。说明：支持传入自定义审批实例 ID。示例值："6A123516-FB88-470D-A428-9AF58B71B3C0"</param>
    /// <param name="user_id">用户 ID，ID 类型与 user_id_type 取值一致。示例值："e5286g26"</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/approval/v4/instances/{instance_id}/comments")]
    Task<FeishuApiResult<CommentsPageListResult>?> GetCommentsPageListByIdAsync(
      [Path] string instance_id,
      [Query("user_id")] string user_id,
      [Query("page_size")] int page_size = 10,
      [Query("page_token")] string? page_token = null,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
