// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bot;

namespace Mud.Feishu;


/// <summary>
/// 飞书机器人信息接口，获取当前应用所对应机器人的基本信息，包括应用名称、头像、可用状态、IP 白名单与 open_id。
/// <para>调用前需在开发者后台为应用开启机器人能力并发布版本。</para>
/// <para><see href="https://open.feishu.cn/document/client-docs/bot-v3/obtain-bot-info">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Bot")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV3BotInfo : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取机器人信息。
    /// <para>获取机器人的基本信息。调用前需在开发者后台开启机器人能力并发布应用版本。</para>
    /// <para>该接口响应无 data 包装层，bot 字段位于响应体顶层。</para>
    /// <para><see href="https://open.feishu.cn/document/client-docs/bot-v3/obtain-bot-info">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bot/v3/info")]
    Task<GetBotInfoApiResult?> GetBotInfoAsync(
      CancellationToken cancellationToken = default);
}
