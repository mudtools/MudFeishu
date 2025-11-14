using Mud.Feishu.DataModels.EmployeeType;

namespace Mud.Feishu;

/// <summary>
/// 飞书人员类型相关的API接口函数。
/// </summary>
[HttpClientApi]
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
    Task<ApiResult<EmployeeTypeEnumResult>> CreateEmployeeTypeAsync(
        [Header("Authorization")] string user_access_token,
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
    Task<ApiResult<EmployeeTypeEnumResult>> UpdateEmployeeTypeAsync(
        [Header("Authorization")] string user_access_token,
        [Path] string enum_id,
        [Body] EmployeeTypeEnumRequest groupInfoRequest,
        CancellationToken cancellationToken = default);
}
