// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceRemedys;

namespace Mud.Feishu;

/// <summary>
/// 对于只使用飞书考勤系统而未使用飞书审批系统的企业,可以通过该接口，将在三方审批系统中补卡审批数据，同步到飞书考勤系统中。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_task_remedy/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceRemedys : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 将在三方审批系统中发起的补卡审批数据，写入到飞书考勤系统中，状态为审批中。
    /// <para>写入后可以由<seealso cref="IFeishuTenantV1AttendanceApprovals.ProcessApprovalInfoAsync(DataModels.AttendanceApprovals.UpdateApprovalInfosRequest, CancellationToken)">通知审批状态更新</seealso>进行状态更新。</para> 
    /// </summary>
    /// <param name="attendanceRemedysRequest">通知补卡审批发起请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>

    [Post("/open-apis/attendance/v1/user_task_remedys")]
    Task<FeishuApiResult<AttendanceRemedysResult>?> CreateUserTaskRemedyAsync(
           [Body] AttendanceRemedysRequest attendanceRemedysRequest,
           [Query("employee_type")] string employee_type = Consts.User_Id_Type,
           CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取用户某天可以补的第几次上 / 下班卡的时间。
    /// </summary>
    /// <param name="allowedRemedysRequest">获取可补卡时间请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_task_remedys/query_user_allowed_remedys")]
    Task<FeishuApiResult<QueryUserAllowedRemedysResult>?> QueryUserAllowedRemedysUserTaskRemedyAsync(
       [Body] AllowedRemedysRequest allowedRemedysRequest,
       [Query("employee_type")] string employee_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取用户补卡记录，补卡记录是用户通过审批的方式，在某一次上/下班的打卡时间范围内，补充一条打卡记录，用以修正用户的考勤结果。
    /// </summary>
    /// <param name="queryUserRemedysRequest">获取补卡记录请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_task_remedys/query")]
    Task<FeishuApiResult<QueryUserRemedysResult>?> QueryUserTaskRemedyAsync(
       [Body] QueryUserRemedysRequest queryUserRemedysRequest,
       [Query("employee_type")] string employee_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

}
