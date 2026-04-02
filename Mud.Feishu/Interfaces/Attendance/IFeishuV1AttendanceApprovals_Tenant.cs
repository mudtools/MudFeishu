// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceApprovals;

namespace Mud.Feishu;

/// <summary>
/// 用于管理三方系统假勤审批的请假、加班、外出和出差四种审批数据。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_approval/query"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceApprovals : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取员工在某段时间内的请假、加班、外出和出差四种审批数据。
    /// </summary>
    /// <param name="queryAttendanceApprovalsRequest">获取审批数据请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_approvals/query")]
    Task<FeishuApiResult<QueryAttendanceApprovalsResult>?> QueryUserApprovalAsync(
      [Body] QueryAttendanceApprovalsRequest queryAttendanceApprovalsRequest,
      [Query("employee_type")] string employee_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 对于只使用飞书考勤系统，而未使用飞书审批系统的企业，可以通过本接口将三方审批结果数据回写到飞书考勤系统中。
    /// </summary>
    /// <param name="writeApprovalsDataRequest">写入审批结果请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_approvals")]
    Task<FeishuApiResult<CreateUserApprovalResult>?> CreateUserApprovalAsync(
         [Body] CreateUserApprovalRequest writeApprovalsDataRequest,
         [Query("employee_type")] string employee_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过该接口更新写入飞书考勤系统中的三方系统审批状态，例如请假、加班、外出、出差、补卡等审批，状态包括通过、不通过、撤销等。
    /// </summary>
    /// <param name="updateApprovalInfosRequest">通知审批状态更新请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/approval_infos/process")]
    Task<FeishuApiResult<UpdateAttendanceApprovalInfoResult>?> ProcessApprovalInfoAsync(
        [Body] UpdateApprovalInfosRequest updateApprovalInfosRequest,
        CancellationToken cancellationToken = default);
}
