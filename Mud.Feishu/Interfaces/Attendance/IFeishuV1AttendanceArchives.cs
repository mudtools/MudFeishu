// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceArchives;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 归档报表用于对应对应后台假勤管理-考勤统计-报表-归档报表功能。
/// <para>归档报表支持引用系统报表，可设置归档时间和数据归档周期，并且支持根据部门/人员、国家/地区、人员类型、工作地点、职级、序列、职务进行人员圈选。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/user_task_remedy/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1AttendanceArchives : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询归档报表表头。
    /// </summary>
    /// <param name="queryArchiveUserStatsFieldsRequest">查询归档报表表头请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/archive_rule/user_stats_fields_query")]
    Task<FeishuApiResult<QueryArchiveUserStatsFieldsResult>?> QueryUserStatsFieldsArchiveRuleAsync(
          [Body] QueryArchiveUserStatsFieldsRequest queryArchiveUserStatsFieldsRequest,
          [Query("employee_type")] string employee_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 写入归档报表结果，对应假勤管理-考勤统计-报表-归档报表页签，点击报表名称进入后的导入功能。可以将数据直接写入归档报表。
    /// </summary>
    /// <param name="archiveUploadReportRequest">写入归档报表结果请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/archive_rule/upload_report")]
    Task<FeishuApiResult<ArchiveUploadReportResult>?> UploadReportArchiveRuleAsync(
          [Body] ArchiveUploadReportRequest archiveUploadReportRequest,
          [Query("employee_type")] string employee_type = Consts.User_Id_Type,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 按月份、用户和归档规则ID直接删除归档报表行数据。
    /// </summary>
    /// <param name="archiveUploadReportRequest">删除归档报表行数据请求体</param>
    /// <param name="employee_type">请求体中的 user_id 和响应体中的 user_id 的员工ID类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/archive_rule/del_report")]
    Task<FeishuNullDataApiResult?> DeleteReportArchiveRuleAsync(
         [Body] DelArchiveReportRequest archiveUploadReportRequest,
         [Query("employee_type")] string employee_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询所有归档规则。
    /// <para>对应后台假勤管理-考勤统计-报表-归档报表功能</para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/archive_rule")]
    Task<FeishuApiResult<FeishuApiPageListResult<ArchiveReportMeta>>?> GetArchiveRulePageListAsync(
           [Query("page_size")] int page_size = Consts.PageSize_10,
           [Query("page_token")] string? page_token = null,
           CancellationToken cancellationToken = default);
}
