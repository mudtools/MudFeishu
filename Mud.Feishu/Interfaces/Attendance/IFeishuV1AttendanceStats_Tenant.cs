// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceStats;

namespace Mud.Feishu;

/// <summary>
/// 考勤统计接口支持开发者定制接口返回数据，让开发者可以只获取自己所关注的数据内容。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_stats_data/attendance-statistic-reference"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance")]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceStats : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 更新开发者定制的日度统计或月度统计的统计报表表头设置信息。
    /// </summary>
    /// <param name="userStatsViewsRequest">更新统计设置请求体</param>
    /// <param name="user_stats_view_id">用户视图 ID,示例值："TmpZNU5qTTJORFF6T1RnNU5UTTNOakV6TWl0dGIyNTBhQT09"</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("/open-apis/attendance/v1/user_stats_views/{user_stats_view_id}")]
    Task<FeishuApiResult<UserStatsViewsResult>?> UpdateUserStatsViewAsync(
        [Body] UserStatsViewsRequest userStatsViewsRequest,
        [Path] string user_stats_view_id,
        [Query("employee_type")] string employee_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询考勤统计支持的日度统计或月度统计的统计表头。
    /// </summary>
    /// <param name="queryStatsFieldsRequest">查询统计表头请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_stats_fields/query")]
    Task<FeishuApiResult<QueryStatsFieldsResult>?> QueryUserStatsFieldAsync(
       [Body] QueryStatsFieldsRequest queryStatsFieldsRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询考勤统计支持的日度统计或月度统计的统计表头。报表的表头信息可以在考勤统计-报表中查询到具体的报表信息，此接口专门用于查询表头数据。
    /// </summary>
    /// <param name="queryStatsFieldsRequest">查询统计设置请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_stats_views/query")]
    Task<FeishuApiResult<UserStatsViewsResult>?> QueryUserStatsViewAsync(
      [Body] QueryStatsViewsRequest queryStatsFieldsRequest,
      [Query("employee_type")] string employee_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询日度统计或月度统计的统计数据。字段包含基本信息、考勤组信息、出勤统计、异常统计、请假统计、加班统计、打卡时间、考勤结果和自定义字段。
    /// </summary>
    /// <param name="queryStatsDatasRequest">查询统计数据请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/user_stats_datas/query")]
    Task<FeishuApiResult<QueryStatsDatasResult>?> QueryUserStatsDataAsync(
     [Body] QueryStatsDatasRequest queryStatsDatasRequest,
     [Query("employee_type")] string employee_type = Consts.User_Id_Type,
     CancellationToken cancellationToken = default);
}
