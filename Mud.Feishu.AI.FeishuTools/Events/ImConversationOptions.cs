// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Events;

/// <summary>
/// 内置 IM 会话事件处理器配置（AI-FD-D12 P2D-5a，配置节 <c>FeishuAgent:ImConversation</c>）。
/// </summary>
/// <remarks>
/// 配置面治理（R4/R5）：每个属性有唯一消费点（<see cref="ImMessageConversationalEventHandler"/>）；
/// 配置 DTO 禁用 <c>required</c>（源生成配置绑定经 <c>new T()</c> 构造），布尔属性无非法取值，
/// <see cref="Validate"/> 仅做签名级兜底。
/// </remarks>
public sealed class ImConversationOptions
{
    /// <summary>配置节名称（嵌套于 <c>FeishuAgent</c> 之下）。</summary>
    public const string SectionName = "FeishuAgent:ImConversation";

    /// <summary>
    /// 群聊是否仅响应 @Bot 消息（消费点：群聊事件过滤；单聊不受影响）。
    /// </summary>
    /// <remarks>默认 true——群聊消息风暴防线；false 时群聊任意消息都进入会话（事件量大，谨慎）。</remarks>
    public bool RequireMentionInGroup { get; set; } = true;

    /// <summary>
    /// 是否启用单聊会话（消费点：单聊事件过滤）。
    /// </summary>
    /// <remarks>默认 true；仅做任务型 Bot 的宿主可关闭单聊，收窄可对话面。</remarks>
    public bool AllowP2pConversation { get; set; } = true;

    /// <summary>校验配置合法性（注册时触发，fail-fast）。</summary>
    public void Validate()
    {
        // 布尔属性无非法取值；保留方法与 FeishuAgentOptions/ImConversationOptions 同批守卫的签名级约定。
    }
}
