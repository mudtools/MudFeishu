namespace Mud.Feishu;

/// <summary>
/// 企业人员信息管理相关的API
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManage), WrapInterface = "IFeishuUser")]
public interface IFeishuUserApi
{
    /// <summary>
    /// 向通讯录创建一个用户（该动作可以理解为员工入职）。成功创建用户后，系统会以短信或邮件的形式向用户发送邀请，用户在同意邀请后方可访问企业或团队。
    /// </summary>
    /// <param name="user_access_token">tenant_access_token</param>
    /// <param name="userModel">创建的用户请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="client_token">用于幂等判断是否为同一请求，避免重复创建。请参考参数示例值，传入自定义的 client_token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/users")]
    Task<FeishuApiResult<CreateOrUpdateUserResult>> CreateUserAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Body] CreateUserRequest userModel,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("client_token")] string? client_token = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新通讯录中指定用户的信息，包括名称、邮箱、手机号、所属部门以及自定义字段等信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="userModel">用于更新的用户请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}")]
    Task<FeishuApiResult<CreateOrUpdateUserResult>> UpdateUserAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Path] string user_id,
        [Body] UpdateUserRequest userModel,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新用户 ID
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id">用户 ID，ID 类型与查询参数 user_id_type 的取值保持一致。</param>
    /// <param name="updateUserId">自定义新的用户 user_id。长度不能超过 64 字符。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}/update_user_id")]
    Task<FeishuNullDataApiResult> UpdateUserIdAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Path] string user_id,
        [Body] UpdateUserIdRequest updateUserId,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取通讯录中某一用户的信息，包括用户 ID、名称、邮箱、手机号、状态以及所属部门等信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id">用户ID。ID 类型与查询参数 user_id_type 保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}")]
    Task<FeishuApiResult<GetUserInfoResult>> GetUserByIdAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Path] string user_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取通讯录中用户的信息，包括用户 ID、名称、邮箱、手机号、状态以及所属部门等信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <param name="user_ids">用户ID。ID 类型与查询参数 user_id_type 保持一致。如需一次查询多个用户ID，可多次传递同一参数名，并且每次传递不同的参数值。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/users/batch")]
    Task<FeishuApiResult<GetUserInfosResult>> GetUserByIdsAsync(
       [Token][Header("Authorization")] string user_access_token,
       [Query("user_ids")] string[] user_ids,
       [Query("user_id_type")] string? user_id_type = null,
       [Query("department_id_type")] string? department_id_type = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定部门直属的用户信息列表。用户信息包括用户 ID、名称、邮箱、手机号以及状态等信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <param name="department_id">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/users/find_by_department")]
    Task<FeishuApiResult<GetUserInfosResult>> GetUserByDepartmentIdAsync(
     [Token][Header("Authorization")] string user_access_token,
     [Query("department_id")] string department_id,
     [Query("page_size")] int page_size = 10,
     [Query("page_token")] string? page_token = null,
     [Query("user_id_type")] string? user_id_type = null,
     [Query("department_id_type")] string? department_id_type = null,
     CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过手机号或邮箱获取一个或多个用户的 ID （包括 user_id、open_id、union_id）与状态信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="queryRequest">查询参数请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>

    [Post("https://open.feishu.cn/open-apis/contact/v3/users/batch_get_id")]
    Task<FeishuApiResult<UserQueryListResult>> GetBatchUsersAsync(
      [Token][Header("Authorization")] string user_access_token,
      [Body] UserQueryRequest queryRequest,
      [Query("user_id_type")] string? user_id_type = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过用户名关键词搜索其他用户的信息，包括用户头像、用户名、用户所在部门、用户 user_id 以及 open_id。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="query">搜索关键词，接口通过传入的关键词搜索相匹配的用户名。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/search/v1/user")]
    Task<FeishuApiResult<UserSearchListResult>> GetUsersByKeywordAsync(
     [Token][Header("Authorization")] string user_access_token,
     [Query("query")] string query,
     [Query("page_size")] int page_size = 10,
     [Query("page_token")] string? page_token = null,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 从通讯录内删除一个指定用户（该动作可以理解为员工离职），删除时可通过请求参数将用户所有的群组、文档、日程和应用等数据转让至他人。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id">用户 ID。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="deleteSettingsRequest">用户删除参数请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}")]
    Task<FeishuNullDataApiResult> DeleteUserByIdAsync(
       [Token][Header("Authorization")] string user_access_token,
       [Path] string user_id,
       [Body] DeleteSettingsRequest deleteSettingsRequest,
       [Query("user_id_type")] string? user_id_type = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于恢复已删除用户（已离职的成员）。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="user_id">用户 ID。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="resurrectUserRequest">恢复已删除用户操作的请求体。</param>
    /// <param name="department_id_type">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}/resurrect")]
    Task<FeishuNullDataApiResult> ResurrectUserByIdAsync(
      [Token][Header("Authorization")] string user_access_token,
      [Path] string user_id,
      [Body] ResurrectUserRequest resurrectUserRequest,
      [Query("user_id_type")] string? user_id_type = null,
      [Query("department_id_type")] string? department_id_type = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过 user_access_token 获取相关用户信息。
    /// </summary>
    /// <param name="user_access_token">user_access_token 以登录用户身份调用 API，可读写的数据范围由用户可读写的数据范围决定。参考获取 user_access_token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/authen/v1/user_info")]
    Task<FeishuApiResult<GetUserDataResult>> GetUserInfoAsync(
        [Token][Header("Authorization")] string user_access_token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 该接口用于退出用户的登录态
    /// </summary>
    /// <param name="user_access_token">user_access_token 以登录用户身份调用 API</param>
    /// <param name="user_id_type">用户 ID 类型，非必填项。</param>
    /// <param name="logoutRequest">退出登录请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/passport/v1/sessions/logout")]
    Task<FeishuNullDataApiResult> LogoutAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Query("user_id_type")] string user_id_type,
        [Body] LogoutRequest logoutRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于返回调用 JSAPI 临时调用凭证，使用该凭证调用 JSAPI 时，请求不会被拦截。
    /// </summary>
    /// <param name="tenant_access_token">
    /// tenant_access_token
    /// <para>值格式："Bearer access_token"</para>
    /// <para>示例值："Bearer t-7f1bcd13fc57d46bac21793a18e560"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/jssdk/ticket/get")]
    Task<FeishuApiResult<TicketData>> GetJsTicketAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        CancellationToken cancellationToken = default);

}
