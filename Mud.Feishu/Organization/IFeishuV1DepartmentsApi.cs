// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.DepartmentsV1;

namespace Mud.Feishu;

/// <summary>
/// 部门是飞书组织架构里的一个基础实体，每个员工都归属于一个或多个部门。
/// <para>部门在飞书的身份标识包括department_id、open_department_id。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/directory-v1/department/overview"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "Organization")]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuV1Departments))]
public interface IFeishuV1DepartmentsApi
{
    /// <summary>
    /// 用于用于在企业组织机构中创建新部门，支持设置部门名称、父部门、负责人等信息。
    /// </summary>
    /// <param name="access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="departmentCreateRequest">创建部门的请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/directory/v1/departments")]
    Task<FeishuApiResult<DepartmentCreateUpdateRequest>> CreateDepartmentAsync(
           [Token(TokenType.Both)][Header("Authorization")] string access_token,
           [Body] DepartmentCreateUpdateRequest departmentCreateRequest,
           [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
           [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// 用于更新企业组织机构部门信息。仅更新显式传参的部分。
    /// </summary>
    /// <param name="access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="departmentUpdateRequest">更新部门的请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/directory/v1/departments/{department_id}")]
    Task<FeishuNullDataApiResult> UpdateDepartmentAsync(
           [Token(TokenType.Both)][Header("Authorization")] string access_token,
           [Path] string department_id,
           [Body] DepartmentCreateUpdateRequest departmentUpdateRequest,
           [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
           [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// 从企业组织机构中删除指定的部门。
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，需要通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="department_id">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/directory/v1/departments/{department_id}")]
    Task<FeishuNullDataApiResult> DeleteDepartmentByIdAsync(
       [Token(TokenType.Both)][Header("Authorization")] string tenant_access_token,
       [Path] string department_id,
       [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 支持传入多个部门ID，返回每个部门的详细信息（如名称、负责人、子部门等）。
    /// </summary>
    /// <param name="access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="departmentQueryRequest">部门查询参数请求体。</param>
    /// <param name="employee_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("https://open.feishu.cn/open-apis/directory/v1/departments/mget")]
    Task<FeishuApiResult<DepartmentListResult>> QueryDepartmentsAsync(
       [Token(TokenType.Both)][Header("Authorization")] string access_token,
       [Body] DepartmentQueryRequest departmentQueryRequest,
       [Query("employee_id_type")] string? employee_id_type = Consts.User_Id_Type,
       [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
       CancellationToken cancellationToken = default);

}