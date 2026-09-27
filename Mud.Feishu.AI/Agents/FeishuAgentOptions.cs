// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Agents;

/// <summary>
/// <see cref="FeishuAgent"/> 配置项（配置节 <see cref="SectionName"/>）。
/// </summary>
/// <remarks>
/// <para>
/// 配置面治理（R4/R5）：每个属性必须有真实消费点（读取处见各属性 XML 注释）；
/// 配置 DTO 不得使用 <c>required</c>（源生成配置绑定器经 <c>new T()</c> 构造会报 CS9035），
/// 合法性统一在 <see cref="Validate"/> 校验；模型 API Key / AppSecret 走
/// <c>FeishuAppConfig</c> 既有安全面，<b>不得</b>在本类复制任何 Key 字段。
/// </para>
/// </remarks>
public sealed class FeishuAgentOptions
{
    /// <summary>配置节名称（<c>FeishuAgent</c>）。</summary>
    public const string SectionName = "FeishuAgent";

    /// <summary>
    /// 键控 <see cref="IChatClient"/> 的服务键（消费点：服务注册工厂解析键控模型客户端）。
    /// </summary>
    /// <remarks>
    /// 为空时解析未键控的 <see cref="IChatClient"/>。对应
    /// <c>AddFeishuOpenAIChatClient(serviceKey, …)</c> 注册的服务键。
    /// </remarks>
    public string? ModelServiceKey { get; set; }

    /// <summary>
    /// 系统指令（system prompt；消费点：装配进 <c>ChatClientAgentOptions.ChatOptions.Instructions</c>）。
    /// </summary>
    /// <remarks>非空校验见 <see cref="Validate"/>；内容仅由开发者控制（提示注入防线，§7.2）。</remarks>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Agent 展示名（消费点：<c>ChatClientAgentOptions.Name</c> 与 OTel Span 的 <c>feishu.agent.name</c> 属性）。
    /// </summary>
    public string Name { get; set; } = "FeishuAgent";

    /// <summary>
    /// 会话历史裁剪窗（消费点：<c>MessageCountingChatReducer(targetCount)</c> 注入
    /// <c>InMemoryChatHistoryProvider</c>，超出窗口的旧消息被折叠）。
    /// </summary>
    public int MaxHistoryMessages { get; set; } = 50;

    /// <summary>
    /// 工具白名单——<c>MapTool</c> 的配置面等价物（消费点：
    /// <c>Mud.Feishu.AI.FeishuTools</c> 的 <c>AddFeishuReadonlyTools</c> 逐名启用注册表工具）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 值域 = 已注册工具名（Phase 1 为 §3.3.2 的 10 个只读工具名）；工具默认
    /// 「收进注册表不启用」，仅本名单中的名字被 <c>MapTool</c> 启用后才会暴露给模型。
    /// 白名单中出现未注册的名字在工具注册期即失败（fail-fast，防配置漂移）。
    /// </para>
    /// </remarks>
    public string[] Tools { get; set; } = [];

    /// <summary>
    /// 工具结果回填文本的最大长度（消费点：<c>FeishuToolBinding</c> 对下游结果做
    /// 白名单投影后按此上限截断并标记 <c>truncated</c>——防大结果撑爆模型上下文，Phase 1 §3.3.1）。
    /// </summary>
    public int MaxToolResultLength { get; set; } = 4000;

    /// <summary>
    /// 是否强制工具授权门禁（消费点：<c>FeishuToolBinding</c>——为 <see langword="true"/> 时
    /// 写类（<c>IsWrite</c>）工具未注册授权器即拒绝、未过授权即不调下游接口；只读工具钩子预留）。
    /// </summary>
    /// <remarks>
    /// 安全默认：与宿主强制门禁语义一致（总体设计 §7.2；与官方 CLI 的自愿 dry-run 相区分，已决策⑥）。
    /// </remarks>
    public bool EnforceToolAuthorization { get; set; } = true;

    /// <summary>
    /// 校验配置合法性（注册时与 <see cref="FeishuAgent"/> 构造时双触发，fail-fast）。
    /// </summary>
    /// <exception cref="InvalidOperationException">存在非法取值。</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Instructions))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(Instructions)} 不能为空——空指令将使 Agent 失去角色与边界约束");

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException($"FeishuAgent:{nameof(Name)} 不能为空");

        if (MaxHistoryMessages < 1)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxHistoryMessages)} 须 ≥ 1，实际值: {MaxHistoryMessages.ToString(CultureInfo.InvariantCulture)}");

        if (MaxToolResultLength < 1)
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(MaxToolResultLength)} 须 ≥ 1，实际值: {MaxToolResultLength.ToString(CultureInfo.InvariantCulture)}");

        if (Tools.Any(t => string.IsNullOrWhiteSpace(t)))
            throw new InvalidOperationException(
                $"FeishuAgent:{nameof(Tools)} 白名单不能包含空项——工具名是模型可见契约");
    }
}
