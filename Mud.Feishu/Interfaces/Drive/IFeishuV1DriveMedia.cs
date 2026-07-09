// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.DataModels.Drive.Media;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 素材指在文档、电子表格、多维表格等中用到的资源素材，如文档中的图片、视频或文件等。每个素材都有唯一的 token 作为标识。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1DriveMedia : IFeishuAppContextSwitcher
{
    /// <summary>
    /// <para>将文件、图片、视频等素材上传到指定云文档中。素材将显示在对应云文档中，在云空间中不会显示。</para>
    /// <para>## 使用限制</para>
    /// <para>- 素材大小不得超过 20 MB。要上传大于 20 MB 的文件，你需使用分片上传素材相关接口。详情参考[素材概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/media/introduction)。</para>
    /// <para>- 该接口调用频率上限为 5 QPS，10000 次/天。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/upload_all">接口文档</see></para>
    /// </summary>
    /// <param name="mediasUploadAllRequest">上传素材请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/medias/upload_all")]
    Task<FeishuApiResult<FilesUploadAllResult>?> UploadAllMediaAsync(
      [FormContent] MediasUploadAllRequest mediasUploadAllRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>发送初始化请求，以获取上传事务 ID 和分片策略，为[上传素材分片]做准备。平台固定以 4MB 的大小对素材进行分片。</para>
    /// <para>## 注意事项</para>
    /// <para>上传事务 ID 和上传进度在 24 小时内有效。请及时保存和恢复上传。</para>
    /// <para>## 使用限制</para>
    /// <para>该接口调用频率上限为 5 QPS，10000 次/天。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/multipart-upload-media/upload_prepare">接口文档</see></para>
    /// </summary>
    /// <param name="mediasUploadPrepareRequest">预上传请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/medias/upload_prepare")]
    Task<FeishuApiResult<FilesUploadPrepareResult>?> UploadPrepareMediaAsync(
       [Body] MediasUploadPrepareRequest mediasUploadPrepareRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 根据 预上传接口返回的上传事务 ID 和分片策略上传对应的文件分片。
    /// <para>上传完成后，需调用分片上传文件（完成上传）触发完成上传。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/multipart-upload-media/upload_part">接口文档</see></para>
    /// </summary>
    /// <param name="filesUploadPartRequest"> 分片上传文件-上传分片请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/medias/upload_part")]
    Task<FeishuNullDataApiResult?> UploadPartMediaAsync(
       [FormContent] FilesUploadPartRequest filesUploadPartRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 将分片全部上传完毕后，需调用本接口触发完成上传。否则将上传失败。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/multipart-upload-media/upload_finish">接口文档</see></para>
    /// </summary>
    /// <param name="filesUploadPartRequest">分片上传文件-完成上传请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/medias/upload_finish")]
    Task<FeishuApiResult<FilesUploadFinishResult>?> UploadFinishMediaAsync(
      [Body] FilesUploadFinishRequest filesUploadPartRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载各类云文档中的素材，例如电子表格中的图片。该接口支持通过在请求头添加Range 参数分片下载素材。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/download">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件的 token，示例值："boxcnabCdefgabcef"。</param>
    /// <param name="extra">拥有高级权限的多维表格在下载素材时，需要添加额外的扩展信息作为 URL 查询参数鉴权。</param>
    /// <param name="range">
    /// <para> 在 HTTP 请求头中，通过指定 Range 来下载文件的部分内容，单位是字节（byte）。</para>
    /// <para> 该参数的格式为 Range: bytes=start-end，示例值为 Range: bytes=0-1024，表示下载第 0 个字节到第 1024 个字节之间的数据。</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/medias/{file_token}/download")]
    Task<byte[]?> DownloadFileAsync(
        [Path] string file_token,
        [Query("extra")] string extra,
        [Header("Range")] string? range = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>该接口用于获取云文档中素材的临时下载链接。链接的有效期为 24 小时，过期失效。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/media/batch_get_tmp_download_url">接口文档</see></para>
    /// <para>## 前提条件</para>
    /// <para>调用此接口之前，你需确保应用已拥有素材的下载权限。否则接口将返回 403 的 HTTP 状态码。参考[云空间常见问题](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/faq)第 3 点了解如何分享素材的下载权限给应用。更多云文档接口权限问题，参考[云文档常见问题](https://open.feishu.cn/document/ukTMukTMukTM/uczNzUjL3czM14yN3MTN)。</para>
    /// <para>## 注意事项</para>
    /// <para>本接口仅支持下载云文档而非云空间中的资源文件。如要下载云空间中的资源文件，需调用[下载文件](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/file/download)接口。</para>
    /// </summary>
    /// <param name="file_token">文件的 token，示例值："boxcnabCdefgabcef"。</param>
    /// <param name="extra">拥有高级权限的多维表格在下载素材时，需要添加额外的扩展信息作为 URL 查询参数鉴权。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/medias/batch_get_tmp_download_url")]
    Task<FeishuApiResult<BatchGetTmpDownloadUrlResult>?> BatchGetTmpDownloadUrlAsync(
         [Query("file_tokens")] string file_token,
         [Query("extra")] string extra,
         CancellationToken cancellationToken = default);
}
