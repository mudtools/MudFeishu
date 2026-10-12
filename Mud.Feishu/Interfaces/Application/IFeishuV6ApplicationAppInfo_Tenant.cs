// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用信息查询接口，用于获取应用的基础信息。
/// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application/get">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationAppInfo : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取应用信息。
    /// <para>根据 app_id 获取应用的基础信息。商店应用必须正式发布版本后，才可以调用该接口获取应用信息。如果灰度发布应用，调用该接口将会报错 210504 错误码。</para>
    /// <para>仅查询本应用信息时，app_id 可传入 "me"。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application/get">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID。查询其他应用信息时必须申请「获取应用信息」权限。仅查询本应用信息时，可传入 "me" 或本应用的 app_id</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </param>
    /// <param name="lang">
    /// <para>获取应用信息的语言</para>
    /// <para>可选值：zh_cn（中文）、en_us（英文）、ja_jp（日语）</para>
    /// <para>示例值：zh_cn</para>
    /// </param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/applications/{app_id}")]
    Task<FeishuApiResult<GetApplicationResult>?> GetApplicationAsync(
      [Path] string app_id,
      [Query] string lang,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
