// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;
using Mud.Feishu.AI.CapabilityDemo.Host;
using Mud.Feishu.AI.Mcp;

namespace Mud.Feishu.AI.CapabilityDemo.Mcp;

/// <summary>
/// <c>--act mcp</c>：以 stdio 协议运行 MCP server。
/// </summary>
/// <remarks>
/// <b>stdout 是协议通道</b>：本方法不打印横幅、不写任何非 JSON 行；日志只落文件。
/// </remarks>
internal static class McpStdioRunner
{
    public static async Task<int> RunAsync(
        DemoConfigurationSources sources, DemoSettings settings, string? appKeyOverride = null)
    {
        settings.Validate(requireModel: false, requireApp: true, requireData: false);
        var configuration = DemoConfig.EnsureAppSection(sources.Configuration, settings);

        // 进程即租户边界：appKey 只能来自宿主配置/启动参数，绝不从 MCP 协议参数读取。
        var appKey = string.IsNullOrWhiteSpace(appKeyOverride) ? settings.AppKey : appKeyOverride!;

        await using var provider = CompositionRoot.Build(
            configuration,
            settings,
            ActProfiles.A6(settings),
            includeModel: false,
            consoleAllowed: false,
            configureServices: services => services.AddFeishuMcpServer(options =>
            {
                options.AppKey = appKey;
                options.UserId = settings.UserId;
                options.ServerName = "mud-feishu";
                options.IncludeGuidance = true;
            }));

        return await provider.RunFeishuMcpStdioAsync(cancellationToken: CancellationToken.None);
    }
}
