// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.WebSockets;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.WebSocket;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// R5.2.7/X5 生产加固：证书安全旁路（<c>Mode=Dev</c> / <c>ValidateServerCertificate=false</c>）
/// 在生产环境应输出 <c>LogError</c>（不阻断启动），非生产环境保持 <c>LogWarning</c>。
/// </summary>
/// <remarks>
/// <para>
/// 复用 Webhook ADR-4 的「缺省即 Production」安全默认：
/// 未注入 <see cref="IHostEnvironment"/> 时回退读取 <c>DOTNET_ENVIRONMENT</c> / <c>ASPNETCORE_ENVIRONMENT</c>，
/// 均未设置按 Production 处理（宁可多告警，不可漏告警）。
/// </para>
/// <para>
/// major 版本再评估是否升级为启动阻断（Validate 失败）——本批次仅告警升级，见迁移表。
/// </para>
/// </remarks>
public class WebSocketCertificateProductionHardeningTests
{
    private static readonly Uri WssUri = new("wss://open.feishu.cn/ws");

    // ────────────────────────────────────────────────────────────────────
    // 测试基建：捕获日志的 ILogger + 假 IHostEnvironment
    // ────────────────────────────────────────────────────────────────────

    private sealed class CapturingLogger : ILogger<WebSocketConnectionManager>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = new();

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Entries.Add((logLevel, formatter(state, exception)));
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Production";
        public string ApplicationName { get; set; } = "Mud.Feishu.Tests";
        public string ContentRootPath { get; set; } = ".";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static bool InvokeResolveIsProduction(IHostEnvironment? hostEnvironment)
    {
        var method = typeof(WebSocketConnectionManager).GetMethod(
            "ResolveIsProduction", BindingFlags.Static | BindingFlags.NonPublic);

        method.Should().NotBeNull("ResolveIsProduction 是 R5.2.7 生产判定的核心入口");

        return (bool)method!.Invoke(null, [hostEnvironment])!;
    }

    private static (CapturingLogger Logger, WebSocketConnectionManager Manager) CreateManager(
        FeishuWebSocketOptions options, IHostEnvironment? hostEnvironment)
    {
        var logger = new CapturingLogger();
        var manager = new WebSocketConnectionManager(logger, options, NullLoggerFactory.Instance, hostEnvironment);
        return (logger, manager);
    }

    private static void ConfigureCertificate(WebSocketConnectionManager manager, ClientWebSocket socket)
    {
        var method = typeof(WebSocketConnectionManager).GetMethod(
            "ConfigureCertificateValidation", BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull("证书校验配置入口必须存在（R5/X5 改造点）");
        method!.Invoke(manager, [socket, WssUri]);
    }

    private static FeishuWebSocketOptions OptionsWith(
        CertificateValidationMode mode, bool validateServerCertificate = true) =>
        new()
        {
            Certificate = new WebSocketCertificateOptions
            {
                Mode = mode,
                ValidateServerCertificate = validateServerCertificate
            }
        };

    // ────────────────────────────────────────────────────────────────────
    // ResolveIsProduction：环境判定
    // ────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Production", true)]
    [InlineData("production", true)] // 大小写不敏感
    [InlineData("Development", false)]
    [InlineData("Staging", false)]
    public void ResolveIsProduction_WithHostEnvironment_ShouldUseEnvironmentName(string envName, bool expected)
    {
        InvokeResolveIsProduction(new FakeHostEnvironment { EnvironmentName = envName })
            .Should().Be(expected);
    }

    [Fact]
    public void ResolveIsProduction_WithoutHostEnvironment_AndNoEnvVars_ShouldDefaultToProduction()
    {
        var savedDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var savedAspNet = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", null);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", null);

            // 「缺省即 Production」的安全默认：宁可多告警，不可漏告警
            InvokeResolveIsProduction(null).Should().BeTrue();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", savedDotnet);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", savedAspNet);
        }
    }

    [Fact]
    public void ResolveIsProduction_WithoutHostEnvironment_ShouldPreferDotnetEnvVar()
    {
        var savedDotnet = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var savedAspNet = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        try
        {
            // DOTNET_ENVIRONMENT 优先于 ASPNETCORE_ENVIRONMENT（通用主机语义）
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

            InvokeResolveIsProduction(null).Should().BeFalse();
        }
        finally
        {
            Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", savedDotnet);
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", savedAspNet);
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 运行时日志级别：生产 LogError，非生产 LogWarning
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void DevMode_InProduction_ShouldLogError()
    {
        var (logger, manager) = CreateManager(
            OptionsWith(CertificateValidationMode.Dev), new FakeHostEnvironment { EnvironmentName = "Production" });

        using var socket = new ClientWebSocket();
        ConfigureCertificate(manager, socket);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error && e.Message.Contains("生产环境"),
            "生产环境的 Mode=Dev 是安全面旁路，必须以 Error 级别呈现");
    }

    [Fact]
    public void DevMode_InDevelopment_ShouldKeepLogWarning()
    {
        var (logger, manager) = CreateManager(
            OptionsWith(CertificateValidationMode.Dev), new FakeHostEnvironment { EnvironmentName = "Development" });

        using var socket = new ClientWebSocket();
        ConfigureCertificate(manager, socket);

        logger.Entries.Should().Contain(e => e.Level == LogLevel.Warning && e.Message.Contains("Dev 模式"));
        logger.Entries.Should().NotContain(e => e.Level == LogLevel.Error,
            "非生产环境保持既有 Warning，不制造噪音");
    }

    [Fact]
    public void ValidateServerCertificateFalse_InProduction_ShouldLogError()
    {
        var (logger, manager) = CreateManager(
            OptionsWith(CertificateValidationMode.Strict, validateServerCertificate: false),
            new FakeHostEnvironment { EnvironmentName = "Production" });

        using var socket = new ClientWebSocket();
        ConfigureCertificate(manager, socket);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error && e.Message.Contains("禁用SSL证书验证"),
            "完全关闭校验比 Mode=Dev 更激进，生产环境必须 Error");
    }

    [Fact]
    public void ValidateServerCertificateFalse_InProduction_ViaDevMode_ShouldLogError()
    {
        var (logger, manager) = CreateManager(
            OptionsWith(CertificateValidationMode.Dev, validateServerCertificate: false),
            new FakeHostEnvironment { EnvironmentName = "Production" });

        using var socket = new ClientWebSocket();
        ConfigureCertificate(manager, socket);

        logger.Entries.Should().Contain(e =>
            e.Level == LogLevel.Error && e.Message.Contains("禁用 SSL 证书验证"),
            "「完全关闭校验」优先于 Mode=Dev（G-06 优先级链），生产环境同样必须 Error");
    }
}
