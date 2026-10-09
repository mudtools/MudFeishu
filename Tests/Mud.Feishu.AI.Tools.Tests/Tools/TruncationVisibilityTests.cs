// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// B4 截断标记回填测试（方案 §3.B4）：超长 JSON → 标记 + 合法 JSON；纯文本 → 标记；未超长 → 零标记。
/// </summary>
/// <remarks>
/// <para>
/// <b>为何不直接调 <c>ToolExecutor.FromApi</c></b>：<c>ToolExecutor</c> 是 internal struct，
/// 且其输入 <c>FeishuApiOutcome&lt;T&gt;</c> 的构造需要真实 API 返回——
/// 测试退化为"直接调 <c>TruncateJson</c>/<c>Truncate</c> + <c>IsTruncated</c> 判定逻辑"的等价断言，
/// 与守卫 <c>TruncationVisibilityContractGuards</c> 互补（守卫锁源码结构，测试锁行为）。
/// </para>
/// </remarks>
public class TruncationVisibilityTests
{
    /// <summary>
    /// 超长 JSON 经 <c>TruncateJson</c> 后必须：
    /// ① 结果比原文短；② 仍是合法 JSON（可被 <c>JsonDocument.Parse</c> 解析）。
    /// </summary>
    [Fact]
    public void TruncateJson_LongJson_ShouldProduceValidJsonAndShorterText()
    {
        // 构造超长 JSON：包含一个 items 数组，每项有较长的 text 字段。
        var items = new JsonObject();
        var array = new JsonArray();
        for (var i = 0; i < 100; i++)
        {
            array.Add(new JsonObject
            {
                ["id"] = $"item-{i}",
                ["text"] = new string('x', 200),
            });
        }
        items["items"] = array;

        var fullText = ToolResultJson.ToText(items);
        fullText.Length.Should().BeGreaterThan(1000, "构造的 JSON 应足够长以触发截断");

        const int maxLength = 500;
        var truncated = ToolResultText.TruncateJson(fullText, maxLength);

        // ① 截断后比原文短
        truncated.Length.Should().BeLessThan(fullText.Length,
            "截断后文本必须比原文短——否则截断未生效");
        // ② 截断后文本不超过 maxLength 太多（允许标记开销）
        truncated.Length.Should().BeLessThanOrEqualTo(maxLength + 200,
            "截断后文本应在 maxLength 附近（允许截断标记开销）");

        // ③ 仍是合法 JSON
        var act = () => System.Text.Json.JsonDocument.Parse(truncated);
        act.Should().NotThrow("截断后的 JSON 必须保持结构合法——字节级截断会让模型拿到非法 JSON");
    }

    /// <summary>
    /// 未超长的 JSON 经 <c>TruncateJson</c> 后：原文不变，截断标记为 false。
    /// </summary>
    [Fact]
    public void TruncateJson_ShortJson_ShouldNotTruncate()
    {
        var json = ToolResultJson.ToText(new JsonObject
        {
            ["name"] = "test",
            ["count"] = 42,
        });

        var result = ToolResultText.TruncateJson(json, 1000);

        result.Should().Be(json, "短文本不应被截断");
        // 长度比较判定：截断后长度 == 原文长度 → 未截断
        (result.Length < json.Length).Should().BeFalse("短文本截断标记应为 false");
    }

    /// <summary>
    /// 超长纯文本经 <c>Truncate</c> 后：结果比原文短，且包含截断标记。
    /// </summary>
    [Fact]
    public void Truncate_LongText_ShouldBeShorterAndMarked()
    {
        var fullText = new string('a', 5000);

        var result = ToolResultText.Truncate(fullText, 1000);

        result.Length.Should().BeLessThan(fullText.Length, "截断后必须比原文短");
        result.Should().Contain("[truncated", "纯文本截断必须包含截断标记");
        // 长度比较判定：截断 → true
        (result.Length < fullText.Length).Should().BeTrue("长度比较应检测到截断");
    }

    /// <summary>
    /// 未超长纯文本经 <c>Truncate</c> 后：原文不变。
    /// </summary>
    [Fact]
    public void Truncate_ShortText_ShouldNotTruncate()
    {
        const string shortText = "hello world";

        var result = ToolResultText.Truncate(shortText, 1000);

        result.Should().Be(shortText, "短文本不应被截断");
        (result.Length < shortText.Length).Should().BeFalse("短文本截断标记应为 false");
    }

    /// <summary>
    /// FeishuToolResult.FromText 的 truncated 参数必须正确传入：
    /// 截断时 Truncated=true 且 TruncationReason 非空。
    /// </summary>
    [Fact]
    public void FromText_WithTruncated_ShouldBackfillMarker()
    {
        var result = FeishuToolResult.FromText("some text", truncated: true, truncationReason: "JSON 感知截断");

        result.Truncated.Should().BeTrue("FromText 必须正确回填 Truncated 标记");
        result.TruncationReason.Should().Be("JSON 感知截断");
    }

    /// <summary>
    /// FeishuToolResult.FromText 默认不截断。
    /// </summary>
    [Fact]
    public void FromText_WithoutTruncated_ShouldDefaultToFalse()
    {
        var result = FeishuToolResult.FromText("some text");

        result.Truncated.Should().BeFalse("默认未截断");
        result.TruncationReason.Should().BeNull();
    }
}
