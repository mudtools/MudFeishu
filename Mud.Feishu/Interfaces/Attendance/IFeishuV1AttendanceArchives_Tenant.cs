// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceArchives;

namespace Mud.Feishu;


/// <summary>
/// 归档报表用于对应对应后台假勤管理-考勤统计-报表-归档报表功能（租户令牌）。
/// <para>归档报表支持引用系统报表，可设置归档时间和数据归档周期，并且支持根据部门/人员、国家/地区、人员类型、工作地点、职级、序列、职务进行人员圈选。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/archive_rule/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance", InheritedFrom = nameof(FeishuV1AttendanceArchives))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceArchives : IFeishuV1AttendanceArchives
{
    /// <summary>
    /// 写入归档报表结果，对应假勤管理-考勤统计-报表-归档报表页签，点击报表名称进入后的导入功能。可以将数据直接写入归档报表。
    /// <para>官方文档：<see href="https://open.feishu.cn/api-explorer?from=op_doc_tab&amp;apiName=upload_report&amp;project=attendance&amp;resource=archive_rule&amp;version=v1"/></para>
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
}
