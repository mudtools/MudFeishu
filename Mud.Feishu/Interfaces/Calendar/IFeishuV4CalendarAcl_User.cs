// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;


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
[HttpClientApi(RegistryGroupName = "Calendar", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV4CalendarAcl))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV4CalendarAcl : IFeishuV4CalendarAcl, ICurrentUserId
{

    /// <summary>
    /// 订阅日历访问控制变更事件
    /// <para>以用户身份订阅指定日历下的访问控制变更事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/subscription">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/acls/subscription")]
    Task<FeishuNullDataApiResult?> SubscriptionCalendarAclAsync(
      [Path] string calendar_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消订阅日历访问控制变更事件
    /// <para>以用户身份取消订阅指定日历下的访问控制变更事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-acl/unsubscription">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/acls/unsubscription")]
    Task<FeishuNullDataApiResult?> UnSubscriptionCalendarAclAsync(
      [Path] string calendar_id,
      CancellationToken cancellationToken = default);
}