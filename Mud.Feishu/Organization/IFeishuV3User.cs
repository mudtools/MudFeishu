// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Users;

namespace Mud.Feishu;

/// <summary>
/// 飞书用户是飞书通讯录中的基础资源，对应企业组织架构中的成员实体。
/// <para>当前接口不能直接调用，仅为子接口的公共方法抽象</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/contact-v3/user/field-overview"/></para>
/// </summary>
public interface IFeishuV3User
{

    /// <summary>
    /// 更新通讯录中指定用户的信息，包括名称、邮箱、手机号、所属部门以及自定义字段等信息。
    /// </summary>
    /// <param name="user_id">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="userModel">用于更新的用户请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}")]
    Task<FeishuApiResult<CreateOrUpdateUserResult>?> UpdateUserAsync(
        [Path] string user_id,
        [Body] UpdateUserRequest userModel,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取通讯录中某一用户的信息，包括用户 ID、名称、邮箱、手机号、状态以及所属部门等信息。
    /// </summary>
    /// <param name="user_id">用户ID。ID 类型与查询参数 user_id_type 保持一致。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/users/{user_id}")]
    Task<FeishuApiResult<GetUserInfoResult>> GetUserInfoByIdAsync(
        [Path] string user_id,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取通讯录中用户的信息，包括用户 ID、名称、邮箱、手机号、状态以及所属部门等信息。
    /// </summary>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="user_ids">用户ID。ID 类型与查询参数 user_id_type 保持一致。如需一次查询多个用户ID，可多次传递同一参数名，并且每次传递不同的参数值。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/users/batch")]
    Task<FeishuApiResult<GetUserInfosResult>?> GetUserByIdsAsync(
       [Query("user_ids")] string[] user_ids,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定部门直属的用户信息列表。用户信息包括用户 ID、名称、邮箱、手机号以及状态等信息。
    /// </summary>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中使用的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <param name="department_id">部门 ID，ID 类型与 department_id_type 的取值保持一致。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/contact/v3/users/find_by_department")]
    Task<FeishuApiResult<GetUserInfosResult>?> GetUserByDepartmentIdAsync(
     [Query("department_id")] string department_id,
     [Query("page_size")] int page_size = 10,
     [Query("page_token")] string? page_token = null,
     [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
     [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
     CancellationToken cancellationToken = default);
}
