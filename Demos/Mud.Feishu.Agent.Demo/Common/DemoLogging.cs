// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// 运行时日志（Serilog）：**控制台只收 Warning+**（不干扰 REPL / 模型流式输出），
/// **文件收全量 Information**（<c>logs/agent-demo-.log</c>，按天滚动，10 MB 上限，保留 7 个）。
/// </summary>
/// <remarks>
/// <para>
/// <b>级别唯一事实源是配置</b>（R5 治理：不设「日志开关」属性）：
/// <c>appsettings*.json</c> 的 <c>Serilog</c> 节（见模板注释）；把类别调到
/// <c>Debug</c>/<c>Information</c> 即可在日志文件里看到 SDK 内部日志。
/// 节缺失时代码兜底（控制台 + 文件），保证「删了节也不静默失明」。
/// </para>
/// <para>
/// <b>装配方式</b>：静态 <see cref="Log.Logger"/> 由 <see cref="ConfigureRootLogger"/> 装配一次
/// （每进程只有一个运行模式）；DI 侧经 <see cref="AddDemoLogging"/> 把 Serilog 桥接进
/// <c>Microsoft.Extensions.Logging</c>（<c>AddFeishuAgent</c> 会从容器解析
/// <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/>，SDK 内部日志由此汇入同一出口）。
/// </para>
/// <para>
/// <b>Serilog 包均来自传递依赖</b>（与其它 Demo 一致，不新增 PackageReference——契约守卫 ⑧ 锁定依赖面）。
/// </para>
/// </remarks>
internal static class DemoLogging
{
    /// <summary>Serilog 配置节名（<c>appsettings*.json</c> 下）。</summary>
    public const string SectionName = "Serilog";

    /// <summary>日志文件相对路径（按天滚动：<c>logs/agent-demo-20260101.log</c>）。</summary>
    public const string FileRelativePath = "logs/agent-demo-.log";

    /// <summary>文件/控制台兜底配置的单文件大小上限（10 MB）。</summary>
    private const long FileSizeLimitBytes = 10 * 1024 * 1024;

    /// <summary>
    /// 向服务容器注册运行时日志：装配根日志器 + 把 Serilog 桥接进 MEL（<c>ILogger&lt;T&gt;</c> 可解析）。
    /// </summary>
    /// <param name="services">服务容器。</param>
    /// <param name="configuration">配置来源（读取 <see cref="SectionName"/> 节）。</param>
    /// <returns>服务容器（链式）。</returns>
    public static IServiceCollection AddDemoLogging(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        ConfigureRootLogger(configuration);
        return services.AddLogging(logging => logging.AddSerilog(dispose: false));
    }

    /// <summary>
    /// 装配根日志器（<see cref="Log.Logger"/>）：<c>Serilog</c> 节存在时以配置为唯一事实源，
    /// 否则用代码兜底（级别 Information、控制台 Warning+、文件全量）。
    /// </summary>
    /// <param name="configuration">配置来源。</param>
    public static void ConfigureRootLogger(IConfiguration configuration)
    {
        var loggerConfiguration = new LoggerConfiguration();

        if (configuration.GetSection(SectionName).Exists())
        {
            loggerConfiguration.ReadFrom.Configuration(configuration);
        }
        else
        {
            loggerConfiguration
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Warning)
                .WriteTo.File(
                    FileRelativePath,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true,
                    fileSizeLimitBytes: FileSizeLimitBytes,
                    retainedFileCountLimit: 7);
        }

        Log.Logger = loggerConfiguration.CreateLogger();
    }
}
