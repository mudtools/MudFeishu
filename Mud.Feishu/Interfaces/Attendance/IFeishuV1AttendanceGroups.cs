// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceGroups;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 考勤组，是对部门或者员工在某个特定场所及特定时间段内的出勤情况
/// <para>（包括上下班、迟到、早退、病假、婚假、丧假、公休、工作时间、加班情况等）的一种规则设定。</para>
/// <para>通过设置考勤组，可以从部门、员工两个维度，来设定考勤方式、考勤时间、考勤地点等考勤规则。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/group/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1AttendanceGroups : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询考勤组下所有成员
    /// </summary>
    /// <param name="group_id">考勤组 ID，示例值：6919358128597097404</param>
    /// <param name="member_clock_type">查询的考勤组成员的打卡类型
    /// <para>可选值有：</para>
    /// <para>&lt;ul&gt;</para>
    /// <para>&lt;li&gt;0：全部打卡类型&lt;/li&gt;</para>
    /// <para>&lt;li&gt;1：需要打卡类型&lt;/li&gt;</para>
    /// <para>&lt;li&gt;2：无需打卡类型&lt;/li&gt;</para>
    /// <para>&lt;/ul&gt;</para>
    /// <para>示例值：1</para>
    /// </param>
    /// <param name="employee_type">响应体中 user_id 的员工 ID 类型。</param>
    /// <param name="dept_type">响应体中 department_ids 的部门 ID 的类型</param>
    /// <param name="page_size">分页大小，示例值：10，默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/groups/{group_id}/list_user")]
    Task<FeishuApiResult<AttendanceGroupsUserPageResult>?> GetGroupUserPageListAsync(
        [Path] string group_id,
        [Query("member_clock_type")] int member_clock_type = 0,
        [Query("employee_type")] string employee_type = Consts.User_Id_Type,
        [Query("dept_type")] string dept_type = Consts.Department_Id_Type,
        [Query("page_size")] int page_size = Consts.PageSize,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
