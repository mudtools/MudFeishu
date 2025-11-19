using Mud.Feishu.DataModels.WorkCites;

namespace Mud.Feishu;

/// <summary>
/// 工作城市是用户属性之一，通过工作城市 API 仅支持查询工作城市信息。
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV3WorkCity))]
public interface IFeishuV3WorkCityApi
{
    /// <summary>
    /// 获取当前租户下所有工作城市信息，包括工作城市的 ID、名称、多语言名称以及启用状态。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/work_cities")]
    Task<FeishuApiListResult<WorkCity>> GetTenantWorkCitesListAsync(
         [Token][Header("Authorization")] string tenant_access_token,
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前登录用户下所有工作城市信息，包括工作城市的 ID、名称、多语言名称以及启用状态。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/work_cities")]
    Task<FeishuApiListResult<WorkCity>> GetUserWorkCitesListAsync(
         [Token(TokenType.UserAccessToken)][Header("Authorization")] string user_access_token,
         [Query("page_size")] int page_size = 10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定工作城市的信息，包括工作城市的 ID、名称、多语言名称以及启用状态。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="work_city_id">工作城市 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/work_cities/{work_city_id}")]
    Task<FeishuApiResult<WorkCityResult>> GetTenantWorkCityByIdAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Path] string work_city_id,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定工作城市的信息，包括工作城市的 ID、名称、多语言名称以及启用状态。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="work_city_id">工作城市 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/work_cities/{work_city_id}")]
    Task<FeishuApiResult<WorkCityResult>> GetUserWorkCityByIdAsync(
       [Token][Header("Authorization")] string user_access_token,
       [Path] string work_city_id,
       CancellationToken cancellationToken = default);
}
