// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Extensions;
using Mud.Feishu.WebSocket;

namespace Mud.Feishu.WebSocket.Tests.Configuration;

/// <summary>
/// C3/R3：WebSocket 证书 Mode 与 Reconnect 嵌套 + 安全默认。
/// </summary>
public class WebSocketCertificateModeOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeStrictAndSecure()
    {
        var options = new FeishuWebSocketOptions();
        options.Certificate.Mode.Should().Be(CertificateValidationMode.Strict);
        options.Certificate.AllowInsecureWebSocket.Should().BeFalse();
        options.Certificate.ValidateServerCertificate.Should().BeTrue();
        options.Certificate.AllowSelfSignedCertificates.Should().BeFalse();
        options.Reconnect.Auto.Should().BeTrue();
        options.Reconnect.MaxAttempts.Should().Be(5);
    }

    /// <summary>
    /// R5/X5：<c>Mode</c> 与两个细粒度布尔各自独立（Mode 不隐式改写配置值）。
    /// </summary>
    /// <remarks>
    /// 原 <c>FlatAndNested_ShouldShareStorage</c> 已删除：它是**恒真断言**（先赋
    /// <c>true</c> 再断言 <c>true</c>），且其名暗示的「扁平/嵌套共享存储」契约已随 R4 删除扁平属性而消失，
    /// 属假安全网（见 .docs/配置面可用性修复与收敛方案-R5.md §0.5.4）。
    /// 运行时行为改由 <c>WebSocketCertificateModeRuntimeTests</c> 按优先级矩阵逐格锁定。
    /// </remarks>
    [Fact]
    public void Mode_ShouldNotImplicitlyMutateFineGrainedBooleans()
    {
        var options = new FeishuWebSocketOptions
        {
            Certificate = new WebSocketCertificateOptions { Mode = CertificateValidationMode.Dev }
        };

        options.Certificate.AllowSelfSignedCertificates.Should().BeFalse(
            "Mode 与细粒度布尔相互独立：Dev 的放宽由运行时回调实现，不改写配置值");
        options.Certificate.AllowCertificateNameMismatch.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenStrictPlusSelfSigned()
    {
        var options = new FeishuWebSocketOptions
        {
            Certificate = new WebSocketCertificateOptions
            {
                Mode = CertificateValidationMode.Strict,
                AllowSelfSignedCertificates = true
            }
        };

        var act = () => options.ValidateCertificateOptions();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Strict*");
    }

    [Fact]
    public void Validate_ShouldFail_WhenCustomModeWithoutCallback()
    {
        var options = new FeishuWebSocketOptions
        {
            Certificate = new WebSocketCertificateOptions
            {
                Mode = CertificateValidationMode.Custom
            }
        };

        var act = () => options.ValidateCertificateOptions();
        act.Should().Throw<InvalidOperationException>().WithMessage("*Custom*");
    }

    [Fact]
    public void Validate_ShouldPass_WhenDevAllowsSelfSigned()
    {
        var options = new FeishuWebSocketOptions
        {
            Certificate = new WebSocketCertificateOptions
            {
                Mode = CertificateValidationMode.Dev,
                AllowSelfSignedCertificates = true
            }
        };

        var act = () => options.ValidateCertificateOptions();
        act.Should().NotThrow();
    }

    [Fact]
    public void Bind_ShouldMapNestedCertificateAndReconnect()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebSocket:Certificate:Mode"] = "Dev",
                ["FeishuWebSocket:Certificate:AllowSelfSignedCertificates"] = "true",
                ["FeishuWebSocket:Reconnect:MaxAttempts"] = "9",
                ["FeishuWebSocket:Reconnect:BaseDelayMs"] = "2000"
            })
            .Build();

        var options = new FeishuWebSocketOptions();
        configuration.GetSection("FeishuWebSocket").Bind(options);

        options.Certificate.Mode.Should().Be(CertificateValidationMode.Dev);
        options.Certificate.AllowSelfSignedCertificates.Should().BeTrue();
        options.Reconnect.MaxAttempts.Should().Be(9);
        options.Reconnect.BaseDelayMs.Should().Be(2000);
    }
}
