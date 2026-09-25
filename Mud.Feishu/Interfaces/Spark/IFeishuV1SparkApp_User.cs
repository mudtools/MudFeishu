// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu;


/// <summary>
/// 飞书妙搭（Spark）应用 SDK 是一组服务端 OpenAPI 的封装，用于以用户身份创建、更新妙搭应用、上传图标、发布 HTML 代码与管理应用可用范围，并继承双令牌只读端点（批量查询应用、AI 额度、运营数据）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Spark", InheritedFrom = nameof(FeishuV1SparkApp))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1SparkApp : IFeishuV1SparkApp, ICurrentUserId
{
    /// <summary>
    /// 创建妙搭应用
    /// <para>创建一个新的妙搭应用，返回应用详细信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（name 必填；可选 app_type、description、icon_url）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回创建后的妙搭应用信息（app_id、app_type、name 等）</returns>
    [Post("/open-apis/spark/v1/apps")]
    Task<FeishuApiResult<AppResult>?> CreateAppAsync(
        [Body] CreateAppRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新妙搭应用信息
    /// <para>更新应用名称、描述或图标地址，未传字段保持不变。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/patch">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用唯一标识，示例值：app_4k6af8utt2s0n</param>
    /// <param name="request">更新请求体（name、description、icon_url 均可选）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的妙搭应用信息</returns>
    [Patch("/open-apis/spark/v1/apps/{app_id}")]
    Task<FeishuApiResult<AppResult>?> PatchAppAsync(
        [Path] string app_id,
        [Body] PatchAppRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 上传妙搭应用图标
    /// <para>上传应用图标文件（multipart/form-data），返回图标访问 URL，可用于创建或更新应用的 icon_url。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/icon">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传请求体（file：图标文件本地路径，建议 PNG/JPG、128×128 像素）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回上传成功后的图标访问 URL</returns>
    [Post("/open-apis/spark/v1/icon")]
    Task<FeishuApiResult<UploadAppIconResult>?> UploadAppIconAsync(
        [FormContent] UploadAppIconRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 上传 HTML 代码并发布
    /// <para>上传 tar 格式的 HTML 代码文件并直接发布应用，返回发布成功后的在线访问地址。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/upload_html_code_and_release">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用唯一标识，通过应用创建接口或妙搭管理后台获取，示例值：app_4k6af8utt2s0n</param>
    /// <param name="request">上传请求体（file：tar 格式的 HTML 文件本地路径）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回发布成功后的在线访问地址</returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/upload_and_release_html_code")]
    Task<FeishuApiResult<UploadHtmlCodeResult>?> UploadHtmlCodeAndReleaseAsync(
        [Path] string app_id,
        [FormContent] UploadHtmlCodeRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取妙搭应用可用范围
    /// <para>查询应用的可见范围类型（All/Tenant/Range）、授权用户/部门/群聊列表与申请访问配置。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/get_app_visibility">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用唯一标识，通过应用创建接口或妙搭管理后台获取，示例值：app_4k6af8utt2s0n</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回可见范围类型、授权对象列表与申请访问配置</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/access-scope")]
    Task<FeishuApiResult<GetAppVisibilityResult>?> GetAppVisibilityAsync(
        [Path] string app_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新妙搭应用可用范围
    /// <para>设置应用的可见范围类型（Public/Tenant/Range）及授权对象、申请访问配置与登录要求。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/update_app_visibility">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用的唯一标识，示例值：app_4k6af8utt2s0n</param>
    /// <param name="request">更新请求体（scope 必填：Public/Tenant/Range；users、departments、chats 仅 Scope=Range 时生效；可选 apply_config、require_login）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>更新成功时 data 为空对象</returns>
    [Put("/open-apis/spark/v1/apps/{app_id}/access-scope")]
    Task<FeishuNullDataApiResult?> UpdateAppVisibilityAsync(
        [Path] string app_id,
        [Body] UpdateAppVisibilityRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
