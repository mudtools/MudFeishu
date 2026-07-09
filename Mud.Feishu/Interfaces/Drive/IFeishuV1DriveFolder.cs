// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive.Folder;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 文件夹是飞书云空间中用于管理文件和其它文件夹的容器。每个文件夹都有唯一的 token 作为标识。在不同接口中，其参数命名可能不同，包括 token、 folder_token、folderToken 等。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/drive-v1/folder/folder-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DriveFolder : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取用户“我的空间”（根文件夹）的元数据，包括根文件夹的 token、ID 和文件夹所有者的 ID。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/folder/get-root-folder-meta">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/explorer/v2/root_folder/meta")]
    Task<FeishuApiResult<GetDriveRootFolderMetaReuslt>?> GetDriveRootFolderMetaAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// 根据用户和任务分组查询任务列表。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/folder/list">接口文档</see></para>
    /// </summary>
    /// <param name="direction">定义清单中文件的排序规则，与 order_by 配合使用，可选值：ASC：按升序排序、DESC：按降序排序</param>
    /// <param name="folder_token">文件夹的 token。不填写或填空字符串，将获取用户云空间根目录下的清单，且不支持分页和返回快捷方式。</param>
    /// <param name="order_by">排序字段，默认为 EditedTime，表示按照文件的编辑时间进行排序。可选值包括：CreatedTime（创建时间）、EditedTime（编辑时间）。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files")]
    Task<FeishuApiResult<GetDriveFilesResult>?> GetFilesPageListAsync(
       [Query("folder_token")] string? folder_token,
       [Query("order_by")] string? order_by = "EditedTime",
       [Query("direction")] string? direction = "DESC",
       [Query("page_size")] int page_size = Consts.PageSize_10,
       [Query("page_token")] string? page_token = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于根据文件夹 token 获取该文件夹的元数据，包括文件夹的 ID、名称、创建者 ID 等。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/folder/get-folder-meta">接口文档</see></para>
    /// </summary>
    /// <param name="folderToken">文件夹的 token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/explorer/v2/folder/{folderToken}/meta")]
    Task<FeishuApiResult<GetFolderMetaResult>?> GetFolderMetaByTokenAsync(
        [Path] string folderToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于在用户云空间指定文件夹中创建一个空文件夹。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/folder/create_folder">接口文档</see></para>
    /// </summary>
    /// <param name="createFolderRequest">新建文件夹请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/drive/v1/files/create_folder")]
    Task<FeishuApiResult<CreateFolderResult>?> CreateFolderAsync(
        [Body] CreateFolderRequest createFolderRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询异步任务的状态信息。目前支持查询删除文件夹和移动文件夹的异步任务。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/file/async-task/task_check">接口文档</see></para>
    /// </summary>
    /// <param name="task_id">异步任务的 ID。目前支持查询删除文件夹和移动文件夹的异步任务。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files/task_check")]
    Task<FeishuApiResult<FilesTaskCheckResult>?> GetTaskCheckFileAsync(
        [Query("task_id")] string task_id,
        CancellationToken cancellationToken = default);

}
