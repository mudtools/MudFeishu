// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 文件是云空间内各种类型的文件的统称，泛指云空间内所有的文件。包括在云空间创建的在线文档、电子表格、多维表格、思维笔记、知识库中的文档等，也包括从本地环境上传的各类文件。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/docs/drive-v1/file/file-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DriveFiles : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 用于根据文件 token 获取其元数据，包括标题、所有者、创建时间、密级、访问链接等数据。
    /// </summary>
    /// <param name="metasBatchQueryRequest">获取文件元数据请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/drive/v1/metas/batch_query")]
    Task<FeishuApiResult<MetasBatchQueryResult>?> BatchQueryMetasAsync(
        [Body] MetasBatchQueryRequest metasBatchQueryRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 用于获取各类文件的流量统计信息和互动信息，包括阅读人数、阅读次数和点赞数。
    /// </summary>
    /// <param name="file_token">文件 token。示例值：doccnfYZzTlvXqZIGTdAHKabcef</param>
    /// <param name="file_type">
    /// <para>必填：是</para>
    /// <para>文件类型</para>
    /// <para>示例值：doc</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>bitable：多维表格</item>
    /// <item>wiki：知识库文档</item>
    /// <item>file：文件</item>
    /// <item>docx：新版文档</item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/files/{file_token}/statistics")]
    Task<FeishuApiResult<FileStatisticsReuslt>?> GetFileStatisticsByFileTokenAsync(
      [Path] string? file_token,
      [Query("file_type")] string file_type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取文档、电子表格、多维表格等文件的历史访问记录，包括访问者的 ID、姓名、头像和最近访问时间。
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="file_type">
    /// <para>必填：是</para>
    /// <para>文件类型</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// <item>bitable：多维表格</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>wiki：知识库文档</item>
    /// <item>file：文件</item>
    /// </list>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="viewer_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files/{file_token}/view_records")]
    Task<FeishuApiPageListResult<FileViewRecord>?> GetFileViewRecordPageListByFileTokenAsync(
       [Path] string? file_token,
       [Query("file_type")] string file_type,
       [Query("page_size")] int page_size = Consts.PageSize,
       [Query("page_token")] string? page_token = null,
       [Query("viewer_id_type")] string? viewer_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 将用户云空间中的文件复制至其它文件夹下。该接口为异步接口。
    /// </summary>
    /// <param name="copyFileRequest">复制文件请求体</param>
    /// <param name="file_token">文件 token。示例值：doccnfYZzTlvXqZIGTdAHKabcef</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/files/{file_token}/copy")]
    Task<FeishuApiResult<CopyFileResult>?> CopyFileByFileTokenAsync(
      [Body] CopyFileRequest copyFileRequest,
      [Path] string? file_token,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 将文件或者文件夹移动到用户云空间的其他位置。该接口为异步接口。
    /// </summary>
    /// <param name="moveFileRequest">移动文件请求体</param>
    /// <param name="file_token">文件 token。示例值：doccnfYZzTlvXqZIGTdAHKabcef</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/files/{file_token}/move")]
    Task<FeishuApiResult<FileTaskResult>?> MoveFileByFileTokenAsync(
     [Body] MoveFileRequest moveFileRequest,
     [Path] string? file_token,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除用户在云空间内的文件或者文件夹。文件或文件夹被删除后，会进入回收站中。
    /// </summary>
    /// <param name="file_type">被删除文件的类型
    /// <para>必填：是</para>
    /// <para>被删除文件的类型</para>
    /// <para>示例值：file</para>
    /// <list type="bullet">
    /// <item>file：文件类型</item>
    /// <item>docx：新版文档类型</item>
    /// <item>bitable：多维表格类型</item>
    /// <item>folder：文件夹类型</item>
    /// <item>doc：文档类型</item>
    /// <item>sheet：电子表格类型</item>
    /// <item>mindnote：思维笔记类型</item>
    /// <item>shortcut：快捷方式类型</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="file_token">文件 token。示例值：doccnfYZzTlvXqZIGTdAHKabcef</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/drive/v1/files/{file_token}")]
    Task<FeishuApiResult<FileTaskResult>?> DeleteFileByFileTokenAsync(
        [Path] string? file_token,
        [Query("type")] string file_type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建指定文件的快捷方式到云空间的其它文件夹中。
    /// </summary>
    /// <param name="createShortcutRequest">创建文件快捷方式请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/files/create_shortcut")]
    Task<FeishuApiResult<CreateShortcutResult>?> CreateShortcutAsync(
       [Body] CreateShortcutRequest createShortcutRequest,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 将指定文件上传至云空间指定目录中。
    /// <para>将指定文件上传至云空间指定目录中。</para>
    /// <para>## 使用限制</para>
    /// <para>- 文件大小不得超过 20 MB，且不可上传空文件。要上传大于 20 MB 的文件，你需使用分片上传文件相关接口。详情参考[上传文件概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/multipart-upload-file-/introduction)。</para>
    /// <para>- 该接口调用频率上限为 5 QPS，10000 次/天。否则会返回 1061045 错误码，可通过稍后重试解决。</para>
    /// </summary>
    /// <param name="uploadAllFileRequest">上传文件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/files/upload_all")]
    Task<FeishuApiResult<FilesUploadAllResult>?> UploadAllFileAsync(
      [FormContent] UploadAllFileRequest uploadAllFileRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 发送初始化请求，以获取上传事务 ID 和分片策略，为上传分片做准备。平台固定以 4MB 的大小对文件进行分片。
    /// <para>上传事务 ID 和上传进度在 24 小时内有效。请及时保存和恢复上传。</para>
    /// <para>## 使用限制</para>
    /// <para>- 上传文件的大小限制因飞书版本而异。</para>
    /// <para>- 该接口不支持并发调用，且调用频率上限为 5 QPS，10000 次/天。否则会返回 1061045 错误码，可通过稍后重试解决。</para>
    /// </summary>
    /// <param name="filesUploadPartRequest">预上传请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/files/upload_prepare")]
    Task<FeishuApiResult<FilesUploadPrepareResult>?> UploadPrepareFileAsync(
       [Body] FilesUploadPrepareRequest filesUploadPartRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据 预上传接口返回的上传事务 ID 和分片策略上传对应的文件分片。
    /// <para>上传完成后，需调用分片上传文件（完成上传）触发完成上传。</para>
    /// </summary>
    /// <param name="filesUploadPartRequest"> 分片上传文件-上传分片请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/files/upload_part")]
    Task<FeishuNullDataApiResult?> UploadPartFileAsync(
       [FormContent] FilesUploadPartRequest filesUploadPartRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 将分片全部上传完毕后，需调用本接口触发完成上传。否则将上传失败。
    /// </summary>
    /// <param name="filesUploadPartRequest">分片上传文件-完成上传请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/files/upload_finish")]
    Task<FeishuApiResult<FilesUploadFinishResult>?> UploadFinishFileAsync(
      [Body] FilesUploadFinishRequest filesUploadPartRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载云空间中的文件，如 PDF 文件。不包含飞书文档、电子表格以及多维表格等在线文档。该接口支持通过在请求头添加 Range 参数分片下载部分文件。
    /// </summary>
    /// <param name="file_token">文件的 token，示例值："boxcnabCdefgabcef"。</param>
    /// <param name="range">
    /// <para> 在 HTTP 请求头中，通过指定 Range 来下载文件的部分内容，单位是字节（byte）。</para>
    /// <para> 该参数的格式为 Range: bytes=start-end，示例值为 Range: bytes=0-1024，表示下载第 0 个字节到第 1024 个字节之间的数据。</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/files/{file_token}/download")]
    Task<byte[]?> DownloadFileAsync(
        [Path] string file_token,
        [Header("Range")] string? range = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>用于创建导入文件的任务，并返回导入任务 ID。导入文件指将本地文件如 Word、TXT、Markdown、Excel 等格式的文件导入为某种格式的飞书在线云文档。</para>
    /// <para>该接口为异步接口，需要继续调用查询导入任务结果接口获取导入结果。</para>
    /// </summary>
    /// <param name="importTasksRequest">创建导入任务请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/import_tasks")]
    Task<FeishuApiResult<TasksResult>?> CreateImportTaskAsync(
      [Body] ImportTasksRequest importTasksRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>根据创建导入任务返回的导入任务 ID（ticket）轮询导入结果。</para>
    /// </summary>
    /// <param name="ticket">导入任务 ID。示例值："7369583175086912356"。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/import_tasks/{ticket}")]
    Task<FeishuApiResult<ImportTaskResult>?> GetImportTaskAsync(
      [Path] string ticket,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>用于创建导出文件的任务，并返回导出任务 ID。导出文件指将飞书文档、电子表格、多维表格导出为本地文件，包括 Word、Excel、PDF、CSV 格式。</para>
    /// <para>该接口为异步接口，需要继续调用查询导出任务结果接口获取导出结果。</para>
    /// </summary>
    /// <param name="exportTasksRequest">创建导出任务请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/export_tasks")]
    Task<FeishuApiResult<TasksResult>?> CreateExportTaskAsync(
     [Body] ExportTasksRequest exportTasksRequest,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>根据创建导出任务返回的导出任务 ID（ticket）轮询导出任务结果，并返回导出文件的 token。</para>
    /// <para>你可使用该 token 继续调用下载导出文件接口将导出的产物下载到本地。</para>
    /// </summary>
    /// <param name="ticket">导出任务 ID。示例值："7369583175086912356"。</param>
    /// <param name="token">要导出的云文档的 token。示例值："docbcZVGtv1papC6jAVGiyabcef"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/export_tasks/{ticket}")]
    Task<FeishuApiResult<ExportTasksResult>?> GetExportTaskAsync(
    [Path] string ticket,
    [Query] string? token,
    CancellationToken cancellationToken = default);


    /// <summary>
    /// 根据查询导出任务结果返回的导出文件的 token，下载导出产物到本地。
    /// </summary>
    /// <remarks>你需及时下载导出的文件。在导出任务结束 10 分钟后，导出的文件将被删除，导致无法下载。</remarks>
    /// <param name="file_token">导出的文件的 token，示例值："boxcnabCdefgabcef"。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/export_tasks/file/{file_token}/download")]
    Task<byte[]?> DownloadExportFileAsync([Path] string file_token, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据查询导出任务结果返回的导出文件的 token，下载导出产物到本地（适应于导出的大文件下载）。
    /// </summary>
    /// <remarks>你需及时下载导出的文件。在导出任务结束 10 分钟后，导出的文件将被删除，导致无法下载。</remarks>
    /// <param name="file_token">导出的文件的 token，示例值："boxcnabCdefgabcef"。</param>
    /// <param name="localFile">需要保存的本地文件。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/export_tasks/file/{file_token}/download")]
    Task DownloadExportLargeFileAsync([Path] string file_token, [FilePath] string localFile, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定云文档的点赞者列表并按点赞时间由近到远分页返回。
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="file_type">
    /// <para>必填：是</para>
    /// <para>云文档类型，如果该值为空或者与云文档实际类型不匹配，接口会返回失败。</para>
    /// <para>示例值：doc</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档</item>
    /// <item>docx：新版文档</item>
    /// <item>file：文件</item>
    /// </list>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v2/files/{file_token}/likes")]
    Task<FeishuApiPageListResult<FileLikeInfo>?> GetFileLikePageListByFileTokenAsync(
       [Path] string? file_token,
       [Query("file_type")] string file_type,
       [Query("page_size")] int page_size = Consts.PageSize,
       [Query("page_token")] string? page_token = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
