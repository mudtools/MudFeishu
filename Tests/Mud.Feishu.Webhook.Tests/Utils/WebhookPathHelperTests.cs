// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Tests.Utils;

/// <summary>
/// W-04：路由前缀归一化与 AppKey 提取的回归锁。
/// </summary>
/// <remarks>
/// 历史缺陷：配置写成 <c>"/feishu"</c> 会拼出 <c>"//feishu"</c>，写成 <c>"feishu/"</c>
/// 会在前缀后直接比较 AppKey 段——两者都让端点**静默失效**（全量 404，无任何日志线索）。
/// 归一化因此上收到 <c>PostConfigure</c>，本组用例锁定"任意写法都能路由"。
/// </remarks>
public class WebhookPathHelperTests
{
    [Theory]
    [InlineData("feishu")]
    [InlineData("/feishu")]
    [InlineData("feishu/")]
    [InlineData(" /feishu/ ")]
    [InlineData("//feishu//")]
    public void ExtractAppKey_ShouldRoute_RegardlessOfPrefixSlashes(string configuredPrefix)
    {
        var appKey = WebhookPathHelper.ExtractAppKeyFromPath("/feishu/app1", configuredPrefix);

        appKey.Should().Be("app1", $"前缀写法「{configuredPrefix}」归一化后必须能路由");
    }

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("  ", "")]
    [InlineData("/feishu", "feishu")]
    [InlineData("feishu/", "feishu")]
    [InlineData(" /feishu/ ", "feishu")]
    [InlineData("feishu", "feishu")]
    public void NormalizePrefix_ShouldTrimWhitespaceAndSlashes(string? input, string expected)
    {
        WebhookPathHelper.NormalizePrefix(input).Should().Be(expected);
    }

    [Fact]
    public void NormalizePrefix_ShouldBeIdempotent()
    {
        // PostConfigure 会在每次 Options 重建（含热更）时执行，必须幂等
        var once = WebhookPathHelper.NormalizePrefix("/feishu/");
        WebhookPathHelper.NormalizePrefix(once).Should().Be(once);
    }

    [Theory]
    [InlineData("/feishu/app1", "feishu", "app1")]
    [InlineData("/FEISHU/app1", "feishu", "app1")]      // 前缀大小写不敏感
    [InlineData("/feishu/App1", "feishu", "App1")]      // AppKey 段大小写敏感
    [InlineData("/feishu/app1/events", "feishu", "app1")]
    [InlineData("/feishu", "feishu", null)]             // 只有前缀
    [InlineData("/feishu/", "feishu", null)]            // 前缀 + 空段
    [InlineData("/other/app1", "feishu", null)]         // 不同前缀
    [InlineData("/feishu/app key", "feishu", null)]     // 非法字符
    [InlineData("", "feishu", null)]
    public void ExtractAppKey_ShouldMatchExpected(string path, string prefix, string? expected)
    {
        WebhookPathHelper.ExtractAppKeyFromPath(path, prefix).Should().Be(expected);
    }
}
