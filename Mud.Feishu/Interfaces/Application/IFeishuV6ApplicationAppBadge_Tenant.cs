// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用红点接口，用于更新工作台场景下的应用红点信息。
/// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/app_badge/set">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationAppBadge : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 更新应用红点。
    /// <para>更新工作台场景下指定用户的应用红点信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/app_badge/set">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新应用红点请求体</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v6/app_badge/set")]
    Task<FeishuNullDataApiResult?> SetAppBadgeAsync(
      [Body] SetAppBadgeRequest request,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
