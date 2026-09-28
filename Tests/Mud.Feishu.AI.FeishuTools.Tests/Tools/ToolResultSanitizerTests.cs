// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 出站净化单元测试（方案 §5 AT-B08 / §8.2 R4 的正反两面）。
/// </summary>
/// <remarks>
/// 用例分两类：<b>必须脱敏</b>（凭据/手机号/控制字符）与<b>必须保留</b>
/// （邮箱、各类 <c>*_id</c>/<c>*_token</c> 标识、JSON 结构）——后者是"脱敏误伤业务"的回归面。
/// </remarks>
public class ToolResultSanitizerTests
{
    [Fact]
    public void Sanitize_ShouldMaskCredentialValues()
    {
        var sanitized = ToolResultSanitizer.Sanitize(
            "{\"app_secret\":\"s3cr3t\",\"access_token\":\"t-123\",\"password\":\"p@ss\"}");

        sanitized.Should().NotContain("s3cr3t").And.NotContain("t-123").And.NotContain("p@ss");
        sanitized.Should().Contain("app_secret").And.Contain("access_token").And.Contain("password");
        sanitized.Should().Contain(ToolResultSanitizer.Mask);
    }

    [Fact]
    public void Sanitize_ShouldMaskChinaMobile()
    {
        var sanitized = ToolResultSanitizer.Sanitize("联系电话 13800138000，备用 15912345678。");

        sanitized.Should().NotContain("13800138000").And.NotContain("15912345678");
        sanitized.Should().Contain("联系电话");
    }

    /// <summary>
    /// 边界断言：手机号规则不得命中更长数字串（如订单号/时间戳内含的 11 位数字）。
    /// </summary>
    [Fact]
    public void Sanitize_ShouldNotMaskLongerDigitRuns()
    {
        var sanitized = ToolResultSanitizer.Sanitize("{\"order_no\":\"2026138001380009\"}");

        sanitized.Should().Contain("2026138001380009", "11 位片段嵌在更长数字串中，不得脱敏");
    }

    /// <summary>
    /// 邮箱是平台的寻址货币（<c>im.send_message</c> 的 <c>receive_id_type=email</c>），必须保留。
    /// </summary>
    [Fact]
    public void Sanitize_ShouldPreserveEmailAddresses()
    {
        var sanitized = ToolResultSanitizer.Sanitize("{\"email\":\"zhangsan@example.com\"}");

        sanitized.Should().Contain("zhangsan@example.com");
    }

    /// <summary>
    /// 标识类字段（形如 <c>*_token</c>/<c>*_id</c>）是多步调用链的必需凭据，**不得**被通配脱敏。
    /// </summary>
    [Fact]
    public void Sanitize_ShouldPreserveIdentifierFields()
    {
        var json = "{\"app_token\":\"bascnXxx\",\"page_token\":\"pt_1\",\"open_id\":\"ou_1\","
            + "\"chat_id\":\"oc_1\",\"document_id\":\"doxcn1\",\"node_token\":\"wikcn1\"}";

        var sanitized = ToolResultSanitizer.Sanitize(json);

        sanitized.Should().Be(json, "标识类字段一律原样保留（否则多步工具链断裂）");
    }

    [Fact]
    public void Sanitize_ShouldStripAnsiEscapeSequences()
    {
        var sanitized = ToolResultSanitizer.Sanitize("正常\u001b[31m红色\u001b[0m文本");

        sanitized.Should().Be("正常红色文本");
    }

    [Fact]
    public void Sanitize_ShouldStripControlCharacters_ButKeepWhitespace()
    {
        var sanitized = ToolResultSanitizer.Sanitize("a\u0000b\u0007c\nD\te\r\nf");

        sanitized.Should().Be("abc\nD\te\r\nf", "换行/制表保留（JSON 与可读性），其余控制字符剥离");
    }

    /// <summary>净化必须保持 JSON 合法（模型侧解析依赖结构完整性）。</summary>
    [Fact]
    public void Sanitize_ShouldPreserveJsonValidity()
    {
        var json = "{\"items\":[{\"mobile\":\"13800138000\",\"app_secret\":\"x\"}],\"has_more\":false}";

        var sanitized = ToolResultSanitizer.Sanitize(json);
        var act = () => JsonDocument.Parse(sanitized);

        act.Should().NotThrow("净化不得破坏 JSON 结构");
        sanitized.Should().Contain("has_more", "非敏感字段原样保留");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Sanitize_ShouldReturnEmptyForEmptyInput(string? input)
        => ToolResultSanitizer.Sanitize(input).Should().BeEmpty();
}
