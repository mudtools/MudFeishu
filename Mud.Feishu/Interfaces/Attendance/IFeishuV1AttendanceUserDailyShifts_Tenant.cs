// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceUserDailyShifts;

namespace Mud.Feishu;

/// <summary>
/// 排班表是用来描述考勤组内人员每天按哪个班次进行上班。
/// <para>目前排班表支持按x月y日对一位或多位人员进行排班。当用户的排班数据不存在时会进行创建，当用户的排班数据存在时会按照入参信息进行修改。</para>
/// <para>注意：每人每天只能在一个考勤组中。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_daily_shift/batch_create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceUserDailyShifts : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建或修改排班表
    /// </summary>
    /// <param name="userDailyShiftRequest">创建或修改排班表请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_daily_shifts/batch_create")]
    Task<FeishuApiResult<UserDailyShiftResult>?> BatchCreateUserDailyShiftAsync(
         [Body] UserDailyShiftsRequest userDailyShiftRequest,
         [Query("employee_type")] string employee_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>支持查询多个用户的排班情况，注意此接口返回的是用户维度的排班结果，与页面功能并不对应。</para>
    /// <para>可以通过返回结果中的group_id查询考勤组按 ID 查询考勤组 ，shift_id查询班次按 ID 查询班次 。</para>
    /// <para>查询的时间跨度不能超过 30 天。</para>
    /// </summary>
    /// <param name="userDailyShiftRequest">查询排班表请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_daily_shifts/query")]
    Task<FeishuApiResult<UserDailyShiftsQueryResult>?> QueryUserDailyShiftAsync(
      [Body] QueryUserDailyShiftsRequest userDailyShiftRequest,
      [Query("employee_type")] string employee_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>创建或修改临时排班</para>
    /// <para>可在排班表上创建或修改临时班次，并用于排班。目前支持按日期对一位或多位人员进行排临时班次。</para>
    /// <para>临时排班为付费功能，如需使用请联系飞书的客户经理。</para>
    /// </summary>
    /// <param name="userTmpDailyShiftRequest">创建或修改临时排班请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_daily_shifts/batch_create_temp")]
    Task<FeishuApiResult<UserTmpDailyShiftResult>?> BatchCreateTempUserDailyShiftAsync(
       [Body] UserTmpDailyShiftRequest userTmpDailyShiftRequest,
       [Query("employee_type")] string employee_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
