// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalMessage;

namespace Mud.Feishu;

/// <summary>
/// 审批 Bot 消息，用来通过飞书审批的 Bot 推送消息给用户或更新审批 Bot 消息。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/approval-search/query-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Approval")]
[Token(TokenType.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1ApprovalMessage : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 用来通过飞书审批的 Bot 推送消息给用户，当有新的审批待办，或者审批待办的状态有更新时，可以通过飞书审批的 Bot 告知用户。
    /// <para>如果出现推送成功，但是没有收到消息，可能是因为开通了审批机器人的聚合推送。</para>
    /// </summary>
    /// <param name="approvalBotMessageRequest">发送审批 Bot 消息通用模板请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v1/message/send")]
    Task<FeishuApiResult<ApprovalBotMessageResult>?> SendBotMessageAsync(
       [Body] ApprovalBotMessageRequest approvalBotMessageRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 用来通过飞书审批的 Bot 推送消息给用户，当有新的审批待办，或者审批待办的状态有更新时，可以通过飞书审批的 Bot 告知用户。
    /// <para>如果出现推送成功，但是没有收到消息，可能是因为开通了审批机器人的聚合推送。</para>
    /// </summary>
    /// <param name="customApprovalBotMessageRequest">发送审批 Bot 消息我肯定义模板请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v1/message/send")]
    Task<FeishuApiResult<ApprovalBotMessageResult>?> SendBotMessageAsync(
       [Body] CustomApprovalBotMessageRequest customApprovalBotMessageRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 调用发送审批 Bot 消息接口后，可根据审批 Bot 消息 ID 及审批相应的状态，更新审批 Bot 消息。
    /// <para>例如，给审批人推送了审批待办消息，当审批人通过审批后，可以将之前推送的 Bot 消息更新为已审批。</para>
    /// </summary>
    /// <param name="approvalBotMessageUpdateRequest">更新审批 Bot 消息 请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v1/message/update")]
    Task<FeishuApiResult<ApprovalBotMessageResult>?> UpdateBotMessageAsync(
       [Body] ApprovalBotMessageUpdateRequest approvalBotMessageUpdateRequest,
       CancellationToken cancellationToken = default);
}
