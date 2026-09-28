// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 入站净化正反用例矩阵（AT-B19 / 不变量 A5）。
/// </summary>
/// <remarks>
/// <para>
/// <b>本矩阵的重点是"误伤面"</b>（方案 §10.2 R-2）：入站净化的风险不在于漏检，而在于<b>把正常内容判成攻击</b>
/// ——用户正文里的换行、Tab、Emoji（尤其含 ZWJ 的家庭/职业 emoji）一旦被拒，整条写链路就废了。
/// 故每个"该拒"的用例都配一个"该放"的对照用例。
/// </para>
/// <para>
/// <b>与官方 CLI 的有意偏离</b>：官方 <c>internal/charcheck/charcheck.go</c> 直接拒绝 <c>U+200D</c>，
/// 本仓不能照抄——<c>U+200D</c> 是 Emoji ZWJ 序列的合法组成。
/// </para>
/// </remarks>
public class ToolArgumentSanitizerTests
{
    // ───────────────────── 该拒的必须拒 ─────────────────────

    [Theory]
    [InlineData("text", "hello\u0001world", "NUL/SOH 类 C0 控制符")]
    [InlineData("text", "a\u001bb[31mRED", "ESC（ANSI 转义起始符）")]
    [InlineData("text", "a\u007fb", "DEL")]
    [InlineData("text", "a\u0085b", "C1 控制符（NEL）")]
    [InlineData("text", "a\u200bb", "孤立零宽空格 U+200B")]
    [InlineData("text", "a\u202eb", "Bidi 覆盖 U+202E（可用于视觉欺骗）")]
    [InlineData("text", "a\ufeffb", "BOM")]
    [InlineData("text", "a\u2028b", "行分隔符 U+2028")]
    public void ValidateText_ShouldReject_ForControlAndInvisibleCharacters(string name, string text, string why)
    {
        ToolArgumentSanitizer.ValidateText(name, text)
            .Should().NotBeNull($"{why} 对模型无信息价值，却可被用来伪造工具输出的视觉结构");
    }

    [Fact]
    public void ValidateText_ShouldReject_LoneCarriageReturn_ButAllowCrLf()
    {
        ToolArgumentSanitizer.ValidateText("text", "line1\r\nline2").Should().BeNull("CRLF 是 Windows 正常换行");
        ToolArgumentSanitizer.ValidateText("text", "line1\nline2").Should().BeNull("LF 是正常换行");

        var failure = ToolArgumentSanitizer.ValidateText("text", "a\rb");
        failure.Should().NotBeNull("独立 CR 是 HTTP 头注入（CRLF）的经典载体");
        failure!.Should().Contain("CR");
    }

    [Fact]
    public void ValidateText_ShouldReject_WhenOverLengthLimit()
    {
        ToolArgumentSanitizer.ValidateText("text", new string('a', ToolArgumentSanitizer.MaxArgumentValueLength)).Should().BeNull();
        ToolArgumentSanitizer.ValidateText("text", new string('a', ToolArgumentSanitizer.MaxArgumentValueLength + 1))
            .Should().NotBeNull("超长参数是把整篇文档塞进参数的病态输入，必须拒绝并让模型拆分");
    }

    // ───────────────────── 该放的必须放（误伤面） ─────────────────────

    [Fact]
    public void ValidateText_ShouldNotReject_NewlineTabAndChineseAndEmoji()
    {
        ToolArgumentSanitizer.ValidateText("text", "第一行\n第二行\t制表").Should().BeNull("换行/Tab 是 im.send_message 正文的合法内容");
        ToolArgumentSanitizer.ValidateText("text", "中文标点：，。！？【】（）").Should().BeNull();
        ToolArgumentSanitizer.ValidateText("text", "😀🚀✅").Should().BeNull("普通 Emoji");
    }

    [Fact]
    public void ValidateText_ShouldNotReject_EmojiZwjSequence()
    {
        // 👨‍👩‍👧 = U+1F468 + ZWJ(U+200D) + U+1F469 + ZWJ + U+1F467 —— ZWJ 在此是合法组成。
        // 若照抄官方"拒 U+200D"，这条最常见的家庭 emoji 会被判成攻击载荷。
        ToolArgumentSanitizer.ValidateText("text", "👨‍👩‍👧 一家").Should()
            .BeNull("Emoji ZWJ 序列中的 U+200D 是合法组成（不得照抄官方的无差别拒绝）");

        // 对照：孤立的 ZWJ（前后都是 ASCII）应被判为可疑不可见字符。
        ToolArgumentSanitizer.ValidateText("text", "abc\u200ddef").Should()
            .NotBeNull("孤立零宽连接符（非 Emoji 上下文）可用于隐藏文本内容");
    }

    // ───────────────────── 集合与整字典 ─────────────────────

    [Fact]
    public void Validate_ShouldWalkStringArrays_AndReportIndexedFailure()
    {
        var ok = new Dictionary<string, object?> { ["emails"] = new[] { "a@example.com", "b@example.com" } };
        ToolArgumentSanitizer.Validate(ok).Should().BeNull();

        var bad = new Dictionary<string, object?> { ["texts"] = new[] { "ok", "bad\u0001" } };
        var failure = ToolArgumentSanitizer.Validate(bad);
        failure.Should().NotBeNull();
        failure!.Should().Contain("texts[1]", "错误消息必须定位到具体元素（否则模型无从修正）");
    }

    [Fact]
    public void Validate_ShouldIgnoreNonTextValues()
    {
        var args = new Dictionary<string, object?>
        {
            ["count"] = 3,
            ["flag"] = true,
            ["nothing"] = null,
        };

        ToolArgumentSanitizer.Validate(args).Should().BeNull("非文本参数不携带可注入的不可见字符");
    }
}
