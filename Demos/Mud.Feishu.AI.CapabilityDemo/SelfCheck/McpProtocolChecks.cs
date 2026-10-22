// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;
using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.CapabilityDemo.SelfCheck;

/// <summary>组 6：MCP 协议（stdout 纯 JSON、tools/list 同源、未知工具 -32602）。</summary>
internal static class McpProtocolChecks
{
    public static IEnumerable<CheckResult> Run()
    {
        const string group = "mcp-protocol";
        var settings = new DemoSettings { ModelId = "verify", ApiKey = "verify", AppKey = "cap-verify" };

        using var provider = CompositionRoot.Build(
            DemoConfig.CreateVerifyConfiguration(),
            settings,
            ActProfiles.A6(settings),
            includeModel: false,
            consoleAllowed: false,
            configureServices: services => services.AddFeishuMcpServer(options =>
            {
                options.AppKey = "cap-verify";
                options.ServerName = "mud-feishu";
                options.IncludeGuidance = true;
            }));

        var enabledCount = provider.GetRequiredService<FeishuToolRegistry>().EnabledTools.Count;

        var requests = string.Join('\n',
            """{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-03-26","capabilities":{},"clientInfo":{"name":"verify","version":"1.0"}}}""",
            """{"jsonrpc":"2.0","id":2,"method":"tools/list"}""",
            """{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"bitable_delete_record","arguments":{}}}""") + "\n";

        using var input = new StringReader(requests);
        using var output = new StringWriter(CultureInfo.InvariantCulture);
        var handled = provider.RunFeishuMcpStdioAsync(input, output).GetAwaiter().GetResult();

        var lines = output.ToString()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        yield return new CheckResult(group, "请求全部被处理", handled == 3,
            "3", handled.ToString(CultureInfo.InvariantCulture));

        var parsed = new List<JsonDocument>();
        var invalid = 0;
        foreach (var line in lines)
        {
            try
            {
                parsed.Add(JsonDocument.Parse(line));
            }
            catch (JsonException)
            {
                invalid++;
            }
        }

        yield return new CheckResult(group, "stdout 每行都是 JSON", invalid == 0 && parsed.Count == lines.Length,
            lines.Length.ToString(CultureInfo.InvariantCulture), invalid.ToString(CultureInfo.InvariantCulture));

        try
        {
            var toolsList = parsed.FirstOrDefault(static d =>
                d.RootElement.TryGetProperty("id", out var id) && id.GetInt32() == 2);

            var tools = toolsList?.RootElement.GetProperty("result").GetProperty("tools");
            var count = tools?.GetArrayLength() ?? -1;
            yield return new CheckResult(group, "tools/list 与白名单同源", count == enabledCount,
                enabledCount.ToString(CultureInfo.InvariantCulture), count.ToString(CultureInfo.InvariantCulture));

            var names = tools?.EnumerateArray()
                .Select(static t => t.GetProperty("name").GetString() ?? string.Empty)
                .ToArray() ?? [];
            yield return new CheckResult(group, "线名不含 '.'", names.All(static n => !n.Contains('.', StringComparison.Ordinal)));

            var unknown = parsed.FirstOrDefault(static d =>
                d.RootElement.TryGetProperty("id", out var id) && id.GetInt32() == 3);
            var code = unknown?.RootElement.GetProperty("error").GetProperty("code").GetInt32() ?? 0;
            yield return new CheckResult(group, "未启用工具 -32602", code == -32602, "-32602", code.ToString(CultureInfo.InvariantCulture));
        }
        finally
        {
            foreach (var document in parsed)
            {
                document.Dispose();
            }
        }
    }
}
