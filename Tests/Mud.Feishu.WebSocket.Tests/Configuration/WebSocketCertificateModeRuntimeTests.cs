// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net.Security;
using System.Net.WebSockets;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.WebSocket;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// R5/X5：<see cref="CertificateValidationMode"/> 真正驱动运行时证书校验行为。
/// </summary>
/// <remarks>
/// <para>
/// 改造前 <c>Mode</c> 只被 <c>ValidateCertificateOptions</c> 与 <c>ToString</c> 读取，
/// 运行时分支完全依据四个布尔属性 —— 于是 <c>Validate</c> 的报错文案
/// 「请改 Mode=Dev」把用户引入无效循环（改完仍被拒绝）。
/// </para>
/// <para>
/// 本测试**逐格断言优先级链**，而非只测 <c>Dev</c>/<c>Strict</c> 两格：
/// </para>
/// <code>
/// CustomCallback（Mode=Custom，或 Mode≠Custom 但回调非 null 的兼容分支）
///   &gt; ValidateServerCertificate=false（完全关闭校验）
///   &gt; Mode=Dev（仍校验，仅放宽「自签名根」与「名称不匹配」）
///   &gt; Mode=Strict（默认）
/// </code>
/// <para>
/// 注：本测试取代了 <c>WebSocketCertificateModeOptionsTests.FlatAndNested_ShouldShareStorage</c>。
/// 那条用例是**恒真断言**（先赋 true 再断言 true），R4 删除扁平属性后其名暗示的
/// 「扁平/嵌套共享存储」契约已不存在，属假安全网。
/// </para>
/// </remarks>
public class WebSocketCertificateModeRuntimeTests
{
    private static readonly Uri WssUri = new("wss://open.feishu.cn/ws");

