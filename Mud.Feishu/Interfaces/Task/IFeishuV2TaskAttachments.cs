// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.TaskAttachments;

namespace Mud.Feishu;

/// <summary>
/// 任务可以拥有附件。一个附件可以是任意类型的文件，如图片，PDF文档，zip文件等。
/// <para>附件不可以单独存在，必须与某种资源产生关联关系。</para>
/// <para>关联附件的资源类型只有任务。因为附件不可单独存在，因此为新任务添加附件时，必须先调用创建任务接口，完成任务创建，再调用上传附件接口上传文件，并关联到新建的任务上。</para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header(Consts.Authorization)]
public interface IFeishuV2TaskAttachments
{
    /// <summary>
    /// 为特定资源上传附件。本接口可以支持一次上传多个附件，最多5个。每个附件尺寸不超过50MB，格式不限。
    /// </summary>
    /// <param name="uploadFileRequest">上传附件请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/task/v2/attachments/upload")]
    [IgnoreImplement]
    Task<FeishuApiResult<TaskAttachmentsUploadResult>?> UploadAttachmentAsync(
         [Body] UploadTaskAttachmentsRequest uploadFileRequest,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 列取一个资源的所有附件。返回的附件列表支持分页，按照附件上传时间排序。
    /// <para>每个附件会返回一个可供下载的临时url，有效期为3分钟，最多可以支持3次下载。如果超过使用限制，需要通过本接口获取新的临时url。</para>
    /// </summary>
    /// <param name="resource_id">要获取评论的资源ID。例如要获取任务的评论列表，此处应该填写任务全局唯一ID
    /// <para>示例值："d300a75f-c56a-4be9-80d1-e47653028ceb"</para></param>
    /// <param name="resource_type">要获取评论列表的资源类型，目前只支持"task"，默认为"task"。
    /// <para>示例值："task"</para></param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/attachments")]
    Task<FeishuApiPageListResult<AttachmentResultInfo>?> GetAttachmentPageListAsync(
         [Query("resource_id")] string? resource_id = null,
         [Query("resource_type")] string? resource_type = "task",
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 提供一个附件GUID，返回附件的详细信息，包括GUID，名称，大小，上传时间，临时可下载链接等。
    /// </summary>
    /// <param name="attachment_guid">获取详情的附件GUID。示例值："b59aa7a3-e98c-4830-8273-cbb29f89b837"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/task/v2/attachments/{attachment_guid}")]
    Task<FeishuApiResult<GetAttachmentsInfoResult>?> GetAttachmentByIdAsync(
        [Path] string attachment_guid,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 提供一个附件GUID，删除该附件。删除后该附件不可再恢复。
    /// </summary>
    /// <param name="attachment_guid">删除的附件GUID。示例值："b59aa7a3-e98c-4830-8273-cbb29f89b837"</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/task/v2/attachments/{attachment_guid}")]
    Task<FeishuNullDataApiResult?> DeleteAttachmentByIdAsync(
          [Path] string attachment_guid,
          [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);
}
