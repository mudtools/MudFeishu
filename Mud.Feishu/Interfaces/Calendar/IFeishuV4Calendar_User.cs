// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 基于飞书日历功能开放了对日历、日程、忙闲等资源的操作与查询能力。开发人员能以应用或用户的身份调用日历 API 来实现多种功能。
/// <para>日历资源包括日历本身的资源以及日历包含的日程资源。日历本身可以创建多个，并且每个日历拥有标题、颜色、类型以及公开范围等属性。</para>
/// <para>同时，针对每一个日历，都支持查询其中的日程忙闲信息。开发人员可通过开放平台提供的创建、订阅以及查询等一系列 API，管理日历资源、查询日程忙闲。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar/introduction"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Calendar", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV4Calendar))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV4Calendar : IFeishuV4Calendar, ICurrentUserId
{
    /// <summary>
    /// 订阅日历变更事件
    /// <para>为当前用户身份订阅日历变更事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar/subscription">接口文档</see></para>
    /// </summary> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/subscription")]
    Task<FeishuNullDataApiResult?> SubscribeCalendarEventAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消订阅日历变更事件
    /// <para>为当前用户身份取消订阅日历变更事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar/unsubscription">接口文档</see></para>
    /// </summary> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/unsubscription")]
    Task<FeishuNullDataApiResult?> UnSubscribeCalendarEventAsync(CancellationToken cancellationToken = default);

}