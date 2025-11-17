using Mud.Feishu.DataModels.Departments;

namespace Mud.Feishu;

/// <summary>
/// 飞书组织机构部门相关的API接口函数。
/// </summary>
[HttpClientApi]
[HttpClientApiWrap(TokenManage = "ITokenManage", WrapInterface = "IFeishuDepartments")]
public interface IFeishuDepartmentsApi
{
    /// <summary>
    /// 在通讯录内创建一个部门。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="departmentCreateRequest">创建部门的请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="client_token">用于幂等判断是否为同一请求，避免重复创建。请参考参数示例值，传入自定义的 client_token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/contact/v3/departments")]
    Task<FeishuApiResult<DepartmentCreateUpdateResult>> CreateDepartmentAsync(
           [Token][Header("Authorization")] string user_access_token,
           [Body] DepartmentCreateRequest departmentCreateRequest,
           [Query("user_id_type")] string? user_id_type = null,
           [Query("department_id_type")] string? department_id_type = null,
           [Query("client_token")] string? client_token = null,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定部门的部分信息，包括名称、父部门、排序以及负责人等。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="departmentCreateRequest">创建部门的请求体。</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}")]
    Task<FeishuApiResult<DepartmentCreateUpdateResult>> UpdatePartDepartmentAsync(
          [Token][Header("Authorization")] string user_access_token,
          [Path] string department_id,
          [Body] DepartmentPartUpdateRequest departmentCreateRequest,
          [Query("user_id_type")] string? user_id_type = null,
          [Query("department_id_type")] string? department_id_type = null,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定部门的信息，包括名称、父部门以及负责人等信息。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="departmentCreateRequest">创建部门的请求体。</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    [Put("https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}")]
    Task<FeishuApiResult<DepartmentUpdateResult>> UpdateDepartmentAsync(
         [Token][Header("Authorization")] string user_access_token,
         [Path] string department_id,
         [Body] DepartmentUpdateRequest departmentCreateRequest,
         [Query("user_id_type")] string? user_id_type = null,
         [Query("department_id_type")] string? department_id_type = null,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门的自定义 ID，即 department_id。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="departMentUpdateIdRequest">更新部门ID的请求体。</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}/update_department_id")]
    Task<FeishuNullDataApiResult> UpdateDepartmentIdAsync(
        [Token][Header("Authorization")] string user_access_token,
        [Path] string department_id,
        [Body] DepartMentUpdateIdRequest departMentUpdateIdRequest,
        [Query("department_id_type")] string? department_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取单个部门信息，包括部门名称、ID、父部门、负责人、状态以及成员个数等。
    /// </summary>
    /// <param name="user_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>对象。</param>
    [Get("https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}")]
    Task<FeishuApiResult<GetDepartmentInfoResult>> GetDepartmentByIdAsync(
     [Token][Header("Authorization")] string user_access_token,
     [Path] string department_id,
     [Query("user_id_type")] string? user_id_type = null,
     [Query("department_id_type")] string? department_id_type = null,
     CancellationToken cancellationToken = default);
}
