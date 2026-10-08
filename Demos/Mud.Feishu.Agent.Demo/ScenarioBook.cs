// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 一部命中式剧本：引导语 + 可选的自动应答序列。
/// </summary>
/// <param name="Name">剧本名（<c>/scenario</c> 的主键）。</param>
/// <param name="Aliases">别名。</param>
/// <param name="Description">一句话说明（含审批次数预告）。</param>
/// <param name="Prompt">引导语（提交给模型的用户输入）。</param>
/// <param name="AutoAnswers">
/// 自动应答序列（<b>仅用于只读链路</b>）：每完成一轮后按序自动提交一条，用于一键跑通多轮链路。
/// 一旦本轮产生挂起审批，自动应答立即停止（写动作一律要人确认）。
/// </param>
internal sealed record AgentScenario(
    string Name,
    IReadOnlyList<string> Aliases,
    string Description,
    string Prompt,
    IReadOnlyList<string> AutoAnswers);

/// <summary>
/// 剧本注册表：注册、按名/别名查询、默认六部剧本的装配。
/// </summary>
/// <remarks>
/// <para>
/// <b>机制</b>：<c>/scenario &lt;名&gt;</c> 把 <see cref="AgentScenario.Prompt"/> 作为用户输入提交，
/// 并把 <see cref="AgentScenario.AutoAnswers"/> 压入 <c>AgentConsoleLoop</c> 的<b>剧本应答栈</b>；
/// 剧本栈在 <c>/reset</c>、<c>/exit</c>、<c>/scenario clear</c> 时清空。
/// </para>
/// <para>
/// <b>为何不自动批准</b>：自动批准会绕过「批准只能来自宿主且由人给出」的核心教学点（ADR-04）。
/// </para>
/// </remarks>
internal sealed class ScenarioBook
{
    /// <summary><c>/scenario clear</c> 的哨兵参数（清空应答栈）。</summary>
    public const string ClearArgument = "clear";

    /// <summary><c>/scenario list</c> 的哨兵参数（仅列清单）。</summary>
    public const string ListArgument = "list";

    private readonly IReadOnlyList<AgentScenario> _all;
    private readonly IReadOnlyList<string> _warnings;

    private ScenarioBook(IReadOnlyList<AgentScenario> all, IReadOnlyList<string> warnings)
    {
        _all = all;
        _warnings = warnings;
    }

    /// <summary>全部剧本（顺序即 <c>/scenario</c> 的展示顺序）。</summary>
    public IReadOnlyList<AgentScenario> All => _all;

    /// <summary>
    /// 装配期收集的告警（前置条件缺失等）。
    /// </summary>
    /// <remarks>
    /// <b>为什么不是构造期直接打印</b>：剧本在 DI 解析期构造，此刻输出会把 Trace 插到启动横幅
    /// <b>之前</b>，读者先看到零散告警再看标题，观感错乱。改为宿主在横幅之后统一输出。
    /// 这也是本方法<b>不接收 <c>ConsoleRenderer</c></b> 的原因（设计文档原签名为
    /// <c>CreateDefault(ConsoleRenderer)</c>；此处以"输出时序正确"为优先，语义等价）。
    /// </remarks>
    public IReadOnlyList<string> Warnings => _warnings;

    /// <summary>
    /// 按名或别名查找（大小写不敏感）。
    /// </summary>
    /// <param name="nameOrAlias">剧本名或别名。</param>
    /// <returns>剧本；未命中时为 <see langword="null"/>。</returns>
    public AgentScenario? Find(string? nameOrAlias)
    {
        if (string.IsNullOrWhiteSpace(nameOrAlias))
        {
            return null;
        }

        var key = nameOrAlias!.Trim();
        return _all.FirstOrDefault(s
            => string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase)
            || s.Aliases.Any(a => string.Equals(a, key, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// 装配默认六部剧本（S1~S6）。
    /// </summary>
    /// <param name="settings">配置（S1 可选的知识库空间 ID）。</param>
    /// <returns>剧本注册表（含装配期告警，见 <see cref="Warnings"/>）。</returns>
    public static ScenarioBook CreateDefault(DocAgentSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        var warnings = new List<string>();

        var resource = DocAgentScenarios.KnowledgeBaseDigest(settings);
        if (string.IsNullOrWhiteSpace(settings.WikiSpaceId))
        {
            warnings.Add(
                $"剧本 {resource.Name} 未配置 {DocAgentSettings.EnvWikiSpaceId}："
                + "引导语会要求模型自行列出知识库空间（多一次只读调用，仍可一键跑通）");
        }

        return new ScenarioBook(
        [
            resource,
            DocAgentScenarios.DocumentAuthoring(),
            DocAgentScenarios.MarkdownImport(),
            DocAgentScenarios.SheetSync(),
            DocAgentScenarios.SafeDelete(),
            DocAgentScenarios.SelfHeal(),
        ],
        warnings);
    }
}
