// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.FeishuTools.Internal;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// R5 / F-6：卡片窄 DSL 编译器断言。
/// </summary>
/// <remarks>
/// 断言分两类：<b>① 编译正确</b>（产出与仓内已实战验证的卡片形态一致）与
/// <b>② 非法组合必须报错且附合法组合</b>（DoD 后半句）——后者是让模型能自我纠正的关键。
/// </remarks>
public class CardDslTests
{
    private static JsonArray Elements(JsonObject card)
        => card["zh_cn"]!["elements"]!.AsArray();

    /// <summary>四种元素都能编译，且<b>顺序与输入一致</b>。</summary>
    [Fact]
    public void Compile_ShouldRenderAllElementTypes_InDeclarationOrder()
    {
        var card = CardDsl.Compile(
            "T",
            "text:第一段\ndivider\nquote:引用内容\ncode:print(1)",
            null);

        var elements = Elements(card);
        elements.Should().HaveCount(4);

        elements[0]!["tag"]!.GetValue<string>().Should().Be("div", "text 用 div 承载");
        elements[0]!["text"]!["tag"]!.GetValue<string>().Should().Be("lark_md");
        elements[0]!["text"]!["content"]!.GetValue<string>().Should().Be("第一段");

        elements[1]!["tag"]!.GetValue<string>().Should().Be("hr", "divider 编译为 hr");

        // quote/code 借 lark_md 语法表达（不臆造未经本仓验证的新 tag）。
        elements[2]!["text"]!["content"]!.GetValue<string>().Should().StartWith("> ");
        elements[3]!["text"]!["content"]!.GetValue<string>().Should().StartWith("```");
    }

    /// <summary>标题非空时输出 title；标题为空时<b>不输出</b>（避免空标题占位）。</summary>
    [Theory]
    [InlineData("标题", true)]
    [InlineData("   ", false)]
    public void Compile_ShouldIncludeTitleOnlyWhenNonEmpty(string title, bool expected)
    {
        var card = CardDsl.Compile(title, "text:x", null);

        card["zh_cn"]!.AsObject().ContainsKey("title").Should().Be(expected);
    }

    /// <summary>url 按钮：产出 <c>url</c> 字段。</summary>
    [Fact]
    public void Compile_ShouldSupportUrlButton()
    {
        var card = CardDsl.Compile("T", "text:x", "查看|url|https://feishu.cn/t/1");

        var action = Elements(card)[^1]!;
        action["tag"]!.GetValue<string>().Should().Be("action");

        var button = action["actions"]!.AsArray()[0]!;
        button["tag"]!.GetValue<string>().Should().Be("button");
        button["text"]!["content"]!.GetValue<string>().Should().Be("查看");
        button["url"]!.GetValue<string>().Should().Be("https://feishu.cn/t/1");
        button["type"]!.GetValue<string>().Should().Be("primary", "首个按钮为主色");
    }

    /// <summary>value 按钮：产出 <c>value.key</c>（回调给机器人）；第二个起为 default 色。</summary>
    [Fact]
    public void Compile_ShouldSupportValueButton_AndSecondaryButtonsDefault()
    {
        var card = CardDsl.Compile("T", "text:x", "已处理|value|done\n忽略|value|skip");

        var buttons = Elements(card)[^1]!["actions"]!.AsArray();
        buttons[0]!["value"]!["key"]!.GetValue<string>().Should().Be("done");
        buttons[0]!["type"]!.GetValue<string>().Should().Be("primary");
        buttons[1]!["type"]!.GetValue<string>().Should().Be("default");
    }

    /// <summary>无按钮时不输出空的 action 元素（平台会因空 actions 报错）。</summary>
    [Fact]
    public void Compile_ShouldOmitActionElement_WhenNoButtons()
    {
        var card = CardDsl.Compile("T", "text:x", null);

        Elements(card).Should().HaveCount(1);
        Elements(card)[0]!["tag"]!.GetValue<string>().Should().Be("div");
    }

    /// <summary>
    /// <b>DoD 后半句</b>：每种非法组合都必须报错，且消息里<b>带合法组合</b>（否则模型无从纠正）。
    /// </summary>
    [Theory]
    [InlineData("unknown:内容", "未知")]
    [InlineData("没有类型前缀", "类型前缀")]
    [InlineData("text|url|https://x", "合法组合")]
    public void Compile_ShouldRejectIllegalBody_WithLegalForms(string body, string expectedFragment)
    {
        var act = () => CardDsl.Compile("T", body, null);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedFragment}*", "非法 DSL 必须指明原因")
            .Which.Message.Should().Contain(
                CardDsl.LegalForms, "错误消息必须附合法组合，否则模型无法自我纠正（F-8）");
    }

    /// <summary>按钮形态错误同样必须报错并附合法组合。</summary>
    [Theory]
    [InlineData("只有文本", "三段")]
    [InlineData("文本|goto|x", "回调类型")]
    [InlineData("|url|https://x", "不能为空")]
    [InlineData("文本|url|", "不能为空")]
    public void Compile_ShouldRejectIllegalButtons_WithLegalForms(string buttons, string expectedFragment)
    {
        var act = () => CardDsl.Compile("T", "text:x", buttons);

        act.Should().Throw<ArgumentException>()
            .WithMessage($"*{expectedFragment}*")
            .Which.Message.Should().Contain(CardDsl.LegalForms);
    }

    /// <summary>空 body 必须被拒（产出零元素的卡片没有意义）。</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Compile_ShouldRejectEmptyBody(string body)
    {
        var act = () => CardDsl.Compile("T", body, null);

        act.Should().Throw<ArgumentException>().WithMessage("*至少要有一个元素*");
    }

    /// <summary>编译产物<b>可序列化</b>（这是它要作为消息 content 下发的前提）。</summary>
    [Fact]
    public void Compile_ShouldProduceSerializableContent()
    {
        var json = CardDsl.Compile("T", "text:x", "查看|url|https://x").ToJsonString();

        json.Should().StartWith("{").And.EndWith("}");
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("zh_cn").GetProperty("elements").GetArrayLength().Should().Be(2);
    }
}
