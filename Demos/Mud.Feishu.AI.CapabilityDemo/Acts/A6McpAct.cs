// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;
using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.CapabilityDemo.Acts;

/// <summary>
/// A6：MCP stdio 的离线演示——同一份白名单、同一套工具面，走 JSON-RPC。
/// </summary>
/// <remarks>
/// <b>MCP 没有审批管线</b>：<c>ApprovalRequiredAIFunction</c> 在这里只是标记，不会拦下写工具。
/// 因此本幕<b>不暴露任何写工具</b>；进程外形态的真实门禁 = 白名单 + 授权器。
/// </remarks>
internal static class A6McpAct
{
    private const string ToolSearchWire = "feishu_tool_search";

    public static async Task<int> RunAsync(DemoSettings settings, CancellationToken cancellationToken)
    {
        settings.Validate(requireModel: false, requireApp: false, requireData: false);

        var configuration = DemoConfig.CreateVerifyConfiguration();
        await using var provider = CompositionRoot.Build(
            configuration,
            settings with { AppKey = "cap-verify" },
            ActProfiles.A6(settings),
            includeModel: false,
            consoleAllowed: true,
            configureServices: services => services.AddFeishuMcpServer(options =>
            {
                options.AppKey = "cap-verify";
                options.ServerName = "mud-feishu";
                options.IncludeGuidance = true;
            }));

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var enabled = registry.EnabledTools;

        DemoConsole.Banner("A6 · 进程外：MCP stdio（离线回放）", new Dictionary<string, string?>
        {
            ["服务名"] = "mud-feishu",
            ["暴露面"] = $"{enabled.Count} 枚（只读 + 元工具；无写工具）",
            ["协议"] = "JSON-RPC over stdio：initialize / tools/list / tools/call / resources/*",
            ["安全事实"] = "MCP 不走 MAF 审批：写工具的真实门禁是白名单 + 授权器",
        });

        DemoConsole.Section("mcp.json（用编译产物，不要用 dotnet run——构建输出会污染 stdout）");
        var dll = Path.Combine(AppContext.BaseDirectory, "Mud.Feishu.AI.CapabilityDemo.dll");
        DemoConsole.Line($$"""
        {
          "mcpServers": {
            "feishu-bitable": {
              "command": "dotnet",
              "args": ["{{dll.Replace("\\", "/")}}", "--act", "mcp"]
            }
          }
        }
        """);

        DemoConsole.Section("契约名 ↔ 线名（客户端只认下划线）");
        DemoConsole.Table(
            ["契约名", "线名"],
            enabled.Select(t => new[] { t.Name, t.Name.Replace('.', '_') }).ToArray());

        DemoConsole.Section("协议回放");
        var requests = new[]
        {
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"cap-demo","version":"1.0"}}}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"feishu_tool_search","arguments":{"keyword":"多维表格"}}}""",
            """{"jsonrpc":"2.0","id":4,"method":"resources/list"}""",
            """{"jsonrpc":"2.0","id":5,"method":"resources/read","params":{"uri":"feishu-guidance://bitable"}}""",
            """{"jsonrpc":"2.0","id":6,"method":"tools/call","params":{"name":"bitable_delete_record","arguments":{}}}""",
        };

        using var input = new StringReader(string.Join('\n', requests) + "\n");
        using var output = new StringWriter(CultureInfo.InvariantCulture);

        var handled = await provider.RunFeishuMcpStdioAsync(input, output, cancellationToken);
        var lines = output.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        DemoConsole.Notice(NoticeLevel.Info, $"已处理 {handled} 条请求，输出 {lines.Length} 行。");

        var allJson = true;
        int? toolCount = null;
        var wireNamesOk = true;
        int? lastErrorCode = null;

        foreach (var line in lines)
        {
            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(line);
            }
            catch (JsonException)
            {
                allJson = false;
                DemoConsole.Notice(NoticeLevel.Error, $"非 JSON 行（stdout 被污染）：{line}");
                continue;
            }

            using (document)
            {
                var root = document.RootElement;
                var id = root.TryGetProperty("id", out var idElement) ? idElement.GetInt32() : 0;

                if (root.TryGetProperty("error", out var error) && error.TryGetProperty("code", out var code))
                {
                    lastErrorCode = code.GetInt32();
                }

                if (root.TryGetProperty("result", out var result))
                {
                    if (id == 2 && result.TryGetProperty("tools", out var tools))
                    {
                        toolCount = tools.GetArrayLength();
                        wireNamesOk = tools.EnumerateArray()
                            .Select(static t => t.GetProperty("name").GetString() ?? string.Empty)
                            .All(static name => !name.Contains('.', StringComparison.Ordinal));
                    }

                    if (id == 1)
                    {
                        var protocol = result.TryGetProperty("protocolVersion", out var version)
                            ? version.GetString()
                            : "?";
                        var instructions = result.TryGetProperty("instructions", out var ins)
                            ? ins.GetString()?.Length ?? 0
                            : 0;
                        DemoConsole.Line($"  id=1 initialize → protocol={protocol}，instructions={instructions} 字符");
                    }
                    else if (id == 2)
                    {
                        DemoConsole.Line($"  id=2 tools/list → {toolCount} 枚，线名无 '.'={wireNamesOk}");
                    }
                    else if (id == 3)
                    {
                        var text = result.TryGetProperty("content", out var content) && content.GetArrayLength() > 0
                            ? content[0].TryGetProperty("text", out var t) ? t.GetString() : null
                            : null;
                        DemoConsole.Line($"  id=3 tools/call(tool_search) → {Shorten(text)}");
                    }
                    else if (id == 4)
                    {
                        var count = result.TryGetProperty("resources", out var resources) ? resources.GetArrayLength() : 0;
                        DemoConsole.Line($"  id=4 resources/list → {count} 个资源");
                    }
                    else if (id == 5)
                    {
                        DemoConsole.Line("  id=5 resources/read → OK（JSON 有效）");
                    }
                }
                else if (lastErrorCode is not null && id == 6)
                {
                    DemoConsole.Line($"  id=6 未启用工具 → error.code={lastErrorCode}");
                }
            }
        }

        var pass = allJson
                   && toolCount == enabled.Count
                   && wireNamesOk
                   && lastErrorCode == -32602;

        DemoConsole.Line();
        DemoConsole.Notice(pass ? NoticeLevel.Info : NoticeLevel.Error,
            pass
                ? "自检通过：stdout 全为 JSON、tools/list 与白名单同源、未启用工具被拒（-32602）。"
                : $"自检失败：json={allJson} toolCount={toolCount}/{enabled.Count} wire={wireNamesOk} lastError={lastErrorCode}");

        return pass ? 0 : 1;
    }

    private static string Shorten(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return "（空）";
        }

        var singleLine = text.Replace('\n', ' ').Replace('\r', ' ');
        return singleLine.Length <= 100 ? singleLine : singleLine[..97] + "...";
    }
}
