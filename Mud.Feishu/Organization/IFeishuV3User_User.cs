// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu;

/// <summary>
/// 飞书用户是飞书通讯录中的基础资源，对应企业组织架构中的成员实体。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/user/field-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IUserTokenManager), RegistryGroupName = "Organization")]
[Header("Authorization")]
public interface IFeishuUserV3UserApi : IFeishuV3User
{
    /// <summary>
    /// 通过用户名关键词搜索其他用户的信息，包括用户头像、用户名、用户所在部门、用户 user_id 以及 open_id。
    /// </summary>
    /// <param name="query">搜索关键词，接口通过传入的关键词搜索相匹配的用户名。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/search/v1/user")]
    Task<FeishuApiResult<UserSearchListResult>?> GetUsersByKeywordAsync(
     [Query("query")] string query,
     [Query("page_size")] int page_size = 10,
     [Query("page_token")] string? page_token = null,
     CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过 user_access_token 获取相关用户信息。
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/authen/v1/user_info")]
    Task<FeishuApiResult<GetUserDataResult>> GetUserInfoAsync(
        CancellationToken cancellationToken = default);
}