    /// <summary>
    /// 通过真实的 <c>ConfigureCertificateValidation</c> 入口取回运行时回调。
    /// </summary>
    /// <remarks>
    /// 用反射调用私有方法而非把逻辑提取为测试可见 API：本测试应当验证**生产接线**
    /// （配置 → 回调），提取方法会削弱「真的接上了」这一结论。
    /// </remarks>
    private static RemoteCertificateValidationCallback? ResolveRuntimeCallback(FeishuWebSocketOptions options)
    {
        var manager = new WebSocketConnectionManager(
            NullLogger<WebSocketConnectionManager>.Instance,
            options,
            NullLoggerFactory.Instance);

        using var socket = new ClientWebSocket();

        var method = typeof(WebSocketConnectionManager).GetMethod(
            "ConfigureCertificateValidation",
            BindingFlags.Instance | BindingFlags.NonPublic);

        method.Should().NotBeNull("证书校验配置入口必须存在（R5/X5 改造点）");

        try
        {
            method!.Invoke(manager, [socket, WssUri]);
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        return socket.Options.RemoteCertificateValidationCallback;
    }

    private static FeishuWebSocketOptions OptionsWith(
        CertificateValidationMode mode,
        bool validateServerCertificate = true,
        RemoteCertificateValidationCallback? customCallback = null) =>
        new()
        {
            Certificate = new WebSocketCertificateOptions
            {
                Mode = mode,
                ValidateServerCertificate = validateServerCertificate,
                CustomCallback = customCallback
            }
        };

    // ────────────────────────────────────────────────────────────────────
    // Mode = Strict（默认）
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Strict_ShouldRejectNameMismatch()
    {
        var callback = ResolveRuntimeCallback(OptionsWith(CertificateValidationMode.Strict));

        callback.Should().NotBeNull("Strict 必须安装校验回调（不能退化为「不校验」）");
        callback!(null, null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
            .Should().BeFalse("Strict 模式必须拒绝名称不匹配");
    }

    [Fact]
    public void Strict_ShouldAcceptCleanCertificate()
    {
        var callback = ResolveRuntimeCallback(OptionsWith(CertificateValidationMode.Strict));

        callback!(null, null, null, SslPolicyErrors.None).Should().BeTrue();
    }

    // ────────────────────────────────────────────────────────────────────
    // Mode = Dev（本次修复的核心：此前不产生任何运行时效果）
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Dev_ShouldBeWiredIntoRuntime_AndAllowNameMismatch()
    {
        var callback = ResolveRuntimeCallback(OptionsWith(CertificateValidationMode.Dev));

        callback.Should().NotBeNull("Mode=Dev 必须真正安装放宽后的回调（修复伪可配置）");
        callback!(null, null, null, SslPolicyErrors.RemoteCertificateNameMismatch)
            .Should().BeTrue("Dev 模式允许名称不匹配");
    }

    [Fact]
    public void Dev_ShouldStillRejectChainErrorsWithoutSelfSignedRoot()
    {
        // chain 为 null → IsSelfSignedRoot 返回 false → 即使 Dev 也要拒绝
        var callback = ResolveRuntimeCallback(OptionsWith(CertificateValidationMode.Dev));

        callback!(null, null, null, SslPolicyErrors.RemoteCertificateChainErrors)
            .Should().BeFalse("Dev 只在「自签名根」场景放宽链错误；过期/已撤销等仍须拒绝（沿用 WS-12 收紧语义）");
    }

    // ────────────────────────────────────────────────────────────────────
    // Mode = Custom
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void Custom_WithCallback_ShouldUseThatExactCallback()
    {
        RemoteCertificateValidationCallback custom = (_, _, _, _) => true;

        var callback = ResolveRuntimeCallback(
            OptionsWith(CertificateValidationMode.Custom, customCallback: custom));

        callback.Should().BeSameAs(custom);
    }

    // ────────────────────────────────────────────────────────────────────
    // 兼容分支（G-05）：Mode≠Custom 但提供了回调 —— 改造前回调优先级最高且不看 Mode
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void NonCustomMode_WithCustomCallback_ShouldKeepUsingCallback_ForBackwardCompatibility()
    {
        RemoteCertificateValidationCallback custom = (_, _, _, _) => true;

        var callback = ResolveRuntimeCallback(
            OptionsWith(CertificateValidationMode.Strict, customCallback: custom));

        callback.Should().BeSameAs(custom,
            "改造前 CustomCallback 优先级最高且不检查 Mode；若改为「只在 Mode=Custom 时生效」，" +
            "会让「只配回调、不配 Mode」的存量部署静默改用严格回调 —— 安全面行为突变");
    }

    // ────────────────────────────────────────────────────────────────────
    // 优先级链（G-06）：ValidateServerCertificate=false 优先于 Mode
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void ValidateServerCertificateFalse_ShouldFullyDisableValidation_EvenInDevMode()
    {
        var callback = ResolveRuntimeCallback(
            OptionsWith(CertificateValidationMode.Dev, validateServerCertificate: false));

        callback.Should().NotBeNull();
        callback!(null, null, null, SslPolicyErrors.RemoteCertificateChainErrors)
            .Should().BeTrue("ValidateServerCertificate=false 是「完全关闭校验」能力，优先于 Mode=Dev");
    }

    [Fact]
    public void ValidateServerCertificateFalse_ShouldFullyDisableValidation_InStrictMode()
    {
        var callback = ResolveRuntimeCallback(
            OptionsWith(CertificateValidationMode.Strict, validateServerCertificate: false));

        callback.Should().NotBeNull();
        callback!(null, null, null, SslPolicyErrors.RemoteCertificateChainErrors)
            .Should().BeTrue("与改造前行为一致：ValidateServerCertificate=false 仍可完全关闭校验");
    }

    // ────────────────────────────────────────────────────────────────────
    // 边界：非 wss 不安装回调
    // ────────────────────────────────────────────────────────────────────

    [Fact]
    public void NonWssScheme_ShouldNotInstallCallback()
    {
        var manager = new WebSocketConnectionManager(
            NullLogger<WebSocketConnectionManager>.Instance,
            OptionsWith(CertificateValidationMode.Dev),
            NullLoggerFactory.Instance);

        using var socket = new ClientWebSocket();
        var method = typeof(WebSocketConnectionManager).GetMethod(
            "ConfigureCertificateValidation",
            BindingFlags.Instance | BindingFlags.NonPublic)!;

        method.Invoke(manager, [socket, new Uri("ws://open.feishu.cn/ws")]);

        socket.Options.RemoteCertificateValidationCallback.Should().BeNull(
            "仅 wss 需要配置证书校验（保持既有行为）");
    }
}
