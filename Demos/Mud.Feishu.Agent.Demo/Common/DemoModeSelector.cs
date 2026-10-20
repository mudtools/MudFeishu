// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo;

/// <summary>启动菜单里的一条运行模式。</summary>
/// <param name="Id">稳定标识（菜单可直接输入，如 <c>tools</c>；配置节名同样可输入）。</param>
/// <param name="Title">菜单标题。</param>
/// <param name="Description">一行说明（菜单展示；含所需配置面，便于选择前预判要不要填凭证）。</param>
/// <param name="SectionName">该模式自己的配置节（模式专属参数写在这里）。</param>
/// <param name="EnabledKey">配置文件里的 <c>Enabled</c> 键（<see langword="null"/> = 无开关，如裸模型）。
/// 它<b>不再决定进入哪个模式</b>，只决定菜单的默认项（回车采用的项）。</param>
/// <param name="Run">模式入口。统一接收配置来源——多数模式只读 <c>Configuration</c>，
/// 文档业务智能体还需要 <c>Files</c> 事实行。</param>
internal sealed record DemoModeOption(
    string Id,
    string Title,
    string Description,
    string SectionName,
    string? EnabledKey,
    Func<DemoConfigurationSources, Task> Run)
{
    /// <summary>本模式在配置文件里是否被显式启用（<c>Enabled=true</c>）。</summary>
    /// <param name="configuration">配置来源。</param>
    /// <returns>是否显式启用。</returns>
    public bool IsEnabledByConfiguration(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return EnabledKey is not null && configuration.GetValue<bool?>(EnabledKey) is true;
    }
}

/// <summary>
/// 运行模式目录（<b>唯一定义处</b>）：启动菜单、默认项判定与分发全部从这里取——
/// 新增一个模式只需在此追加一项，入口 <see cref="Program"/> 不需要改动。
/// </summary>
/// <remarks>
/// 顺序即菜单顺序，保持「能力由弱到强」的阅读顺序：裸模型 → 工具冒烟 → IM 接入 → 领域事件 →
/// 文档业务智能体 → Skills 产物消费 → 复合工作流。每项的节名/开关键必须与对应设置记录的常量一致
/// （<c>DemoModeSelectorTests</c> 机械比对，防「加了菜单项但没接到真实设置」）。
/// </remarks>
internal static class DemoModeCatalog
{
    /// <summary>全部运行模式（顺序即菜单顺序）。</summary>
    public static IReadOnlyList<DemoModeOption> All { get; } =
    [
        new(
            "bare",
            "裸模型",
            "一问一答，无工具、无飞书客户端（需模型三项）",
            FeishuDemoSettings.SectionName,
            EnabledKey: null,
            sources => BareModelDemo.RunAsync(sources.Configuration)),

        new(
            "tools",
            "工具冒烟",
            "全域只读工具 + 飞书客户端（需模型三项 + 凭证；可选 StreamChatId）",
            ToolsDemoSettings.SectionName,
            ToolsDemoSettings.EnabledKey,
            sources => ToolsDemo.RunAsync(sources.Configuration)),

        new(
            "im",
            "IM 事件接入",
            "内置 IM 会话处理器一行接入（需模型三项；不连平台通道）",
            ImHandlerDemoSettings.SectionName,
            ImHandlerDemoSettings.EnabledKey,
            sources => ImConversationDemo.RunAsync(sources.Configuration)),

        new(
            "events",
            "领域事件",
            "领域处理器各派发一轮真实事件 + 流式降级链（需模型三项 + 凭证）",
            DomainEventsDemoSettings.SectionName,
            DomainEventsDemoSettings.EnabledKey,
            sources => DomainEventsDemo.RunAsync(sources.Configuration)),

        new(
            "doc",
            "文档业务智能体",
            "文档域 27 枚工具写闭环 + 三道安全闸 + 剧本（需模型三项 + 凭证）",
            DocAgentSettings.SectionName,
            DocAgentSettings.ConfigEnabledKey,
            sources => DocAgentDemo.RunAsync(sources)),

        new(
            "skills",
            "Skills 产物消费",
            "读包内 skills 产物并注入域 guidance（零凭证、零模型，CI 可跑）",
            SkillsDemoSettings.SectionName,
            SkillsDemoSettings.EnabledKey,
            sources => SkillsDemo.RunAsync(sources.Configuration)),

        new(
            "workflow",
            "复合工作流",
            "确定性执行 workflow.meeting_summary / workflow.standup_report（需凭证、不调模型；站会日报另需 CalendarId/UserId）",
            WorkflowDemoSettings.SectionName,
            WorkflowDemoSettings.EnabledKey,
            sources => WorkflowDemo.RunAsync(sources.Configuration)),
    ];

    /// <summary>
    /// 菜单的配置默认项（用户直接回车、或输入不可交互时采用的模式）。
    /// </summary>
    /// <param name="configuration">配置来源。</param>
    /// <returns>默认模式（无任何 <c>Enabled=true</c> 时回落为菜单首项：裸模型）。</returns>
    public static DemoModeOption Default(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        return All.FirstOrDefault(mode => mode.IsEnabledByConfiguration(configuration)) ?? All[0];
    }
}

