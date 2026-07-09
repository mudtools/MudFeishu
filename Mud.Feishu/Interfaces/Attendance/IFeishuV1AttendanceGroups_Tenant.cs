// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.AttendanceGroups;

namespace Mud.Feishu;

/// <summary>
/// 考勤组，是对部门或者员工在某个特定场所及特定时间段内的出勤情况
/// <para>（包括上下班、迟到、早退、病假、婚假、丧假、公休、工作时间、加班情况等）的一种规则设定。</para>
/// <para>通过设置考勤组，可以从部门、员工两个维度，来设定考勤方式、考勤时间、考勤地点等考勤规则。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/attendance-v1/group/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Attendance", InheritedFrom = nameof(FeishuV1AttendanceGroups))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1AttendanceGroups : IFeishuV1AttendanceGroups
{

    /// <summary>
    /// 创建或修改考勤组
    /// </summary> 
    /// <param name="createAttendanceShiftsRequest">创建或修改考勤组请求体</param>
    /// <param name="employee_type">响应体或请求体中 user_id 的员工 ID 类型。</param>
    /// <param name="dept_type">响应体或请求体 department_ids 的部门 ID 的类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/groups")]
    Task<FeishuApiResult<CreateAttendanceGroupResult>?> CreateGroupAsync(
          [Body] CreateAttendanceGroupRequest createAttendanceShiftsRequest,
          [Query("employee_type")] string employee_type = Consts.User_Id_Type,
          [Query("dept_type")] string dept_type = Consts.Department_Id_Type,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// <para>通过考勤组 ID 删除考勤组。对应设置-假勤设置-考勤组操作列的删除功能。</para>
    /// </summary>
    /// <param name="group_id">考勤组 ID，示例值："6919358128597097404"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/attendance/v1/groups/{group_id}")]
    Task<FeishuNullDataApiResult?> DeleteGroupByIdAsync(
         [Path] string group_id,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过考勤组 ID 获取考勤组详情。包含基本信息、考勤班次、考勤方式、考勤设置信息。
    /// </summary>
    /// <param name="group_id">考勤组 ID，示例值：6919358128597097404</param>
    /// <param name="employee_type">响应体中 user_id 的员工 ID 类型。</param>
    /// <param name="dept_type">响应体中 department_ids 的部门 ID 的类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/groups/{group_id}")]
    Task<FeishuApiResult<AttendanceGroupsInfo>?> GetGroupByIdAsync(
       [Path] string group_id,
       [Query("employee_type")] string employee_type = Consts.User_Id_Type,
       [Query("dept_type")] string dept_type = Consts.Department_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 按考勤组名称查询考勤组摘要信息。查询条件支持名称精确匹配和模糊匹配两种方式。
    /// <para>查询结果按考勤组修改时间 desc 排序，且最大记录数为 10 条。</para>
    /// </summary>
    /// <param name="groupsSearchRequest">按名称查询考勤组请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/attendance/v1/groups/search")]
    Task<FeishuApiResult<GroupsSearchResult>?> GetGroupByNameAsync(
      [Body] GroupsSearchRequest groupsSearchRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取所有考勤组列表。列表中的数据为考勤组信息，字段包含考勤组名称和考勤组id。
    /// </summary>
    /// <param name="page_size">分页大小，示例值：10，默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/attendance/v1/groups")]
    Task<FeishuApiResult<AttendanceGroupsPageResult>?> GetGroupPageListAsync(
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
