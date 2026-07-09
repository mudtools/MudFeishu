// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu;

/// <summary>
/// 审批事件是飞书开放平台众多事件中的一项，开发者可以订阅审批事件，在审批单发生数据状态变更时，及时收到通知，并根据数据变化做出相应的业务处理。
/// <para>通过事件订阅，开发者可以实时、自动接收到审批资源的状态变化，而无需轮训审批查询接口获取审批资源的最新状态。降低了开发复杂度，节省了接口查询消耗。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/event/function-introduction"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Approval")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV4ApprovalSubscribe : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 订阅审批事件
    /// <para>当应用订阅审批事件后，需要调用该接口指定审批定义 Code（approval_code）开启订阅，开启后应用才可以接收该审批定义对应的事件。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/approval-v4/event/event-interface/subscribe">接口文档</see></para>
    /// </summary>
    /// <param name="approval_code">
    /// <para>审批定义 Code。获取方式：</para>
    /// <para>- 调用[创建审批定义](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/approval-v4/approval/create)接口后，从响应参数 approval_code 获取。</para>
    /// <para>- 登录审批管理后台，在指定审批定义的 URL 中获取，具体操作参见[什么是 Approval Code](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/approval-v4/approval/overview-of-approval-resources#8151e0ae)。</para>
    /// <para>示例值：7C468A54-8745-2245-9675-08B7C63E7A85</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/approval/v4/approvals/{approval_code}/subscribe")]
    Task<FeishuNullDataApiResult?> SubscribeApprovalEventAsync(
       [Path] string approval_code,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消订阅审批事件
    /// <para>调用订阅审批事件接口订阅审批定义 Code 后，如果不再需要接收该审批定义下的事件订阅通知，可以调用本接口取消订阅审批定义 Code，取消后应用无法再收到该审批定义对应实例的事件通知。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/approval-v4/event/event-interface/unsubscribe">接口文档</see></para>
    /// </summary>
    /// <param name="approval_code">
    /// <para>审批定义 Code。获取方式：</para>
    /// <para>- 调用[创建审批定义](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/approval-v4/approval/create)接口后，从响应参数 approval_code 获取。</para>
    /// <para>- 登录审批管理后台，在指定审批定义的 URL 中获取，具体操作参见[什么是 Approval Code](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/approval-v4/approval/overview-of-approval-resources#8151e0ae)。</para>
    /// <para>示例值：7C468A54-8745-2245-9675-08B7C63E7A85</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/approval/v4/approvals/{approval_code}/unsubscribe")]
    Task<FeishuNullDataApiResult?> UnSubscribeApprovalEventAsync(
       [Path] string approval_code,
       CancellationToken cancellationToken = default);
}
