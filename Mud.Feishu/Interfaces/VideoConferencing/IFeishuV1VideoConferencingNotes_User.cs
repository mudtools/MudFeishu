// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu;

/// <summary>
/// 飞书会议纪要资源，用户可以查看会议生成的纪要文档、逐字稿等产物，并获取相关上下文（例如会中共享文档等），可以用于复盘、检索增强、对齐校验与可追溯引用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/notes_overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1VideoConferencingNotes : IFeishuAppContextSwitcher, ICurrentUserId
{

    /// <summary>
    /// 获取纪要详情。
    /// <para>获取一篇纪要的详细数据。只能获取自己可见纪要文档，以及相关联的产物、关联引用信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/get">接口文档</see></para>
    /// </summary>   
    /// <param name="note_id">
    /// <para>纪要ID</para>
    /// <para>示例值：6943848821689040898</para>
    /// </param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/notes/{note_id}")]
    Task<FeishuApiResult<GetNoteResult>?> GetNoteAsync(
       [Path] string note_id,
       [Query] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 订阅纪要变更事件。
    /// <para>订阅当前用户身份相关的纪要资源变更事件。通过指定事件类型，来订阅纪要资源不同的事件变更。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/subscription">接口文档</see></para>
    /// </summary>   
    /// <param name="request">订阅纪要变更事件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/vc/v1/notes/subscription")]
    Task<FeishuNullDataApiResult?> SubscriptionNoteAsync(
        [Body] SubscriptionNoteRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消订阅纪要变更事件。
    /// <para>取消订阅当前用户身份相关的纪要资源变更事件。通过指定事件类型，来取消订阅纪要资源对应的事件变更。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/vc-v1/note/unsubscription">接口文档</see></para>
    /// </summary>   
    /// <param name="request">取消订阅纪要变更事件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/vc/v1/notes/unsubscription")]
    Task<FeishuNullDataApiResult?> UnSubscriptionNoteAsync(
      [Body] UnSubscriptionNoteRequest request,
      CancellationToken cancellationToken = default);
}
