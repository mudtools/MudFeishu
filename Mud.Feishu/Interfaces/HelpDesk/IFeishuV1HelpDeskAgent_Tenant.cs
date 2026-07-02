// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu;

/// <summary>
/// 飞书服务台API是开放平台基于飞书服务台的知识库/工单/客服等功能模块开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台对应的功能模块进行操作。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "HelpDesk", InheritedFrom = nameof(FeishuV1HelpDeskAgent))]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1HelpDeskAgent : IFeishuV1HelpDeskAgent
{

    /// <summary>
    /// 获取客服邮箱
    /// <para>用于获取客服邮箱地址。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/agent-function/agent/agent_email">接口文档</see></para>
    /// </summary> 
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/agent_emails")]
    Task<FeishuApiResult<GetAgentEmailResult>?> GetAgentEmailAsync(
        CancellationToken cancellationToken = default);
}
