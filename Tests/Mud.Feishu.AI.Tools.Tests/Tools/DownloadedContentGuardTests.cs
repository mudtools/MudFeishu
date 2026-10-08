// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// <b>R5 / B-2 + F-10</b>：字节型下载的"错误体伪装成文件"防线（<see cref="DownloadedContentGuard"/>）。
/// </summary>
/// <remarks>
/// <para>
/// 断言的落点全部是"<b>会静默出错</b>"的地方：漏判 ⇒ 磁盘上多一个内容是错误 JSON 的假文件
/// （调用方要等到读它时才发现）；误判 ⇒ 合法下载被拒。
/// </para>
/// <para>
/// 因此本用例集**双向**断言：既要认出真错误体，也要放过合法 JSON 文件与二进制 ——
/// 只有单向断言（"能认出错误体"）无法发现"把所有 JSON 都拒掉"这种过度拦截。
/// </para>
/// </remarks>
public class DownloadedContentGuardTests
{
    private static byte[] Bytes(string text) => Encoding.UTF8.GetBytes(text);

    // ────────── 必须识别：平台错误体 ──────────

    /// <summary>典型错误信封（HTTP 200 + 非 0 code）必须被识别。</summary>
    [Theory]
    [InlineData("""{"code":1770001,"msg":"file not found"}""")]
    [InlineData("""{"code":99991663,"msg":"tenant access token invalid"}""")]
    [InlineData("""{"msg":"err","code":1}""")]                       // 字段顺序无关
    [InlineData("""{"code":"1770001","msg":"string code"}""")]       // 数值字符串形态
    [InlineData("""{"code":-1,"msg":"negative"}""")]
    public void LooksLikeJsonErrorBody_ShouldDetectNonNullCode(string body)
    {
        DownloadedContentGuard.LooksLikeJsonErrorBody(Bytes(body), out var code, out _)
            .Should().BeTrue("HTTP 200 + 非 0 code 就是 SDK 注释里警告的『错误体伪装成文件』");

        code.Should().NotBe(0, "识别出的 code 必须回填给调用方，供结构化错误使用");
    }

    /// <summary>BOM 与前导空白不得影响识别（真实响应常带 BOM）。</summary>
    [Fact]
    public void LooksLikeJsonErrorBody_ShouldIgnoreBomAndLeadingWhitespace()
    {
        byte[] content = [0xEF, 0xBB, 0xBF, (byte)'\n', (byte)' ', .. Bytes("""{"code":5,"msg":"x"}""")];

        DownloadedContentGuard.LooksLikeJsonErrorBody(content, out var code, out var message)
            .Should().BeTrue("BOM/空白是传输层噪声，不该让防线失效");

        code.Should().Be(5);
        message.Should().Be("x");
    }

    /// <summary>错误消息必须被回填（否则调用方只有 code 无法解释给模型）。</summary>
    [Fact]
    public void LooksLikeJsonErrorBody_ShouldReturnMessage()
    {
        DownloadedContentGuard.LooksLikeJsonErrorBody(
            Bytes("""{"code":2,"msg":"invalid param"}"""), out _, out var message);

        message.Should().Be("invalid param");
    }

    // ────────── 必须放过：合法内容（否则防线会误杀真实下载） ──────────

    /// <summary>
    /// <b>合法 JSON 文件不得被拒</b>：这正是"判据不是『是不是 JSON』"的原因。
    /// </summary>
    [Theory]
    [InlineData("""{"name":"config","version":2}""")]            // 无 code
    [InlineData("""{"code":0,"msg":"ok","data":{"a":1}}""")]     // code == 0（成功信封形状）
    [InlineData("""{"pages":[{"code":7}]}""")]                   // 顶层是数组
    [InlineData("""[{"code":1}]""")]                             // 顶层数组（非对象）
    public void LooksLikeJsonErrorBody_ShouldNotDetectLegitimateJson(string text)
    {
        DownloadedContentGuard.LooksLikeJsonErrorBody(Bytes(text), out var code, out _)
            .Should().BeFalse("合法 JSON 文件被误杀会让『下载 JSON』这类正常需求全部失败");

        code.Should().Be(0);
    }

