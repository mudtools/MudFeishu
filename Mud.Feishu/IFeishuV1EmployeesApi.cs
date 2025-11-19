// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
    /// <param name="access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="userModel">创建的员工请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/directory/v1/employees")]
    Task<FeishuApiResult<EmployeeCreateResult>> CreateEmployeeAsync(
        [Token(TokenType.Both)][Header("Authorization")] string access_token,
        [Body] EmployeeCreateRequest userModel,
        [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于更新在职/离职员工的信息、冻结/恢复员工。未传递的参数不会进行更新。员工指飞书企业内身份为「Employee」的成员，等同于通讯录OpenAPI中的「User」。
    /// </summary>
    /// <param name="access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="employee_id">员工ID</param>
    /// <param name="userModel">更新的员工请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}")]
    Task<FeishuNullDataApiResult> UpdateEmployeeAsync(
       [Token(TokenType.Both)][Header("Authorization")] string access_token,
       [Path] string employee_id,
       [Body] EmployeeUpdateRequest userModel,
       [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
       [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于离职员工。
    /// <para>本接口支持tenant_access_token和user_access_token。</para>
    /// <para>使用tenant_access_token时，只能在当前应用的通讯录授权范围内离职员工。</para>
    /// <para>若员工归属于多个部门，应用需要有员工所有所属部门的权限，才能离职成功。</para>
    /// <para>使用user_access_token 时，默认为管理员用户，将校验管理员管理范围。当用户有多个管理员身份均可离职员工时，管理员管理范围取最大集。</para>
    /// </summary>
    /// <param name="access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="employee_id">员工ID</param>
    /// <param name="deleteEmployeeRequest">离职员工请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}")]
    Task<FeishuNullDataApiResult> DeleteEmployeeByIdAsync(
      [Token(TokenType.Both)][Header("Authorization")] string access_token,
      [Path] string employee_id,
      [Body] DeleteEmployeeRequest deleteEmployeeRequest,
      [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于恢复已离职的成员，恢复已离职成员至在职状态。
    /// </summary>
    /// <param name="access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="employee_id">员工ID</param>
    /// <param name="resurrectEmployeeRequest">恢复离职员工请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("https://open.feishu.cn/open-apis/directory/v1/employees/{employee_id}/resurrect")]
    Task<FeishuNullDataApiResult> ResurrectEmployeeAsync(
      [Token(TokenType.Both)][Header("Authorization")] string access_token,
      [Path] string employee_id,
      [Body] ResurrectEmployeeRequest resurrectEmployeeRequest,
      [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
      [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
      CancellationToken cancellationToken = default);
}
