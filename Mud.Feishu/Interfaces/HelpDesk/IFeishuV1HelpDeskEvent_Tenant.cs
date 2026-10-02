// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu;

/// <summary>
/// 飞书服务台事件订阅API是开放平台基于飞书服务台的事件功能开放的订阅/取消订阅API，开发者可以基于这些API管理服务台事件的订阅状态。
/// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/event/subscribe">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "HelpDesk")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HelpDeskEvent : IFeishuAppContextSwitcher
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
    /// 订阅服务台事件
    /// <para>用于订阅服务台事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/event/subscribe">接口文档</see></para>
    /// </summary>
    /// <param name="request">订阅服务台事件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/events/subscribe")]
    Task<FeishuNullDataApiResult?> SubscribeEventAsync(
        [Body] SubscribeEventRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消订阅服务台事件
    /// <para>用于取消订阅服务台事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/event/unsubscribe">接口文档</see></para>
    /// </summary>
    /// <param name="request">取消订阅服务台事件请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/events/unsubscribe")]
    Task<FeishuNullDataApiResult?> UnsubscribeEventAsync(
        [Body] UnsubscribeEventRequest request,
        CancellationToken cancellationToken = default);
}
