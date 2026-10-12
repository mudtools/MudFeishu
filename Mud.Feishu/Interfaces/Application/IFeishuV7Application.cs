// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书应用管理接口（v7），用于更新自建应用的基础信息、应用能力、开发配置，提交应用发布，以及上传应用图标。
/// <para>修改应用配置后需要提交发布并审核通过后才会在线上生效（免审配置除外）。</para>
/// <para>当前接口不能直接调用，仅为子接口的公共方法抽象</para>
/// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v7/application-v7/application-base/patch">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV7Application : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 更新应用基础信息。
    /// <para>更新自建应用的基础信息（名称、头像等），不传入的参数保持不变，仅更新传入的参数。应用审核中时无法更新配置。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v7/application-v7/application-base/patch">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a306c5476fb8d00c</para>
    /// </param>
    /// <param name="request">更新应用基础信息请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/application/v7/applications/{app_id}/base")]
    Task<FeishuNullDataApiResult?> UpdateApplicationBaseAsync(
      [Path] string app_id,
      [Body] UpdateApplicationBaseRequest request,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用能力。
    /// <para>更新自建应用的应用能力（机器人、网页应用等）相关配置，不传入的参数保持不变，仅更新传入的参数。应用审核中时无法更新配置。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v7/application-v7/application-ability/patch">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a42d0b833ab8d01b</para>
    /// </param>
    /// <param name="request">更新应用能力请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/application/v7/applications/{app_id}/ability")]
    Task<FeishuNullDataApiResult?> UpdateApplicationAbilityAsync(
      [Path] string app_id,
      [Body] UpdateApplicationAbilityRequest request,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用开发配置。
    /// <para>更新自建应用的开发配置（通讯录、安全、可见性等），不传入的参数保持不变，仅更新传入的参数。应用审核中时无法更新配置。免审权限、事件订阅服务器地址、重定向 URL、IP 白名单、H5 可信域名、协议名白名单修改后立即生效，其他配置修改后需提交发布。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v7/application-v7/application-config/patch">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a306c5476fb8d00c</para>
    /// </param>
    /// <param name="request">更新应用开发配置请求体</param>
    /// <param name="department_id_type">部门 ID 类型，默认值：open_department_id。可选值：open_department_id、department_id</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/application/v7/applications/{app_id}/config")]
    Task<FeishuNullDataApiResult?> UpdateApplicationConfigAsync(
      [Path] string app_id,
      [Body] UpdateApplicationConfigRequest request,
      [Query] string? department_id_type = Consts.Department_Id_Type,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 提交发布自建应用。
    /// <para>自建应用提交应用发布。如果当前自建应用没有待发布的版本，则会自动创建一个版本；如果有待发布的版本，则直接提交该版本。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v7/application-v7/application-publish/create">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a508dbf34038d01c</para>
    /// </param>
    /// <param name="request">提交发布自建应用请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v7/applications/{app_id}/publish")]
    Task<FeishuApiResult<SubmitApplicationPublishResult>?> SubmitApplicationPublishAsync(
      [Path] string app_id,
      [Body] SubmitApplicationPublishRequest request,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 上传应用图标。
    /// <para>上传自建应用的图标图片，用于创建或更新应用头像。支持 JPEG/PNG/SVG/BMP 格式，最大 2 MB，尺寸大于 240 * 240 px，不带圆角。接口频率限制：10 次/分钟。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v7/application-v7/app_avatar-upload/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">上传应用图标请求体（multipart/form-data）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v7/app_avatar/upload")]
    Task<FeishuApiResult<UploadAppAvatarResult>?> UploadAppAvatarAsync(
      [FormContent] UploadAppAvatarRequest request,
      CancellationToken cancellationToken = default);
}
