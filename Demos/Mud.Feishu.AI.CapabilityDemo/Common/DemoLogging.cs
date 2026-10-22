// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Serilog;
using Serilog.Events;

namespace Mud.Feishu.AI.CapabilityDemo.Common;

/// <summary>
/// 运行时日志：控制台只收 Warning（不干扰 REPL），文件全量。
/// </summary>
/// <remarks>
/// <b>MCP 模式必须关掉 Console sink</b>：stdout 是 JSON-RPC 协议通道，任何一行日志都会让客户端解析失败。
/// </remarks>
internal static class DemoLogging
{
    public const string SectionName = "Serilog";
    public const string FileRelativePath = "logs/cap-demo-.log";
    private const long FileSizeLimitBytes = 10 * 1024 * 1024;

    public static IServiceCollection AddDemoLogging(
        this IServiceCollection services,
        IConfiguration configuration,
        bool consoleAllowed = true)
    {
        ConfigureRootLogger(configuration, consoleAllowed);
        return services.AddLogging(logging => logging.AddSerilog(dispose: false));
    }

    public static void ConfigureRootLogger(IConfiguration configuration, bool consoleAllowed = true)
    {
        var loggerConfiguration = new LoggerConfiguration();

        if (consoleAllowed && configuration.GetSection(SectionName).Exists())
        {
            loggerConfiguration.ReadFrom.Configuration(configuration);
        }
        else
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .WriteTo.File(
                    FileRelativePath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    retainedFileCountLimit: 7);
        }

        Log.Logger = loggerConfiguration.CreateLogger();
    }

    public static void CloseAndFlush() => Log.CloseAndFlush();
}
