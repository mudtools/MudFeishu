// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 访问控制（ACL）用于管理日历的成员权限。一个日历内可以创建多个 ACL，每一个 ACL 内可以为一个成员设置日历的访问权限，其中访问权限包括：
/// <para>
/// <list type="bullet">
/// <item>游客，只能看到日历日程忙闲信息</item>
/// <item>订阅者：可查看日历内的所有日程详情。</item>
/// <item>编辑者：可在日历内创建或修改日程。</item>
/// <item>管理员：可管理日历及共享设置。</item>
/// </list>
/// </para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV4CalendarAcl : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建访问控制
    /// <para>以当前身份（应用或用户）为指定日历添加访问控制，即日历成员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/create">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="createCalendarAclRequest">创建访问控制请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/acls")]
    Task<FeishuApiResult<CreateCalendarAclResult>?> CreateCalendarAclAsync(
        [Path] string calendar_id,
        [Body] CreateCalendarAclRequest createCalendarAclRequest,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除访问控制
    /// <para>以当前身份（应用或用户）删除指定日历内的某一访问控制，即成员权限。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/delete">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="acl_id">
    /// <para>访问控制 ID。</para>
    /// <para>为日历创建访问控制时会返回访问控制 ID。</para>
    /// <para>示例值：user_xxxxxx</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/calendars/{calendar_id}/acls/{acl_id}")]
    Task<FeishuNullDataApiResult?> DeleteCalendarAclAsync(
        [Path] string calendar_id,
        [Path] string acl_id,
        CancellationToken cancellationToken = default);



    /// <summary>
    /// 分页获取访问控制列表
    /// <para>以当前身份（应用或用户）获取指定日历的访问控制列表。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/list">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：500</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/calendar/v4/calendars/{calendar_id}/acls")]
    Task<FeishuApiResult<GetCalendarEventPageListResult>?> GetCalendarAclsPageListAsync(
        [Path] string calendar_id,
        [Query] int page_size = Consts.PageSize_20,
        [Query] string? page_token = null,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}