// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用版本查询接口，用于获取应用版本信息、应用版本列表，以及应用版本中开发者申请的通讯录权限范围。
/// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application/get-2">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationAppVersion : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取应用版本信息。
    /// <para>根据应用 ID 和应用版本 ID 来获取同租户下的应用版本的信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID。查询其他应用版本信息时必须申请「获取应用版本信息」权限。仅查询本应用版本信息时，可传入 "me" 或本应用的 app_id</para>
    /// <para>示例值：cli_9f3ca975326b501b</para>
    /// </param>
    /// <param name="version_id">
    /// <para>应用版本 ID，唯一标识一个应用版本</para>
    /// <para>示例值：oav_d317f090b7258ad0372aa53963cda70d</para>
    /// </param>
    /// <param name="lang">
    /// <para>获取应用信息的语言</para>
    /// <para>可选值：zh_cn（中文）、en_us（英文）、ja_jp（日语）</para>
    /// <para>示例值：zh_cn</para>
    /// </param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/applications/{app_id}/app_versions/{version_id}")]
    Task<FeishuApiResult<GetAppVersionResult>?> GetAppVersionAsync(
      [Path] string app_id,
      [Path] string version_id,
      [Query] string lang,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取应用版本列表。
    /// <para>根据 app_id 获取对应应用版本列表。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID。查询其他应用版本信息时必须申请「获取应用版本信息」权限。仅查询本应用版本信息时，可传入 "me" 或本应用的 app_id</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </param>
    /// <param name="lang">
    /// <para>获取应用信息的语言</para>
    /// <para>可选值：zh_cn（中文）、en_us（英文）、ja_jp（日语）</para>
    /// <para>示例值：zh_cn</para>
    /// </param>
    /// <param name="page_size">分页大小，取值范围：1 ～ 50。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果。分页标记有效期为 2 小时</param>
    /// <param name="order">排序方式。0：按创建时间从大到小；1：按创建时间从小到大。默认值：0</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/applications/{app_id}/app_versions")]
    Task<FeishuApiPageListResult<AppVersionInfo>?> GetAppVersionsPageListAsync(
      [Path] string app_id,
      [Query] string lang,
      [Query] int? page_size = Consts.PageSize_20,
      [Query] string? page_token = null,
      [Query] int? order = 0,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取应用版本中开发者申请的通讯录权限范围。
    /// <para>根据应用的 App ID 和版本 ID 获取企业自建应用某个版本的通讯录权限范围。由于通讯录权限范围需要提交发布新的应用版本并经企业管理员审核通过后才会生效，该权限范围可能与实际生效的权限范围有差别。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application/contacts_range_suggest">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID。查询其他应用时必须申请「获取应用版本信息」权限。仅查询本应用时，可传入 "me" 或本应用的 app_id</para>
    /// <para>示例值：cli_9f3ca975326b501b</para>
    /// </param>
    /// <param name="version_id">
    /// <para>应用版本 ID，可通过获取应用版本列表接口获取</para>
    /// <para>示例值：oav_d317f090b7258ad0372aa53963cda70d</para>
    /// </param>
    /// <param name="department_id_type">返回的部门 ID 类型，默认值：open_department_id。可选值：department_id、open_department_id</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/applications/{app_id}/app_versions/{version_id}/contacts_range_suggest")]
    Task<FeishuApiResult<GetContactsRangeSuggestResult>?> GetContactsRangeSuggestAsync(
      [Path] string app_id,
      [Path] string version_id,
      [Query] string? department_id_type = Consts.Department_Id_Type,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
