// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceLeave;

namespace Mud.Feishu;

/// <summary>
/// 考勤休假管理，包含休假过期时间获取发放记录、休假发放记录接口。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/leave_employ_expire_record/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1AttendanceLeave_Tenant : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 通过过期时间获取发放记录，只能获取到对应时间段过期的发放记录。
    /// </summary>
    /// <param name="leaveEmployExpireRecordsRequest">通过过期时间获取发放记录请求体</param>
    /// <param name="leave_id">假期类型ID，示例值："7111688079785723436"。</param>
    /// <param name="user_id_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/leave_employ_expire_records/{leave_id}")]
    Task<FeishuApiResult<LeaveEmployExpireRecordsResult>?> GetLeaveEmployExpireRecordAsync(
        [Body] LeaveEmployExpireRecordsRequest leaveEmployExpireRecordsRequest,
        [Path] string leave_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新发放记录的发放数量和失效日期，对应假勤管理-休假管理-发放记录。
    /// </summary>
    /// <param name="leaveAccrualRecordRequest">修改发放记录请求体</param>
    /// <param name="leave_id">假期类型ID，示例值："7111688079785723436"。</param>
    /// <param name="user_id_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("/open-apis/attendance/v1/leave_accrual_record/{leave_id}")]
    Task<FeishuApiResult<LeaveAccrualRecordResult>?> ModifyLeaveAccrualRecordAsync(
      [Body] LeaveAccrualRecordRequest leaveAccrualRecordRequest,
      [Path] string leave_id,
      [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
