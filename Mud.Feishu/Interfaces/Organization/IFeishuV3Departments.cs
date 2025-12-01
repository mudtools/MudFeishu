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
/// <para>当前接口不能直接调用，仅为子接口的公共方法抽象</para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITokenManager), IsAbstract = true)]
[Header("Authorization")]
public interface IFeishuV3Departments
{
    /// <summary>
    /// 获取单个部门信息，包括部门名称、ID、父部门、负责人、状态以及成员个数等。
    /// </summary>
    /// <param name="department_id">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}")]
    Task<FeishuApiResult<GetDepartmentInfoResult>?> GetDepartmentInfoByIdAsync(
         [Path] string department_id,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取单个部门信息，包括部门名称、ID、父部门、负责人、状态以及成员个数等。
    /// </summary>
    /// <param name="department_ids">部门 ID，ID 类型需要与查询参数 department_id_type 的取值保持一致。</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("https://open.feishu.cn/open-apis/contact/v3/departments/batch")]
    Task<FeishuApiResult<BatchGetDepartmentRequest>?> GetDepartmentsByIdsAsync(
        [Query("department_ids")] string[] department_ids,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定部门下的子部门列表，列表内包含部门的名称、ID、父部门、负责人以及状态等信息。
    /// </summary>
    /// <param name="department_id">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="fetch_child"></param> 
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/departments/{department_id}/children")]
    Task<FeishuApiPageListResult<GetDepartmentInfo>?> GetDepartmentsByParentIdAsync(
          [Path] string department_id,
          [Query("fetch_child")] bool fetch_child = false,
          [Query("page_size")] int? page_size = 10,
          [Query("page_token")] string? page_token = null,
          [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
          [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 递归获取指定部门的父部门信息，包括部门名称、ID、负责人以及状态等。
    /// </summary>
    /// <param name="department_id">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/departments/parent")]
    Task<FeishuApiPageListResult<GetDepartmentInfo>?> GetParentDepartmentsByIdAsync(
         [Query("department_id")] string department_id,
         [Query("page_size")] int? page_size = 10,
         [Query("page_token")] string? page_token = null,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
         CancellationToken cancellationToken = default);
}
