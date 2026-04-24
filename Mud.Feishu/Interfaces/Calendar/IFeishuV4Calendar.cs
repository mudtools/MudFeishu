// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Calendar;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 基于飞书日历功能开放了对日历、日程、忙闲等资源的操作与查询能力。开发人员能以应用或用户的身份调用日历 API 来实现多种功能。
/// <para>日历资源包括日历本身的资源以及日历包含的日程资源。日历本身可以创建多个，并且每个日历拥有标题、颜色、类型以及公开范围等属性。</para>
/// <para>同时，针对每一个日历，都支持查询其中的日程忙闲信息。开发人员可通过开放平台提供的创建、订阅以及查询等一系列 API，管理日历资源、查询日程忙闲。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar/introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV4Calendar : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 创建共享日历
    /// <para>为当前身份（应用或用户）创建一个共享日历。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar/create">接口文档</see></para>
    /// </summary> 
    /// <param name="createCalendarRequest">创建共享日历请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars")]
    Task<FeishuApiResult<CreateCalendarResult>?> CreateCalendarAsync(
      [Body] CreateCalendarRequest createCalendarRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除共享日历
    /// <para>为当前身份（应用或以当前身份（应用或用户）删除某一指定的共享日历。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar/delete">接口文档</see></para>
    /// </summary> 
    /// <param name="calendar_id">
    /// <para>日历 ID。</para>
    /// <para>创建共享日历时会返回日历 ID。也可以调用以下接口获取某一日历的 ID。</para>
    /// <para>- [查询主日历信息](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/primary)</para>
    /// <para>- [查询日历列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/list)</para>
    /// <para>- [搜索日历](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/calendar-v4/calendar/search)</para>
    /// <para>示例值：feishu.cn_xxxxxxxxxx@group.calendar.feishu.cn</para>
    /// </param> /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/calendar/v4/calendars/{calendar_id}")]
    Task<FeishuNullDataApiResult?> DeleteCalendarAsync(
         [Path] string calendar_id,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询主日历信息
    /// <para>获取当前身份（应用或用户）的主日历信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/calendar-v4/calendar/primary">接口文档</see></para>
    /// </summary> 
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。[了解更多：如何获取 Open ID](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-openid)</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。[了解更多：如何获取 Union ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-union-id)</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。[了解更多：如何获取 User ID？](https://open.feishu.cn/document/uAjLw4CM/ugTN1YjL4UTN24CO1UjN/trouble-shooting/how-to-obtain-user-id)</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/calendar/v4/calendars/primary")]
    Task<FeishuApiResult<GetPrimaryCalendarResult>?> GetPrimaryCalendarAsync(
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
