// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceShifts;

namespace Mud.Feishu;

/// <summary>
/// 考勤班次是描述一次考勤任务时间规则的统称，比如一天打多少次卡，每次卡的上下班时间，晚到多长时间算迟到，晚到多长时间算缺卡等。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/shift/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceShifts : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建考勤班次
    /// </summary>
    /// <param name="createAttendanceShiftsRequest">创建班次请求体</param>
    /// <param name="employee_type">请求体中的 user_ids 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/shifts")]
    Task<FeishuApiResult<CreateAttendanceShiftsResult>?> CreateShiftAsync(
          [Body] CreateAttendanceShiftsRequest createAttendanceShiftsRequest,
          [Query("employee_type")] string employee_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>通过班次 ID 删除班次。对应功能为假勤设置-班次设置班次列表中操作栏的删除按钮。</para>
    /// </summary>
    /// <param name="shift_id">班次 ID，获取方式：1）按名称查询班次 2）创建班次 示例值："6919358778597097404"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/attendance/v1/shifts/{shift_id}")]
    Task<FeishuNullDataApiResult?> DeleteShiftByIdAsync(
       [Path] string shift_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>通过班次 ID 获取班次详情。对应功能为假勤设置-班次设置班次列表中的具体班次，班次信息可以点击班次名称查看</para>
    /// </summary>
    /// <param name="shift_id">班次 ID，获取方式：1）按名称查询班次 2）创建班次 示例值："6919358778597097404"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/shifts/{shift_id}")]
    Task<FeishuApiResult<GetAttendanceShiftsResult>?> GetShiftByIdAsync(
       [Path] string shift_id,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// <para>按名称查询班次。对应功能为飞书人事管理后台中假勤设置-班次配置中的搜索班次名称功能，展示班次名称、打卡规则、弹性班次规则、休息规则等</para>
    /// </summary>
    /// <param name="shift_name">班次名称 示例值："早班"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/shifts/query")]
    Task<FeishuApiResult<GetAttendanceShiftsResult>?> GetShiftByNameAsync(
       [Query("shift_name")] string shift_name,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 分页查询所有班次。
    /// <para>对应功能为飞书人事管理后台中假勤设置-班次配置中的翻页查询所有班次功能，展示班次名称、打卡规则、弹性班次规则、休息规则等</para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/shifts")]
    Task<FeishuApiResult<GetAttendanceShiftsPageListResult>?> GetShiftsPageListAsync(
       [Query("page_size")] int page_size = Consts.PageSize,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);
}