/// <summary>
/// 启动期的模式选择（<b>进入哪个演示模块由用户选择决定</b>）。
/// </summary>
/// <remarks>
/// <para>
/// 交互式终端：打印模式菜单，读取用户输入（编号 / 标识 / 配置节名；回车 = 配置默认项；<c>q</c> = 退出），
/// 非法输入就地提示后重问。
/// </para>
/// <para>
/// <b>非交互输入（stdin 被重定向 / 管道 / CI）不挂起等待</b>：直接采用配置默认项并打印提示——
/// 与"菜单只服务交互场景"的分工一致，也保证既有脚本化调用行为不变。
/// </para>
/// <para>
/// 输出一律走 <see cref="ConsoleRenderer"/>，读取经 <paramref name="readLine"/> 注入，故整条选择链可测。
/// </para>
/// </remarks>
internal static class DemoModeSelector
{
    /// <summary>选择要运行的模式。</summary>
    /// <param name="sources">已装配的配置来源。</param>
    /// <param name="renderer">终端渲染器（所有输出的唯一出口）。</param>
    /// <param name="interactive">是否交互（<see langword="null"/> = 按 <see cref="Console.IsInputRedirected"/> 自动判定）。</param>
    /// <param name="readLine">读取一行输入（<see langword="null"/> = <see cref="Console.ReadLine"/>）。</param>
    /// <returns>被选中的模式；用户输入 <c>q</c> 取消时为 <see langword="null"/>。</returns>
    public static DemoModeOption? Select(
        DemoConfigurationSources sources,
        ConsoleRenderer renderer,
        bool? interactive = null,
        Func<string?>? readLine = null)
    {
        ArgumentNullException.ThrowIfNull(sources);
        ArgumentNullException.ThrowIfNull(renderer);

        var options = DemoModeCatalog.All;
        var fallback = DemoModeCatalog.Default(sources.Configuration);

        renderer.Banner(
            "Mud.Feishu.Agent.Demo · 选择运行模式",
            new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["配置来源"] = sources.Files.Count == 0
                    ? "（未发现 appsettings*.json）"
                    : string.Join(" + ", sources.Files),
                ["配置默认项"] = fallback.EnabledKey is null
                    ? $"{fallback.Title}（无 Enabled 开关，回落菜单首项）"
                    : $"{fallback.Title}（{fallback.EnabledKey}）",
            });

        if (interactive ?? !Console.IsInputRedirected)
        {
            renderer.Table(
                "请选择运行模式（输入编号或标识；回车 = 配置默认项；q = 退出）",
                ["编号", "模式", "配置节", "说明"],
                [.. options.Select((mode, index) => new[]
                {
                    (index + 1).ToString(CultureInfo.InvariantCulture),
                    string.Equals(mode.Id, fallback.Id, StringComparison.Ordinal) ? $"{mode.Title}（默认）" : mode.Title,
                    mode.SectionName,
                    mode.Description,
                })]);
        }
        else
        {
            renderer.Notice(
                NoticeLevel.Info,
                $"非交互输入（stdin 已重定向）：跳过模式选择，按配置默认项进入「{fallback.Title}」。");
            return fallback;
        }

        var read = readLine ?? Console.ReadLine;
        while (true)
        {
            renderer.InputPrompt($"模式 [1-{options.Count}]（回车 = {fallback.Id}）› ");
            var input = read();

            // EOF（管道输入耗尽 / 终端被关闭）：与"非交互"同处置，不挂起等待。
            if (input is null)
            {
                renderer.Notice(
                    NoticeLevel.Info,
                    $"输入已结束：按配置默认项进入「{fallback.Title}」。");
                return fallback;
            }

            var text = input.Trim();

            // 回车 = 采用配置默认项。
            if (text.Length == 0)
            {
                return fallback;
            }

            if (IsQuit(text))
            {
                renderer.Notice(NoticeLevel.Info, "已取消：未选择任何模式，进程退出。");
                return null;
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var ordinal)
                && ordinal >= 1
                && ordinal <= options.Count)
            {
                return options[ordinal - 1];
            }

            var matched = options.FirstOrDefault(mode =>
                string.Equals(mode.Id, text, StringComparison.OrdinalIgnoreCase)
                || string.Equals(mode.SectionName, text, StringComparison.OrdinalIgnoreCase));

            if (matched is not null)
            {
                return matched;
            }

            renderer.Notice(
                NoticeLevel.Warn,
                $"无效选择：'{ConsoleRenderer.StripAnsi(text)}'——请输入 1~{options.Count}、"
                + $"模式标识（{string.Join(" / ", options.Select(static mode => mode.Id))}）或 q 退出。");
        }
    }

    private static bool IsQuit(string text)
        => string.Equals(text, "q", StringComparison.OrdinalIgnoreCase)
        || string.Equals(text, "exit", StringComparison.OrdinalIgnoreCase)
        || string.Equals(text, "quit", StringComparison.OrdinalIgnoreCase);
}
