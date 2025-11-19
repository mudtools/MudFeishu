using Mud.Feishu.DataModels.Employees;

namespace Mud.Feishu;

/// <summary>
/// 员工指飞书企业内身份为「Employee」的成员，等同于通讯录OpenAPI中的「User」。
/// <para>员工在飞书的身份标识包括employee_id、open_id 和 union_id，其中employee_id的值等同于通讯录中的 user_id，其余两个也和通讯录的User的值相同。</para>
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV1Employees))]
public interface IFeishuV1EmployeesApi
{
    /// <summary>
    /// 用于在企业下创建员工。支持传入姓名、手机号等信息，生成在职状态的员工对象。员工指飞书企业内身份为「Employee」的成员，等同于通讯录OpenAPI中的「User」。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="userModel">创建的员工请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/directory/v1/employees")]
    Task<FeishuApiResult<EmployeeCreateResult>> CreateTenantEmployeeAsync(
        [Token][Header("Authorization")] string tenant_access_token,
        [Body] EmployeeRequest userModel,
        [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于在企业下创建员工。支持传入姓名、手机号等信息，生成在职状态的员工对象。员工指飞书企业内身份为「Employee」的成员，等同于通讯录OpenAPI中的「User」。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="userModel">创建的员工请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/directory/v1/employees")]
    Task<FeishuApiResult<EmployeeCreateResult>> CreateUserEmployeeAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Body] EmployeeRequest userModel,
        [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);
}
