// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Events;

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

    /// <summary>
    /// 群聊「@ 到 Bot 本人」判定所用的 Bot 显示名（消费点：群聊事件过滤；
    /// 仅在 <see cref="RequireMentionInGroup"/> 为 <see langword="true"/> 时生效）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 配置后，群聊消息必须存在一个 <c>mentions[].name</c> 与本值（忽略大小写）相等的 @ 对象才进入会话；
    /// 否则按「未 @Bot」跳过（与「<c>mentions</c> 为空」同一条跳过路径）。
    /// 这修复了「任何 @（@ 别人、@ 全体）都会触发 Bot 回复」的过度响应（R3-3）。
    /// </para>
    /// <para>
    /// <b>默认 <see langword="null"/> = 不启用该收紧</b>（等价旧行为：「<c>mentions</c> 非空即视为 @Bot」），
    /// 保证升级零破坏。取值须与飞书群内 @ 时呈现的 <c>mentions[].name</c> 一致（通常是 Bot 的群内显示名，
    /// 未必等于应用名）——配置错误表现为「群聊静默不响应」，故先用默认值观察真实 <c>mentions</c> 报文再配置。
    /// </para>
    /// </remarks>
    public string? BotName { get; set; }

    /// <summary>校验配置合法性（注册时触发，fail-fast）。</summary>
    public void Validate()
    {
        // 布尔属性无非法取值；保留方法与 FeishuAgentOptions/ImConversationOptions 同批守卫的签名级约定。
    }
}
