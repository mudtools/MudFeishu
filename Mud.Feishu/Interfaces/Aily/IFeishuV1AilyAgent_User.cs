// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu;

/// <summary>
/// 飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的智能体对话、会话管理、附件与产物能力，把它集成到自己的业务系统里。
/// <para>以 user_access_token（用户身份）调用；除基础接口方法外，还提供仅支持用户身份的智能体可见性查询。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_chat/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Aily", InheritedFrom = nameof(FeishuV1AilyAgent))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1AilyAgent : IFeishuV1AilyAgent, ICurrentUserId
{
    /// <summary>
    /// 获取智能体可见性
    /// <para>查询当前调用用户对指定智能体的可见性。接口根据 UserAccessToken（用户身份凭证）解析出当前用户，结合传入的 channel_type（渠道类型），返回可见性。</para>
    /// <para>注意事项：</para>
    /// <para>- 仅支持 user_access_token（用户身份凭证），不支持 tenant_access_token；</para>
    /// <para>- 典型用于 WebSDK 场景：前端据此决定是否向当前用户展示该智能体。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent-agent_visibility/check">接口文档</see></para>
    /// </summary>
    /// <param name="agent_id">智能体 ID，通过智能体后台详情的地址栏中获取，示例值：agent_4k4ue29hpwrx2</param>
    /// <param name="request">获取可见性请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回当前用户对该智能体的可见性</returns>
    [Post("/open-apis/aily/v1/agents/{agent_id}/agent_visibility/check")]
    Task<FeishuApiResult<CheckAgentVisibilityResult>?> CheckAgentVisibilityAsync(
        [Path] string agent_id,
        [Body] CheckAgentVisibilityRequest request,
        CancellationToken cancellationToken = default);
}
