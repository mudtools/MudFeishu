// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu;

/// <summary>
/// 通讯录授权范围用于描述当前应用可读写的部门、用户与用户组集合，便于应用在授权边界内安全地访问通讯录数据。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// <para><see href="https://open.feishu.cn/document/server-docs/contact-v3/scope/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Organization")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV3Scope : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 分页获取应用的通讯录授权范围，返回已授权的部门、用户与用户组 ID 列表。
    /// <para>**注意**：所有资源列表长度之和不超过 page_size，返回顺序依次为 user_ids、department_ids、group_ids。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/contact-v3/scope/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">返回值中用户 ID 的类型。</param>
    /// <param name="department_id_type">返回值中部门 ID 的类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/contact/v3/scopes")]
    Task<FeishuApiPageListResult<ListScopeResult>?> GetScopesPageListAsync(
       [Query("page_size")] int? page_size = Consts.PageSize_10,
       [Query("page_token")] string? page_token = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
       CancellationToken cancellationToken = default);
}
