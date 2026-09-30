// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu;

/// <summary>
/// 飞书服务台推送API是开放平台基于飞书服务台的推送功能开放的创建/查询/更新/预览/审批/发送等API，开发者可以基于这些API管理服务台推送任务。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "HelpDesk")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1HelpDeskNotification : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 用于在服务台请求Header中添加“服务台token”参数
    /// <para>服务台的详细接入指南：<see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/access-guide">服务台接入指南</see></para>
    /// <para>Key: X-Lark-Helpdesk-Authorization</para>
    /// <para>Value: base64(helpdesk_id:helpdesk_token)，通过base64加密将helpdesk_id和helpdesk_token用':'连接而成的字符串。</para>
    /// </summary>
    [Header("X-Lark-Helpdesk-Authorization")]
    string HelpdeskTokenAndId { get; set; }


    /// <summary>
    /// 创建推送
    /// <para>创建一个推送，创建后处于草稿状态。限频：10 次/分钟。</para>
    /// <para>字段权限（响应中含敏感字段时才返回）：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建推送请求体</param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/notifications")]
    Task<FeishuApiResult<CreateNotificationResult>?> CreateNotificationAsync(
        [Body] CreateNotificationRequest request,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询推送详情
    /// <para>查询推送详情。限频：100 次/分钟。</para>
    /// <para>字段权限（响应中含敏感字段时才返回）：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/get">接口文档</see></para>
    /// </summary>
    /// <param name="notification_id">
    /// <para>唯一 ID</para>
    /// <para>示例值："1624326025000"</para>
    /// </param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/notifications/{notification_id}")]
    Task<FeishuApiResult<GetNotificationResult>?> GetNotificationAsync(
        [Path] string notification_id,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新推送
    /// <para>更新推送消息。仅可在消息处于草稿状态时调用本接口。限频：20 次/分钟。</para>
    /// <para>字段权限（响应中含敏感字段时才返回）：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/patch">接口文档</see></para>
    /// </summary>
    /// <param name="notification_id">
    /// <para>推送任务唯一 ID</para>
    /// <para>示例值："6985032626234982420"</para>
    /// </param>
    /// <param name="request">更新推送请求体</param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/helpdesk/v1/notifications/{notification_id}")]
    Task<FeishuNullDataApiResult?> UpdateNotificationAsync(
        [Path] string notification_id,
        [Body] UpdateNotificationRequest request,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 预览推送
    /// <para>推送前预览已设置的推送内容。限频：20 次/分钟。</para>
    /// <para>本接口无请求体、无查询参数。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/preview">接口文档</see></para>
    /// </summary>
    /// <param name="notification_id">
    /// <para>推送创建 API 成功后返回的唯一 ID</para>
    /// <para>示例值："6985032626234982420"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/notifications/{notification_id}/preview")]
    Task<FeishuNullDataApiResult?> PreviewNotificationAsync(
        [Path] string notification_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 提交审批
    /// <para>通常在调用"创建推送" API 后调用本接口。如果创建者是服务台所有者，推送消息将自动审批通过；否则会通知服务台所有者审批该推送消息。限频：10 次/分钟。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/submit_approve">接口文档</see></para>
    /// </summary>
    /// <param name="notification_id">
    /// <para>"创建推送" API 返回的唯一 ID</para>
    /// <para>示例值："6985032626234982420"</para>
    /// </param>
    /// <param name="request">提交审批请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/notifications/{notification_id}/submit_approve")]
    Task<FeishuApiResult<SubmitApproveNotificationResult>?> SubmitApproveNotificationAsync(
        [Path] string notification_id,
        [Body] SubmitApproveNotificationRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 发送推送
    /// <para>审批通过后，调用本接口设置推送时间，等待调度系统发送消息。限频：10 次/分钟。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/execute_send">接口文档</see></para>
    /// </summary>
    /// <param name="notification_id">
    /// <para>"创建推送" API 返回的唯一 ID</para>
    /// <para>示例值："6985032626234982420"</para>
    /// </param>
    /// <param name="request">发送推送请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/notifications/{notification_id}/execute_send")]
    Task<FeishuNullDataApiResult?> ExecuteSendNotificationAsync(
        [Path] string notification_id,
        [Body] ExecuteSendNotificationRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消推送
    /// <para>取消推送 API。可在审批通过后等待定时发送期间、消息发送中（已发送消息将被撤回）、以及发送完成后（所有已发送消息将被撤回）调用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/cancel_send">接口文档</see></para>
    /// </summary>
    /// <param name="notification_id">
    /// <para>唯一 ID</para>
    /// <para>示例值："6981801914270744596"</para>
    /// </param>
    /// <param name="request">取消推送请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/notifications/{notification_id}/cancel_send")]
    Task<FeishuNullDataApiResult?> CancelSendNotificationAsync(
        [Path] string notification_id,
        [Body] CancelSendNotificationRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消审批
    /// <para>提交审批后调用本接口取消审批。限频：10 次/分钟。</para>
    /// <para>本接口无请求体、无查询参数。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/notification/cancel_approve">接口文档</see></para>
    /// </summary>
    /// <param name="notification_id">
    /// <para>唯一 ID</para>
    /// <para>示例值："6981801914270744596"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/notifications/{notification_id}/cancel_approve")]
    Task<FeishuNullDataApiResult?> CancelApproveNotificationAsync(
        [Path] string notification_id,
        CancellationToken cancellationToken = default);
}
