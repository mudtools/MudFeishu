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

    [Fact]
    public void FlatAndNested_ShouldShareStorage()
    {
#pragma warning disable CS0618
        var options = new FeishuWebSocketOptions();
        options.AutoReconnect = false;
        options.Reconnect.Auto.Should().BeFalse();
        options.AllowSelfSignedCertificates = true;
        options.Certificate.AllowSelfSignedCertificates.Should().BeTrue();
        options.Certificate.Mode = CertificateValidationMode.Dev;
        options.Certificate.AllowSelfSignedCertificates.Should().BeTrue();
#pragma warning restore CS0618
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
