// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceUserFlows;

namespace Mud.Feishu;

/// <summary>
/// 考勤打卡结果（用户令牌）：获取企业内员工的实际打卡结果（用户令牌，飞书考勤支持租户/用户两种令牌调用）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_task/query"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1AttendanceUserTask : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 获取企业内员工的实际打卡结果。
    /// <para>注意：如果企业给一个员工设定的班次是上午 9 点和下午 6 点各打一次上下班卡，即使员工在这期间打了多次卡，该接口也只会返回 1 条记录。如果要获取打卡的详细数据（如打卡位置等信息），可使用查询打卡流水或批量查询打卡流水的接口。</para>
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=query&amp;project=attendance&amp;resource=user_task&amp;version=v1"/></para>
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
