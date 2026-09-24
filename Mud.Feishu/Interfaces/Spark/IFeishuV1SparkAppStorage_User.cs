// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙搭（Spark）文件存储 SDK 是一组服务端 OpenAPI 的封装，用于上传、下载及分片上传妙搭应用下的文件资源。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spark")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SparkAppStorage : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 上传文件
    /// <para>用于上传 20MB（含）以内的文件。接口频率限制 5 次/秒。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-storage/upload">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取，如 https://miaoda.feishu.cn/app/app_4jcn5n11bpf5v 中的 app_4jcn5n11bpf5v 即为 app_id</param>
    /// <param name="request">上传文件请求体（multipart：file_name、可选 check_sum、file 文件本地路径）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回上传后的文件信息（file_key、相对路径 file_url 等）</returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/storage/upload")]
    Task<FeishuApiResult<UploadStorageResult>?> UploadStorageAsync(
        [Path] string app_id,
        [FormContent] UploadStorageFileRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 下载文件
    /// <para>用于下载 20MB（含）以内的文件。支持通过请求头 Range 分片下载。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-storage/download">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="file_key">文件 ID。ID 和 URL 不能同时为空；都提供时优先使用 file_key。示例值：1859988692091946</param>
    /// <param name="file_url">文件 URL（相对路径）。ID 和 URL 不能同时为空</param>
    /// <param name="range">
    /// <para>在 HTTP 请求头中，通过指定 Range 下载文件的部分内容，单位是字节（byte）。</para>
    /// <para>格式为 Range: bytes=start-end，示例值为 Range: bytes=0-1024</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>
    /// 成功时返回响应的二进制内容（取自 <c>HttpContent.ReadAsByteArrayAsync</c>，不会为 <see langword="null"/>；空响应体对应空数组）。
    /// </returns>
    /// <exception cref="ApiException">
    /// 服务端返回非 2xx 状态码时抛出（由 HTTP 执行器统一抛出，异常携带 <c>StatusCode</c> 与响应内容）。
    /// <para>
    /// 注意：飞书部分业务错误以 HTTP 200 + JSON 错误体（<c>{"code":...,"msg":...}</c>）返回，
    /// 此时本方法会把错误 JSON 当作文件内容返回。落盘前应按 <c>Content-Type</c> 自检，详见 <c>documents/ErrorHandling.md</c>。
    /// </para>
    /// </exception>
    [Get("/open-apis/spark/v1/apps/{app_id}/storage")]
    Task<byte[]?> DownloadStorageAsync(
        [Path] string app_id,
        [Query("file_key")] string? file_key = null,
        [Query("file_url")] string? file_url = null,
        [Header("Range")] string? range = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 分片上传文件 - 创建上传请求
    /// <para>发送初始化请求，以获取上传请求 ID 和分片策略，为上传分片做准备。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-storage/upload_initialize">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="request">创建上传请求体（file_name、file_size；可选 mime_type）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回上传请求 ID（有效期 24h）与建议分片策略</returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/storage/upload/initialize")]
    Task<FeishuApiResult<UploadStorageInitializeResult>?> UploadStorageInitializeAsync(
        [Path] string app_id,
        [Body] UploadStorageInitializeRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 分片上传文件 - 上传分片
    /// <para>根据创建上传请求返回的上传请求 ID 和分片策略上传对应的文件分片。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-storage/upload_part">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="request">上传分片请求体（multipart：upload_id、chunk_index、分片文件本地路径、可选 chunk_check_sum）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>上传成功时 data 为空对象</returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/storage/upload/part")]
    Task<FeishuNullDataApiResult?> UploadStoragePartAsync(
        [Path] string app_id,
        [FormContent] UploadStoragePartRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 分片上传文件 - 完成上传
    /// <para>调用「上传分片」将分片全部上传完毕后，调用本接口触发完成上传。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app-storage/upload_complete">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 id，可从妙搭应用 URL 中获取</param>
    /// <param name="request">完成上传请求体（upload_id）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回上传完成后的文件信息</returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/storage/upload/complete")]
    Task<FeishuApiResult<UploadStorageResult>?> UploadStorageCompleteAsync(
        [Path] string app_id,
        [Body] UploadStorageCompleteRequest request,
        CancellationToken cancellationToken = default);
}
