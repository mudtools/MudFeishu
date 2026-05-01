// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;

namespace Mud.Feishu;

/// <summary>
/// 日程是存在于日历内的实例资源，开发人员可以通过关联特定日期或时间段、参与人、地点等规则，构建指定主题内容的工作安排。
/// <para>例如，个人工作提醒、团队会议沟通、活动直播等类型的日程。开发人员可以通过日程资源 API 构建与管理日程。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/introduction"/></para>
/// </summary>
[HttpClientApi(RegistryGroupName = "Calendar", TokenManage = nameof(IFeishuAppManager), InheritedFrom = nameof(FeishuV4CalendarEvent))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV4CalendarEvent : IFeishuV4CalendarEvent, ICurrentUserId
{
    /// <summary>
    /// 订阅日程变更事件
    /// <para>以用户身份订阅指定日历下的日程变更事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/subscription">接口文档</see></para>
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
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events/subscription")]
    Task<FeishuNullDataApiResult?> SubscriptionCalendarEventAsync(
      [Path] string calendar_id,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消订阅日程变更事件
    /// <para>以用户身份取消订阅指定日历下的日程变更事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar-event/unsubscription">接口文档</see></para>
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
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/{calendar_id}/events/unsubscription")]
    Task<FeishuNullDataApiResult?> UnSubscriptionCalendarEventAsync(
      [Path] string calendar_id,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);



    /// <summary>
    /// 生成 CalDAV 配置
    /// <para>为当前用户生成一个 CalDAV 账号密码，用于将飞书日历信息同步到本地设备日历。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/setting/generate_caldav_conf">接口文档</see></para>
    /// </summary> 
    /// <param name="generateCaldavConfSettingRequest">生成 CalDAV 配置请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/settings/generate_caldav_conf")]
    Task<FeishuApiResult<GenerateCaldavConfSettingResult>?> GenerateCaldavConfSettingAsync(
       [Body] GenerateCaldavConfSettingRequest generateCaldavConfSettingRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 将 Exchange 账户绑定到飞书账户
    /// <para>将 Exchange 账户绑定到飞书账户，进而支持 Exchange 日历的导入。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/exchange_binding/create">接口文档</see></para>
    /// </summary> 
    /// <param name="createExchangeBindingRequest">将 Exchange 账户绑定到飞书账户请求体</param>
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
    [Post("/open-apis/calendar/v4/exchange_bindings")]
    Task<FeishuApiResult<ExchangeBindingOopsResult>?> CreateExchangeBindingAsync(
      [Body] CreateExchangeBindingRequest createExchangeBindingRequest,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 将 Exchange 账户绑定到飞书账户
    /// <para>将 Exchange 账户绑定到飞书账户，进而支持 Exchange 日历的导入。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/exchange_binding/create">接口文档</see></para>
    /// </summary> 
    /// <param name="exchange_binding_id">
    /// <para>Exchange 绑定的唯一标识 ID。调用 [将 Exchange 账户绑定到飞书账户](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/exchange_binding/create) 绑定时，可从返回结果中获取 exchange_binding_id。</para>
    /// <para>示例值：ZW1haWxfYWRtaW5fZXhhbXBsZUBvdXRsb29rLmNvbSBlbWFpbF9hY2NvdW50X2V4YW1wbGVAb3V0bG9vay5jb20=</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/exchange_bindings/{exchange_binding_id}")]
    Task<FeishuNullDataApiResult?> DeleteExchangeBindingAsync(
      [Path] string exchange_binding_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询 Exchange 账户的绑定状态
    /// <para>获取 Exchange 账户的绑定状态，包括 Exchange 日历的同步状态。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/exchange_binding/create">接口文档</see></para>
    /// </summary> 
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
    /// <param name="exchange_binding_id">
    /// <para>Exchange 绑定的唯一标识 ID。调用 [将 Exchange 账户绑定到飞书账户](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/exchange_binding/create) 绑定时，可从返回结果中获取 exchange_binding_id。</para>
    /// <para>示例值：ZW1haWxfYWRtaW5fZXhhbXBsZUBvdXRsb29rLmNvbSBlbWFpbF9hY2NvdW50X2V4YW1wbGVAb3V0bG9vay5jb20=</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/calendar/v4/exchange_bindings/{exchange_binding_id}")]
    Task<FeishuApiResult<ExchangeBindingOopsResult>?> GetExchangeBindingAsync(
       [Path] string exchange_binding_id,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}