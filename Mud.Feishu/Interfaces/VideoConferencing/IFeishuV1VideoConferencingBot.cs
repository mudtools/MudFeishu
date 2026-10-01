// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书会议机器人资源，机器人可以加入会议、在会中发送消息、设置会中倒计时、获取会中事件，以及离会。
/// <para>当前接口不能直接调用，仅为子接口的公共方法抽象</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/user-guide/agent-meeting-user-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1VideoConferencingBot : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 设置会中倒计时，可设置、延长、提前结束或关闭倒计时窗口。
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/countdown">接口文档</see></para>
    /// </summary>
    /// <param name="countdownBotRequest">设置会中倒计时请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/vc/v1/bots/countdown")]
    Task<FeishuNullDataApiResult?> CountdownBotAsync(
       [Body] CountdownBotRequest countdownBotRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 分页获取指定会议内发生的会中事件，例如成员加入、离开、转写接收等。
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/events">接口文档</see></para>
    /// </summary>
    /// <param name="meeting_id">会议 ID。</param>
    /// <param name="start_time">查询的起始时间，秒级时间戳。</param>
    /// <param name="end_time">查询的结束时间，秒级时间戳。</param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/vc/v1/bots/events")]
    Task<FeishuApiPageListResult<BotEvent>?> GetBotEventsPageListAsync(
       [Query("meeting_id")] string meeting_id,
       [Query("start_time")] string? start_time = null,
       [Query("end_time")] string? end_time = null,
       [Query("page_size")] int? page_size = Consts.PageSize_10,
       [Query("page_token")] string? page_token = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过会议号使机器人加入指定的会议。
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/join">接口文档</see></para>
    /// </summary>
    /// <param name="joinBotRequest">机器人入会请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/vc/v1/bots/join")]
    Task<FeishuApiResult<JoinBotResult>?> JoinBotAsync(
       [Body] JoinBotRequest joinBotRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 使机器人离开指定的会议。
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/leave">接口文档</see></para>
    /// </summary>
    /// <param name="leaveBotRequest">机器人离会请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/vc/v1/bots/leave")]
    Task<FeishuApiResult<LeaveBotResult>?> LeaveBotAsync(
       [Body] LeaveBotRequest leaveBotRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 机器人在会中发送文本消息或反馈表情。
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/message">接口文档</see></para>
    /// </summary>
    /// <param name="messageBotRequest">会中发送消息请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/vc/v1/bots/message")]
    Task<FeishuApiResult<MessageBotResult>?> SendBotMessageAsync(
       [Body] MessageBotRequest messageBotRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询指定用户当前正在参加的会议，返回会议号、会议 ID 与会议标题。
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/bot/user_active_meeting">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">目标用户的 ID。以应用身份调用时必填；以用户身份调用时可不填，默认为当前调用用户。</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/vc/v1/bots/user_active_meeting")]
    Task<FeishuApiResult<UserActiveMeetingResult>?> GetUserActiveMeetingAsync(
       [Query("user_id")] string? user_id = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
