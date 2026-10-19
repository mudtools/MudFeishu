// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Acts;
using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Mcp;
using Mud.Feishu.AI.CapabilityDemo.SelfCheck;

namespace Mud.Feishu.AI.CapabilityDemo;

/// <summary>
/// 技术博客配套 Demo：八幕 + <c>--verify</c>。
/// </summary>
/// <remarks>
/// <b>MCP 分流必须先于任何 <c>Console.WriteLine</c></b>：stdout 是 JSON-RPC 协议通道。
/// </remarks>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = CliOptions.Parse(args);
        if (cli.ShowHelp)
        {
            PrintHelp();
            return 0;
        }

        var sources = DemoConfig.Build(AppContext.BaseDirectory);
        var isMcp = string.Equals(cli.Act, "mcp", StringComparison.OrdinalIgnoreCase);

        DemoLogging.ConfigureRootLogger(sources.Configuration, consoleAllowed: !isMcp);

        try
        {
            if (cli.Verify)
            {
                return SelfCheckRunner.Run();
            }

            if (isMcp)
            {
                var mcpSettings = DemoSettings.FromConfiguration(sources.Configuration);
                return await McpStdioRunner.RunAsync(sources, mcpSettings, cli.AppKey);
            }

            if (cli.List || cli.Act is null)
            {
                PrintCatalog();
                if (cli.Act is null && !Console.IsInputRedirected)
                {
                    return await MenuAsync(sources);
                }

                return 0;
            }

            var descriptor = ActCatalog.Resolve(cli.Act);
            if (descriptor is null)
            {
                Console.Error.WriteLine($"未知幕 '{cli.Act}'。可用值见 --list。");
                return 2;
            }

            return await RunActAsync(descriptor, sources, cli);
        }
        finally
        {
            DemoLogging.CloseAndFlush();
        }
    }

    private static async Task<int> RunActAsync(
        ActDescriptor descriptor, DemoConfigurationSources sources, CliOptions cli)
    {
        var settings = DemoSettings.FromConfiguration(sources.Configuration);

        return descriptor.Id switch
        {
            "a1" => await A1HelloAgentAct.RunAsync(sources, settings, CancellationToken.None),
            "a2" => A2SurfaceAct.Run(),
            "a3" => await A3MetaToolsAct.RunAsync(sources, settings, CancellationToken.None),
            "a4" => await A4ChainAct.RunAsync(cli.Case, settings, CancellationToken.None),
            "a5" => await A5HitlAct.RunAsync(sources, settings, cli.IncludeDelete, CancellationToken.None),
            "a6" => await A6McpAct.RunAsync(settings, CancellationToken.None),
            "a7" => A7SkillsAct.Run(cli.Domain ?? settings.SkillDomain),
            "a8" => await A8TenantsAct.RunAsync(sources, settings, CancellationToken.None),
            _ => 2,
        };
    }

    private static async Task<int> MenuAsync(DemoConfigurationSources sources)
    {
        while (true)
        {
            DemoConsole.Line();
            DemoConsole.Prompt();
            var input = Console.ReadLine();
            if (input is null)
            {
                return 0;
            }

            input = input.Trim();
            if (input is "q" or "quit" or "exit")
            {
                return 0;
            }

            if (input.Length == 0)
            {
                continue;
            }

            var descriptor = ActCatalog.Resolve(input);
            if (descriptor is null)
            {
                DemoConsole.Notice(NoticeLevel.Warn, "输入幕编号（a1…a8 / verify）或别名，q 退出。");
                continue;
            }

            return await RunActAsync(descriptor, sources, new CliOptions());
        }
    }

    private static void PrintCatalog()
    {
        DemoConsole.Banner("Mud.Feishu.AI.CapabilityDemo · 技术博客配套演示", new Dictionary<string, string?>
        {
            ["用法"] = "dotnet run -- --act <a1…a8|verify>",
            ["离线"] = "a2 / a4 / a6 / a7 / verify 不需要任何凭证",
        });

        DemoConsole.Table(
            ["ID", "别名", "标题", "博客章节", "模型", "真实数据"],
            ActCatalog.All.Select(a => new[]
            {
                a.Id,
                a.Alias,
                a.Title,
                a.BlogSection,
                a.RequiresModel ? "✓" : "-",
                a.RequiresData ? "✓" : "-",
            }).ToArray());

        DemoConsole.Line();
        DemoConsole.Line("输入编号或别名进入（Ctrl+C 退出）：");
    }

    private static void PrintHelp()
    {
        DemoConsole.Line("""
            Mud.Feishu.AI.CapabilityDemo —— 技术博客配套 Demo

            用法：
              --list                          打印幕目录
              --act <id|alias>                运行指定幕（a1…a8）
              --act mcp                       以 stdio 协议运行 MCP server（stdout 只有 JSON-RPC）
              --verify                        离线自检矩阵（8 组，不需任何凭证）
              --case a|b|c|d|e|all            A4 用例（默认 all）
              --domain <name>                 A7 展开的域（默认取配置 SkillDomain）
              --include-delete                A5 把 bitable.delete_record 加进白名单（演示"批准≠放行"）
              --app <appKey>                  MCP 模式指定租户（多租户 = 每租户一个进程）
              --help                          本帮助

            配置：appsettings.local.json（密钥只写这里；A8 需 FeishuApps 两个应用）。
            """);
    }

    /// <summary>极简 CLI（不引第三方解析器）。</summary>
    private sealed class CliOptions
    {
        public string? Act { get; private set; }
        public bool List { get; private set; }
        public bool Verify { get; private set; }
        public bool ShowHelp { get; private set; }
        public string? Case { get; private set; }
        public string? Domain { get; private set; }
        public string? AppKey { get; private set; }
        public bool IncludeDelete { get; private set; }

        public static CliOptions Parse(string[] args)
        {
            var options = new CliOptions();

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var (name, inlineValue) = Split(arg);

                string? NextValue()
                    => inlineValue ?? (i + 1 < args.Length && !args[i + 1].StartsWith('-') ? args[++i] : null);

                switch (name)
                {
                    case "--act":
                        options.Act = NextValue();
                        break;
                    case "--case":
                        options.Case = NextValue();
                        break;
                    case "--domain":
                        options.Domain = NextValue();
                        break;
                    case "--app":
                        options.AppKey = NextValue();
                        break;
                    case "--list":
                        options.List = true;
                        break;
                    case "--verify":
                        options.Verify = true;
                        break;
                    case "--include-delete":
                        options.IncludeDelete = true;
                        break;
                    case "--all":
                        options.Case = "all";
                        break;
                    case "--help":
                    case "-h":
                    case "/?":
                        options.ShowHelp = true;
                        break;
                }
            }

            return options;

            static (string Name, string? Value) Split(string arg)
            {
                var index = arg.IndexOf('=');
                return index < 0 ? (arg, null) : (arg[..index], arg[(index + 1)..]);
            }
        }
    }
}
