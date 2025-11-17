namespace Mud.Feishu;

/// <summary>
/// 飞书人员类型相关的API接口函数。
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManage), WrapInterface = "IFeishuEmployeeType")]
public interface IFeishuEmployeeTypeApi
{
    /// <summary>
    /// 新增一个自定义的人员类型。人员类型是用户属性之一，用于灵活标记用户的身份类型。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="groupInfoRequest">新增人员类型请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums")]
    Task<FeishuApiResult<EmployeeTypeEnumResult>> CreateEmployeeTypeAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Body] EmployeeTypeEnumRequest groupInfoRequest,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定的自定义人员类型信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="enum_id">自定义人员类型的选项 ID。可以在新建人员类型时从返回值中获取，也可以调用查询人员类型接口，获取选项的 ID。</param>
    /// <param name="groupInfoRequest">新增人员类型请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums/{enum_id}")]
    Task<FeishuApiResult<EmployeeTypeEnumResult>> UpdateEmployeeTypeAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Path] string enum_id,
        [Body] EmployeeTypeEnumRequest groupInfoRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询当前租户下所有的人员类型信息，包括选项 ID、类型、编号以及内容等。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums")]
    Task<FeishuApiResult<EmployeeTypeEnumListResult>> GetEmployeeTypesAsync(
       [Token][Header("Authorization")] string user_access_token,
       [Query("page_size")] int page_size = 10,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定的自定义人员类型。
    /// <para>仅支持删除自定义的人员类型。默认包含的正式、实习、外包、劳务、顾问五个选项不支持删除。</para>
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="enum_id">自定义人员类型的选项 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/contact/v3/employee_type_enums/{enum_id}")]
    Task<FeishuNullDataApiResult> DeleteEmployeeTypeByIdAsync(
         [Token][Header("Authorization")] string user_access_token,
         [Path] string enum_id,
         CancellationToken cancellationToken = default);
}
