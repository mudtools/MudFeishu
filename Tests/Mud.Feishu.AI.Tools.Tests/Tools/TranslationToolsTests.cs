// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.AI.Tools.Tools;
using Mud.Feishu.DataModels.AI;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// AI 文本面工具（R7 / C3：<c>ai.translate_text</c> / <c>ai.detect_language</c>）：
/// 请求映射、语言代码归一化、平台上限的<b>本地</b>拦截与闭集错误文案。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么断言请求体实参而不只看返回值</b>：本域的核心风险是"语言代码写法与平台口径不一致"
/// 与"超限文本被原样下发"——两者都可能返回看似成功的空结果/平台 1140101，只看返回值区分不出来。
/// </para>
/// <para>
/// <b>为什么超限与非法语言必须在本地拦</b>：平台对非法参数的响应是
/// <c>1140101 invalid param</c>（对模型零指向性）；本地拦截会回填<b>合法值清单</b>，
/// 模型据此能自我纠正（F-8 的 suggestions 等价物）。
/// </para>
/// </remarks>
public class TranslationToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static TranslationTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1AITranslation>? client = null)
        => new(Options.Create(AgentOptions()), client?.Object);

    private static Mock<Mud.Feishu.IFeishuTenantV1AITranslation> ClientCapturingTranslate(out Func<TranslateTextRequest?> captured)
    {
        TranslateTextRequest? request = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AITranslation>();
        client.Setup(c => c.TranslateTextAsync(It.IsAny<TranslateTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback<TranslateTextRequest, CancellationToken>((r, _) => request = r)
            .ReturnsAsync(new FeishuApiResult<TranslateTextResult>
            {
                Code = 0,
                Data = new TranslateTextResult { Text = "Try to use Lark" },
            });

        captured = () => request;
        return client;
    }

    // ───────────────────── 契约面：身份 / 读写 / scope ─────────────────────

    /// <summary>两个工具都是 tenant 身份只读，且 scope 恰为官方文档核实的 <c>translation:text</c>。</summary>
    [Theory]
    [InlineData("AiTranslateText")]
    [InlineData("AiDetectLanguage")]
    public void Tools_Should_Be_TenantReadonly_WithTranslationScope(string toolNameField)
    {
        var toolName = toolNameField == "AiTranslateText"
            ? FeishuToolNames.AiTranslateText
            : FeishuToolNames.AiDetectLanguage;

        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.Identity.Should().Be("tenant", "两个工具都映射 tenant 令牌客户端（IFeishuTenantV1AITranslation）");
        contract.IsWrite.Should().BeFalse("机器翻译与语种识别都是只读能力——不得进 WriteAllowList 键控");
        contract.RequiredScopes.Should().BeEquivalentTo(
            new[] { "translation:text" },
            "scope 取自官方《Translate text》/《Detect text》文档的「权限要求」栏（唯一真相源）");
    }

    // ───────────────────── translate：请求映射与归一化 ─────────────────────

    /// <summary>
    /// 语言代码<b>大小写不敏感</b>并归一化为平台口径——模型写 <c>EN</c> / <c>zh-hant</c> 也应命中。
    /// </summary>
    [Fact]
    public async Task TranslateText_Should_NormalizeLanguageCodes_And_PassRequestBody()
    {
        var client = ClientCapturingTranslate(out var captured);
        var tools = CreateTools(client);

        await tools.TranslateTextAsync(
            Args(
                ("text", "尝试使用一下飞书吧"),
                ("source_language", "ZH"),
                ("target_language", "zh-hant")),
            CancellationToken.None);

        var request = captured();
        request.Should().NotBeNull("翻译请求必须真的下发（否则本用例是假绿）");
        request!.Text.Should().Be("尝试使用一下飞书吧");
        request.SourceLanguage.Should().Be("zh", "源语言归一化为平台口径（小写 zh）");
        request.TargetLanguage.Should().Be("zh-Hant", "目标语言归一化为平台口径（zh-Hant 的规范大小写）");
        request.Glossaies.Should().BeNull("未给 glossary 时不得下发术语表字段");
    }

    /// <summary>投影：返回翻译文本 + 生效的语言对 + 术语表条数（模型据此确认术语表真的带上了）。</summary>
    [Fact]
    public async Task TranslateText_Should_Project_TextAndLanguagePair()
    {
        var client = ClientCapturingTranslate(out _);
        var tools = CreateTools(client);

        var result = await tools.TranslateTextAsync(
            Args(
                ("text", "飞书"),
                ("source_language", "zh"),
                ("target_language", "en"),
                ("glossary", """[{"from":"飞书","to":"Lark"}]""")),
            CancellationToken.None);

        result.Text.Should().NotContain("[tool_error]");
        result.Text.Should().Contain("Try to use Lark", "翻译结果必须回填模型");
        result.Text.Should().Contain("\"source_language\"").And.Contain("\"target_language\"");
        result.Text.Should().Contain("glossary_applied");
        result.Text.Should().Contain("1", "术语表 1 项生效——模型据此确认术语表被带上");
    }

    /// <summary>术语表条目落入 <c>glossary</c> 字段（平台 DTO 属性名为 <c>Glossaies</c>）。</summary>
    [Fact]
    public async Task TranslateText_Should_MapGlossaryEntries()
    {
        var client = ClientCapturingTranslate(out var captured);
        var tools = CreateTools(client);

        await tools.TranslateTextAsync(
            Args(
                ("text", "飞书"),
                ("source_language", "zh"),
                ("target_language", "en"),
                ("glossary", """[{"from":"飞书","to":"Lark"},{"from":"妙搭","to":"Spark"}]""")),
            CancellationToken.None);

        var glossary = captured()!.Glossaies;
        glossary.Should().NotBeNull();
        glossary!.Should().HaveCount(2);
        glossary[0].From.Should().Be("飞书");
        glossary[0].To.Should().Be("Lark");
        glossary[1].From.Should().Be("妙搭");
    }

    // ───────────────────── 负例：本地拦截（不让平台报 1140101） ─────────────────────

    /// <summary>超 1000 字符必须<b>本地</b>拒绝并说明上限（平台上限见官方文档）。</summary>
    [Fact]
    public async Task TranslateText_Should_Reject_TextOverPlatformLimit_WithoutCallingDownstream()
    {
        var client = ClientCapturingTranslate(out var captured);
        var tools = CreateTools(client);

        var result = await tools.TranslateTextAsync(
            Args(
                ("text", new string('x', 1001)),
                ("source_language", "zh"),
                ("target_language", "en")),
            CancellationToken.None);

        result.Text.Should().Contain("[tool_error]");
        result.Text.Should().Contain("1000", "错误文案必须给出平台上限（模型据此分段）");
        captured().Should().BeNull("超限必须在本地拦下——不得把必然 400 的请求发给平台");
    }

    /// <summary>非法语言代码必须回填<b>合法值清单</b>（可自我纠正），且零下游调用。</summary>
    [Fact]
    public async Task TranslateText_Should_Reject_UnknownLanguage_WithLegalValues()
    {
        var client = ClientCapturingTranslate(out var captured);
        var tools = CreateTools(client);

        var result = await tools.TranslateTextAsync(
            Args(
                ("text", "hello"),
                ("source_language", "中文"),
                ("target_language", "en")),
            CancellationToken.None);

        result.Text.Should().Contain("source_language", "必须指明是哪个参数错了");
        result.Text.Should().Contain("中文", "必须回显模型给错的原值");
        result.Text.Should().Contain("zh-Hant", "必须给出合法取值清单（F-8 的 suggestions 等价物）");
        captured().Should().BeNull();
    }

    /// <summary>术语表超 128 项（平台上限）必须本地拒绝。</summary>
    [Fact]
    public async Task TranslateText_Should_Reject_GlossaryOverLimit()
    {
        var client = ClientCapturingTranslate(out var captured);
        var tools = CreateTools(client);

        var terms = string.Join(",", Enumerable.Range(0, 129).Select(i => $$"""{"from":"a{{i}}","to":"b{{i}}"}"""));
        var result = await tools.TranslateTextAsync(
            Args(
                ("text", "hello"),
                ("source_language", "en"),
                ("target_language", "zh"),
                ("glossary", $"[{terms}]")),
            CancellationToken.None);

        result.Text.Should().Contain("128");
        captured().Should().BeNull();
    }

    /// <summary>术语表不是合法 JSON / 缺 from-to 时必须给出可读的形态提示。</summary>
    [Theory]
    [InlineData("not-json", "不是合法 JSON")]
    [InlineData("""{"from":"飞书","to":"Lark"}""", "必须是 JSON 数组")]
    [InlineData("""[{"from":"飞书"}]""", "缺少 from/to")]
    public async Task TranslateText_Should_Reject_MalformedGlossary(string glossary, string expected)
    {
        var client = ClientCapturingTranslate(out var captured);
        var tools = CreateTools(client);

        var result = await tools.TranslateTextAsync(
            Args(
                ("text", "hello"),
                ("source_language", "en"),
                ("target_language", "zh"),
                ("glossary", glossary)),
            CancellationToken.None);

        result.Text.Should().Contain("glossary");
        result.Text.Should().Contain(expected);
        captured().Should().BeNull();
    }

    // ───────────────────── detect：请求映射与语言名 ─────────────────────

    [Fact]
    public async Task DetectLanguage_Should_PassText_And_MapLanguageName()
    {
        DetectTextRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AITranslation>();
        client.Setup(c => c.DetectTextAsync(It.IsAny<DetectTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DetectTextRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new FeishuApiResult<DetectTextResult>
            {
                Code = 0,
                Data = new DetectTextResult { Language = "en" },
            });

        var tools = CreateTools(client);

        var result = await tools.DetectTextAsync(Args(("text", "Hello")), CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Text.Should().Be("Hello", "识别请求体必须原样带上文本");
        result.Text.Should().Contain("\"language\"");
        result.Text.Should().Contain("en");
        result.Text.Should().Contain("英语", "语言代码必须映射为可读名（模型友好）");
    }

    /// <summary>未登记的语言代码：<c>language_name</c> 为 null（<b>不臆造</b>名称），代码原样返回。</summary>
    [Fact]
    public async Task DetectLanguage_Should_ReturnNullName_ForUnmappedCode()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AITranslation>();
        client.Setup(c => c.DetectTextAsync(It.IsAny<DetectTextRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<DetectTextResult>
            {
                Code = 0,
                Data = new DetectTextResult { Language = "zz" },
            });

        var tools = CreateTools(client);
        var result = await tools.DetectTextAsync(Args(("text", "???")), CancellationToken.None);

        var json = System.Text.Json.Nodes.JsonNode.Parse(result.Text)!.AsObject();
        json["language"]!.GetValue<string>().Should().Be("zz");
        json["language_name"].Should().BeNull("未登记的代码不得臆造语言名");
    }

    /// <summary>平台文档未声明 detect 的长度上限 ⇒ 只拦空文本（长文本不得被本地臆造上限拒绝）。</summary>
    [Fact]
    public async Task DetectLanguage_Should_NotImposeUndocumentedLengthLimit()
    {
        var captured = "";
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AITranslation>();
        client.Setup(c => c.DetectTextAsync(It.IsAny<DetectTextRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DetectTextRequest, CancellationToken>((r, _) => captured = r.Text)
            .ReturnsAsync(new FeishuApiResult<DetectTextResult>
            {
                Code = 0,
                Data = new DetectTextResult { Language = "en" },
            });

        var tools = CreateTools(client);
        var longText = new string('a', 5000);

        var result = await tools.DetectTextAsync(Args(("text", longText)), CancellationToken.None);

        result.Text.Should().NotContain("[tool_error]");
        captured.Should().Be(longText);
    }

    [Fact]
    public async Task DetectLanguage_Should_Reject_EmptyText()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AITranslation>();
        var tools = CreateTools(client);

        var result = await tools.DetectTextAsync(Args(("text", "   ")), CancellationToken.None);

        result.Text.Should().Contain("[tool_error]");
        result.Text.Should().Contain("text");
    }

    // ───────────────────── 软依赖与闭集口径 ─────────────────────

    /// <summary>客户端缺席（宿主未启用 AI 翻译能力）⇒ 可执行的结构化错误，而非空引用。</summary>
    [Fact]
    public async Task WithoutClient_Should_ReturnActionableError()
    {
        var tools = CreateTools();

        var result = await tools.TranslateTextAsync(
            Args(
                ("text", "hello"),
                ("source_language", "en"),
                ("target_language", "zh")),
            CancellationToken.None);

        result.Text.Should().Contain("[tool_error]");
        result.Text.Should().Contain("IFeishuTenantV1AITranslation", "错误必须告诉宿主该启用什么");
        result.Text.Should().Contain("translation:text", "错误必须指出所需权限点");
    }

    /// <summary>闭集口径：大小写不敏感命中 + 未命中拒绝（K-3 的"归一化"语义，独立于工具层）。</summary>
    [Fact]
    public void TranslationLanguages_Should_NormalizeCaseInsensitively()
    {
        TranslationLanguages.TryNormalize("EN", out var english).Should().BeTrue();
        english.Should().Be("en");

        TranslationLanguages.TryNormalize(" zh-hant ", out var traditional).Should().BeTrue();
        traditional.Should().Be("zh-Hant", "归一化为平台口径的规范大小写");

        TranslationLanguages.TryNormalize("中文", out _).Should().BeFalse("语言名不是合法取值（必须是语言代码）");
        TranslationLanguages.TryNormalize(null, out _).Should().BeFalse();

        ((Action)(() => TranslationLanguages.Require("source_language", "klingon")))
            .Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("zh-Hant");
    }
}
