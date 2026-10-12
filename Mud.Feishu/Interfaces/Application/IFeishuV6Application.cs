// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书应用协作者管理接口，用于转移应用所有者、更新应用协作者（添加/移除）以及查询应用协作者列表。
/// <para>当前接口不能直接调用，仅为子接口的公共方法抽象</para>
/// <para><see href="https://open.feishu.cn/document/application-v6/application/update-2">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV6Application : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 转移应用所有者。
    /// <para>将某个自建应用的所有者转移给另外一个人。以用户身份调用时，需确保该用户为应用当前的所有者。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/application/update-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a306c5476fb8d00c</para>
    /// </param>
    /// <param name="request">转移应用所有者请求体</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/application/v6/applications/{app_id}/owner")]
    Task<FeishuNullDataApiResult?> UpdateApplicationOwnerAsync(
      [Path] string app_id,
      [Body] UpdateApplicationOwnerRequest request,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用协作者。
    /// <para>为应用（包括自建应用和商店应用）添加/移除协作者，添加后协作者将会收到添加通知。以用户身份调用时，需确保该用户为应用的所有者或管理员。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/application/update">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a5002df1b6f8d01c</para>
    /// </param>
    /// <param name="request">更新应用协作者请求体</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/application/v6/applications/{app_id}/collaborators")]
    Task<FeishuNullDataApiResult?> UpdateApplicationCollaboratorsAsync(
      [Path] string app_id,
      [Body] UpdateApplicationCollaboratorsRequest request,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取应用协作者列表。
    /// <para>根据 app_id 获取应用（包括自建应用和商店应用）的协作者信息，包括应用的所有者、管理员、开发者、运营人员。</para>
    /// <para><see href="https://open.feishu.cn/document/application-v6/application/get-3">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_a5002df1b6f8d01c</para>
    /// </param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/applications/{app_id}/collaborators")]
    Task<FeishuApiResult<GetApplicationCollaboratorsResult>?> GetApplicationCollaboratorsAsync(
      [Path] string app_id,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
