// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Tests.Utils;

/// <summary>
/// IpAddressHelper 单元测试
/// </summary>
public class IpAddressHelperTests
{
    #region IsIpAllowed Tests

    [Fact]
    public void IsIpAllowed_WithNullIp_ShouldReturnFalse()
    {
        // Arrange
        var allowedIps = new HashSet<string> { "192.168.1.1" };

        // Act
        var result = IpAddressHelper.IsIpAllowed(null, allowedIps);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithEmptyIp_ShouldReturnFalse()
    {
        // Arrange
        var allowedIps = new HashSet<string> { "192.168.1.1" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("", allowedIps);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithNullAllowedIps_ShouldReturnFalse()
    {
        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.1", null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithEmptyAllowedIps_ShouldReturnFalse()
    {
        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.1", new HashSet<string>());

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithExactMatch_ShouldReturnTrue()
    {
        // Arrange
        var allowedIps = new HashSet<string> { "192.168.1.1", "10.0.0.1" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.1", allowedIps);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpAllowed_WithNoMatch_ShouldReturnFalse()
    {
        // Arrange
        var allowedIps = new HashSet<string> { "192.168.1.1", "10.0.0.1" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.100", allowedIps);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithCidrRange_ShouldReturnTrue()
    {
        // Arrange
        var allowedIps = new HashSet<string> { "192.168.1.0/24" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.100", allowedIps);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpAllowed_WithCidrRangeOutside_ShouldReturnFalse()
    {
        // Arrange
        var allowedIps = new HashSet<string> { "192.168.1.0/24" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.2.1", allowedIps);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithInvalidIp_ShouldReturnFalse()
    {
        // Arrange
        var allowedIps = new HashSet<string> { "192.168.1.1" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("not-an-ip", allowedIps);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpAllowed_WithIpv4MappedExactRule_ShouldReturnTrue()
    {
        // Arrange - WHF-18：Kestrel 常见的 IPv4-mapped IPv6 形态不应因地址族不同被误拒
        var allowedIps = new HashSet<string> { "192.168.1.1" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("::ffff:192.168.1.1", allowedIps);

        // Assert
        result.Should().BeTrue("IPv4-mapped IPv6 应归一为 IPv4 后匹配");
    }

    [Fact]
    public void IsIpAllowed_WithIpv4MappedCidrRule_ShouldReturnTrue()
    {
        // Arrange - WHF-18：mapped IPv6 对 CIDR 规则
        var allowedIps = new HashSet<string> { "192.168.1.0/24" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("::ffff:192.168.1.5", allowedIps);

        // Assert
        result.Should().BeTrue("IPv4-mapped IPv6 应归一后命中 CIDR");
    }

    [Fact]
    public void IsIpAllowed_WithMappedRuleAndPlainIp_ShouldReturnTrue()
    {
        // Arrange - WHF-18：规则侧为 mapped IPv6、被检侧为 IPv4
        var allowedIps = new HashSet<string> { "::ffff:192.168.1.1" };

        // Act
        var result = IpAddressHelper.IsIpAllowed("192.168.1.1", allowedIps);

        // Assert
        result.Should().BeTrue("规则侧 mapped IPv6 应归一后匹配");
    }

    [Fact]
    public void IsIpInRange_WithNegativePrefix_ShouldReturnFalse()
    {
        // Arrange - WHF-18 安全护栏：负数前缀曾生成全零掩码导致 CIDR 恒匹配（白名单失效）
        var ip = IPAddress.Parse("8.8.8.8");

        // Act
        var result = IpAddressHelper.IsIpInRange(ip, "10.0.0.0/-5");

        // Assert
        result.Should().BeFalse("负数前缀必须拒绝，不能退化为『匹配一切』");
    }

    [Fact]
    public void IsIpInRange_WithPrefixExceedingAddressBits_ShouldReturnFalse()
    {
        // Arrange - WHF-18：前缀超过地址位宽（IPv4 最大 32）
        var ip = IPAddress.Parse("10.0.0.1");

        // Act
        var result = IpAddressHelper.IsIpInRange(ip, "10.0.0.0/33");

        // Assert
        result.Should().BeFalse("超出位宽的前缀必须拒绝");
    }

    [Fact]
    public void IsIpInRange_WithValidMaxPrefix_ShouldMatchOnlyExactAddress()
    {
        // Arrange - IPv4 /32 与 IPv6 /128 为合法上限边界
        var ip = IPAddress.Parse("10.0.0.1");
        var ipv6 = IPAddress.Parse("fe80::1");

        // Act & Assert
        IpAddressHelper.IsIpInRange(ip, "10.0.0.1/32").Should().BeTrue();
        IpAddressHelper.IsIpInRange(ip, "10.0.0.2/32").Should().BeFalse();
        IpAddressHelper.IsIpInRange(ipv6, "fe80::1/128").Should().BeTrue();
    }

    #endregion

    #region IsIpInRange Tests

    [Fact]
    public void IsIpInRange_WithExactMatch_ShouldReturnTrue()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInRange(ip, "192.168.1.1");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpInRange_WithNoMatch_ShouldReturnFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInRange(ip, "192.168.1.2");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpInRange_WithEmptyRule_ShouldReturnFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInRange(ip, "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpInRange_WithInvalidRule_ShouldReturnFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInRange(ip, "not-an-ip");

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region IsIpInCidrRange Tests

    [Fact]
    public void IsIpInCidrRange_WithValidCidr24_ShouldReturnTrue()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.100");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "192.168.1.0/24");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpInCidrRange_WithValidCidr16_ShouldReturnTrue()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.100.50");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "192.168.0.0/16");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpInCidrRange_OutsideCidrRange_ShouldReturnFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.2.1");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "192.168.1.0/24");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpInCidrRange_WithEmptyCidr_ShouldReturnFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpInCidrRange_WithInvalidCidrFormat_ShouldReturnFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "192.168.1.0/33");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpInCidrRange_WithInvalidNetworkAddress_ShouldReturnFalse()
    {
        // Arrange
        var ip = IPAddress.Parse("192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "not-valid/24");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsIpInCidrRange_WithIPv6Cidr_ShouldReturnTrue()
    {
        // Arrange
        var ip = IPAddress.Parse("2001:db8::1");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "2001:db8::/32");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsIpInCidrRange_WithIPv4MappedToIPv6_ShouldMatchAfterNormalization()
    {
        // Arrange - WHF-18：IPv4-mapped IPv6 归一后应命中 IPv4 CIDR（原实现因地址族不同误拒）
        var ip = IPAddress.Parse("::ffff:192.168.1.1");

        // Act
        var result = IpAddressHelper.IsIpInCidrRange(ip, "192.168.1.0/24");

        // Assert
        result.Should().BeTrue("IPv4-mapped IPv6 应归一为 IPv4 后参与 CIDR 比较");
    }

    #endregion

    #region ParseIpAddress Tests

    [Fact]
    public void ParseIpAddress_WithValidIPv4_ShouldReturnIpAddress()
    {
        // Act
        var result = IpAddressHelper.ParseIpAddress("192.168.1.1");

        // Assert
        result.Should().NotBeNull();
        result!.ToString().Should().Be("192.168.1.1");
    }

    [Fact]
    public void ParseIpAddress_WithValidIPv6_ShouldReturnIpAddress()
    {
        // Act
        var result = IpAddressHelper.ParseIpAddress("2001:db8::1");

        // Assert
        result.Should().NotBeNull();
    }

    [Fact]
    public void ParseIpAddress_WithNullString_ShouldReturnNull()
    {
        // Act
        var result = IpAddressHelper.ParseIpAddress(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseIpAddress_WithEmptyString_ShouldReturnNull()
    {
        // Act
        var result = IpAddressHelper.ParseIpAddress("");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseIpAddress_WithInvalidString_ShouldReturnNull()
    {
        // Act
        var result = IpAddressHelper.ParseIpAddress("not-an-ip");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region NormalizeIpAddress Tests

    [Fact]
    public void NormalizeIpAddress_WithValidIp_ShouldReturnNormalizedString()
    {
        // Act
        var result = IpAddressHelper.NormalizeIpAddress("192.168.1.1");

        // Assert
        result.Should().Be("192.168.1.1");
    }

    [Fact]
    public void NormalizeIpAddress_WithInvalidIp_ShouldReturnOriginalString()
    {
        // Act
        var result = IpAddressHelper.NormalizeIpAddress("not-an-ip");

        // Assert
        result.Should().Be("not-an-ip");
    }

    [Fact]
    public void NormalizeIpAddress_WithNull_ShouldReturnNull()
    {
        // Act
        var result = IpAddressHelper.NormalizeIpAddress(null!);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
