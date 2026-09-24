// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 Aily SDK 是一组服务端 OpenAPI 的封装，用于让用户以编程方式调用飞书 Aily（智能伙伴/智能体）的能力，把它集成到自己的业务系统里。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/aily-v1/agent/agentuseguide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1AilySessions
{
    /// <summary>
    /// 创建会话
    /// <para>用于创建与某个飞书 Aily 应用的一次会话（Session）；当创建会话成功后，可以发送消息、创建运行。</para>
    /// <para><see href="https://open.feishu.cn/document/aily-v1/aily_session/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建会话请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/aily/v1/sessions")]
    Task<FeishuApiResult<CreateSessionResult>?> CreateSessionAsync(
         [Body] CreateSessionRequest request,
         CancellationToken cancellationToken = default);

}
