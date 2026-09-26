// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceUserDailyShifts;

namespace Mud.Feishu;

/// <summary>
/// 考勤排班（用户令牌）：创建或修改临时排班（用户令牌，飞书考勤支持租户/用户两种令牌调用）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_daily_shift/batch_create_temp"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1AttendanceUserDailyShifts : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 创建或修改临时排班。
    /// <para>可在排班表上创建或修改临时班次，并用于排班。目前支持按日期对一位或多位人员进行排临时班次。</para>
    /// <para>临时排班为付费功能，如需使用请联系飞书的客户经理。</para>
    /// <para>注意：如果返回 code=0，且 msg 不为空，表示临时排班部分成功。如 msg 返回 {人员：[日期，日期]} 格式，代表人员在排班日期下临时排班未成功。这种一般是考勤组 id 与人员不匹配造成的。</para>
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=batch_create_temp&amp;project=attendance&amp;resource=user_daily_shift&amp;version=v1"/></para>
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