    /// <summary><b>嵌套的 code 不算信封字段</b> —— 否则深层业务 JSON 会被误判为错误体。</summary>
    [Fact]
    public void LooksLikeJsonErrorBody_ShouldNotInspectNestedCode()
    {
        DownloadedContentGuard.LooksLikeJsonErrorBody(
            Bytes("""{"code":0,"data":{"results":[{"code":404,"msg":"inner"}]}}"""), out var code, out _)
            .Should().BeFalse("只有**顶层** code 才表征平台错误；子对象的 code 是业务字段");
    }

    /// <summary>二进制内容（图片/压缩包）必须原样放过。</summary>
    [Fact]
    public void LooksLikeJsonErrorBody_ShouldNotDetectBinary()
    {
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00 };

        DownloadedContentGuard.LooksLikeJsonErrorBody(png, out _, out _).Should().BeFalse();
    }

    /// <summary>空内容与非法 JSON 不得抛异常（防线自身不能成为新的故障点）。</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("""{"code":123""")]      // 截断
    [InlineData("not json at all")]
    public void LooksLikeJsonErrorBody_ShouldNotThrowOnMalformedInput(string text)
    {
        var act = () => DownloadedContentGuard.LooksLikeJsonErrorBody(Bytes(text), out _, out _);

        act.Should().NotThrow("防线若自己抛异常，就会把『下载失败』变成『工具崩溃』");
    }

    // ────────── 放行判定：豁免语义 ──────────

    /// <summary>默认（未声明期望类型）时，错误体必须被拒且给出可操作的原因。</summary>
    [Fact]
    public void ShouldReject_ShouldRejectErrorBodyByDefault()
    {
        var rejected = DownloadedContentGuard.ShouldRejectForJsonErrorBody(
            Bytes("""{"code":1770001,"msg":"not found"}"""), expectedContentType: null, out var reason);

        rejected.Should().BeTrue();
        reason.Should().Contain("1770001", "原因里必须带平台错误码");
        reason.Should().Contain("not found");
        reason.Should().Contain("expected_content_type=application/json",
            "必须告诉调用方『若确实要下载 JSON 文件』该如何显式豁免");
    }

    /// <summary>
    /// <b>显式豁免</b>：调用方声明"这次要的就是 JSON"时放行 —— 但这是**声明**而非静默绕过。
    /// </summary>
    [Theory]
    [InlineData("application/json")]
    [InlineData("application/json; charset=utf-8")]
    [InlineData("APPLICATION/JSON")]
    [InlineData("application/vnd.api+json")]
    public void ShouldReject_ShouldHonorExplicitJsonContentType(string contentType)
    {
        var rejected = DownloadedContentGuard.ShouldRejectForJsonErrorBody(
            Bytes("""{"code":1770001,"msg":"x"}"""), contentType, out var reason);

        rejected.Should().BeFalse("调用方已声明期望类型为 JSON，应按其意图放行");
        reason.Should().BeNull();
    }

    /// <summary>非 JSON 的期望类型不构成豁免（仍须拒错误体）。</summary>
    [Theory]
    [InlineData("image/png")]
    [InlineData("application/pdf")]
    [InlineData("")]
    [InlineData("   ")]
    public void ShouldReject_ShouldNotExemptNonJsonContentType(string? contentType)
    {
        DownloadedContentGuard.ShouldRejectForJsonErrorBody(
            Bytes("""{"code":9,"msg":"boom"}"""), contentType, out var reason)
            .Should().BeTrue("只有 JSON 系声明才豁免；其它期望类型下错误体必须被拦");

        reason.Should().NotBeNull();
    }

    /// <summary>正常文件（非错误体）永不因本判据被拒 —— 即使期望类型是 JSON。</summary>
    [Fact]
    public void ShouldReject_ShouldNotRejectNormalContent()
    {
        DownloadedContentGuard.ShouldRejectForJsonErrorBody(
            Bytes("PK\u0003\u0004binary-zip"), expectedContentType: null, out var reason)
            .Should().BeFalse();

        reason.Should().BeNull();
    }
}
