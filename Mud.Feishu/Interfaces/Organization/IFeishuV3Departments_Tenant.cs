// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Departments;

namespace Mud.Feishu;

/// <summary>
/// 飞书组织机构部门是指企业组织架构树上的某一个节点。
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// <para>在部门内部，可添加用户作为部门成员，也可添加新的部门作为子部门。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/department/field-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Organization", InheritedFrom = nameof(FeishuV3Departments))]
[Header("Authorization")]
public interface IFeishuTenantV3Departments : IFeishuV3Departments
{
    /// <summary>
    /// 在通讯录内创建一个部门。
    /// </summary>
    /// <param name="departmentCreateRequest">创建部门的请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="client_token">用于幂等判断是否为同一请求，避免重复创建。请参考参数示例值，传入自定义的 client_token。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/contact/v3/departments")]
    Task<FeishuApiResult<DepartmentCreateUpdateResult>?> CreateDepartmentAsync(
           [Body] DepartmentCreateRequest departmentCreateRequest,
           [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
           [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
           [Query("client_token")] string? client_token = null,
           CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定部门的部分信息，包括名称、父部门、排序以及负责人等。
    /// </summary>
    /// <param name="departmentCreateRequest">创建部门的请求体。</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/contact/v3/departments/{department_id}")]
    Task<FeishuApiResult<DepartmentCreateUpdateResult>?> UpdatePartDepartmentAsync(
          [Path] string department_id,
          [Body] DepartmentPartUpdateRequest departmentCreateRequest,
          [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
          [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定部门的信息，包括名称、父部门以及负责人等信息。
    /// </summary>
    /// <param name="departmentCreateRequest">创建部门的请求体。</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/contact/v3/departments/{department_id}")]
    Task<FeishuApiResult<DepartmentUpdateResult>?> UpdateDepartmentAsync(
         [Path] string department_id,
         [Body] DepartmentUpdateRequest departmentCreateRequest,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新部门的自定义 ID，即 department_id。
    /// </summary>
    /// <param name="departMentUpdateIdRequest">更新部门ID的请求体。</param>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/contact/v3/departments/{department_id}/update_department_id")]
    Task<FeishuNullDataApiResult?> UpdateDepartmentIdAsync(
        [Path] string department_id,
        [Body] DepartMentUpdateIdRequest departMentUpdateIdRequest,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 将指定部门的部门群转为普通群。
    /// </summary> 
    /// <param name="departmentRequest">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/contact/v3/departments/unbind_department_chat")]
    Task<FeishuNullDataApiResult?> UnbindDepartmentChatAsync(
        [Body] DepartmentRequest departmentRequest,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 从通讯录中删除指定的部门。
    /// </summary>
    /// <param name="department_id">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/contact/v3/departments/{department_id}")]
    Task<FeishuNullDataApiResult?> DeleteDepartmentByIdAsync(
       [Path] string department_id,
       [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
       CancellationToken cancellationToken = default);
}
