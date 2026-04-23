// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive.FileVersions;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 文件版本是基于文件生成的新版本，版本依附于文件而存在。飞书开放平台支持基于在线文档和电子表格创建、删除和获取版本信息。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1DriveFilesVersions : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建文档版本。文档支持在线文档或电子表格。该接口为异步接口。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/create">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件 token。示例值：doccnfYZzTlvXqZIGTdAHKabcef</param>
    /// <param name="createFileVersionRequest">创建文档版本请求体</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/drive/v1/files/{file_token}/versions")]
    Task<FeishuApiResult<CreateFileVersionResult>?> CreateFileVersionAsync(
        [Path] string file_token,
        [Body] CreateFileVersionRequest createFileVersionRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文档版本列表。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/list">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="obj_type">
    /// <para>必填：是</para>
    /// <para>源文档的类型</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// </list>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files/{file_token}/versions")]
    Task<FeishuApiPageListResult<FileVersionInfo>?> GetFileVersionPageListByFileTokenAsync(
         [Path] string file_token,
         [Query("obj_type")] string obj_type,
         [Query("page_size")] int page_size = Consts.PageSize_10,
         [Query("page_token")] string? page_token = null,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于获取文档或电子表格指定版本的信息，包括标题、标识、创建者、创建时间等。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/get">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="version_id">版本文档的版本标识，示例值："fnJfyX"</param>
    /// <param name="obj_type">
    /// <para>必填：是</para>
    /// <para>源文档的类型</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// </list>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/drive/v1/files/{file_token}/versions/{version_id}")]
    Task<FeishuApiResult<FileVersionInfo>?> GetFileVersionByFileTokenAsync(
       [Path] string file_token,
       [Path] string version_id,
       [Query("obj_type")] string obj_type,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除基于在线文档或电子表格创建的版本。
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/drive-v1/file-version/delete">接口文档</see></para>
    /// </summary>
    /// <param name="file_token">文件的 token
    /// <para>示例值：XIHSdYSI7oMEU1xrsnxc8fabcef</para>
    /// </param>
    /// <param name="version_id">版本文档的版本标识，示例值："fnJfyX"</param>
    /// <param name="obj_type">
    /// <para>必填：是</para>
    /// <para>源文档的类型</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>docx：新版文档</item>
    /// <item>sheet：电子表格</item>
    /// </list>
    /// </param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/drive/v1/files/{file_token}/versions/{version_id}")]
    Task<FeishuNullDataApiResult?> DeleteFileVersionByFileTokenAsync(
       [Path] string file_token,
       [Path] string version_id,
       [Query("obj_type")] string obj_type,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
