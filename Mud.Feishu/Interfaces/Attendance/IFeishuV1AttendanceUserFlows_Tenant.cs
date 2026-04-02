// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceUserFlows;

namespace Mud.Feishu;

/// <summary>
/// 打卡信息管理，可以导入、查询、删除员工的打卡流水记录
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_task/batch_create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceUserFlows : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 导入员工的打卡流水记录。导入后，会根据员工所在的考勤组班次规则，计算最终的打卡状态与结果。
    /// <para>可在打卡管理-打卡记录中查询</para>
    /// </summary>
    /// <param name="userFlowsBatchCreateRequest">导入打卡流水请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_flows/batch_create")]
    Task<FeishuApiResult<UserFlowsBatchCreateResult>?> BatchCreateUserFlowAsync(
         [Body] UserFlowsBatchCreateRequest userFlowsBatchCreateRequest,
         [Query("employee_type")] string employee_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过打卡记录 ID 获取用户的打卡流水记录。
    /// </summary>
    /// <param name="user_flow_id">打卡流水记录 ID，示例值："6708236686834352397"</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/user_flows/{user_flow_id}")]
    Task<FeishuApiResult<UserFlowInfo>?> GetUserFlowAsync(
        [Path] string user_flow_id,
        [Query("employee_type")] string employee_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过打卡记录 ID 批量查询打卡流水记录。
    /// </summary>
    /// <param name="userFlowsQueryRequest">批量查询打卡流水请求体</param>
    /// <param name="include_terminated_user">由于新入职用户可以复用已离职用户的employee_no/employee_id。
    /// <para> 如果true，返回employee_no/employee_id对应的所有在职+离职用户数据；</para>
    /// <para> 如果false，只返回employee_no/employee_id对应的在职或最近一个离职用户数据</para></param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_flows/query")]
    Task<FeishuApiResult<UserFlowsQueryResult>?> QueryUserFlowAsync(
       [Body] UserFlowsQueryRequest userFlowsQueryRequest,
       [Query("include_terminated_user")] bool? include_terminated_user = null,
       [Query("employee_type")] string employee_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除员工从开放平台导入的打卡记录。删除后会重新计算打卡记录对应考勤任务结果。
    /// </summary>
    /// <param name="userFlowsBatchDelRequest">删除打卡流水请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_flows/batch_del")]
    Task<FeishuApiResult<UserFlowsBatchDelResult>?> BatchDelUserFlowAsync(
       [Body] UserFlowsBatchDelRequest userFlowsBatchDelRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取企业内员工的实际打卡结果。
    /// </summary>
    /// <param name="userTasksQueryRequest">查询打卡结果请求体</param>
    /// <param name="ignore_invalid_users">是否忽略无效和没有权限的用户，对应employee_type。
    /// <para>如果 true，则返回有效用户的信息，并告知无效和没有权限的用户信息；</para>
    /// <para>如果 false，且 user_ids 中存在无效或没有权限的用户，则返回错误</para></param>
    /// <param name="include_terminated_user">由于新入职用户可以复用已离职用户的employee_no/employee_id。
    /// <para> 如果true，返回employee_no/employee_id对应的所有在职+离职用户数据；</para>
    /// <para> 如果false，只返回employee_no/employee_id对应的在职或最近一个离职用户数据</para></param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_tasks/query")]
    Task<FeishuApiResult<UserTasksQueryResult>?> QueryUserTaskAsync(
       [Body] UserTasksQueryRequest userTasksQueryRequest,
       [Query("ignore_invalid_users")] bool? ignore_invalid_users = null,
       [Query("include_terminated_user")] bool? include_terminated_user = null,
       [Query("employee_type")] string employee_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
