// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// R-2 验收：<b>形态认知收敛的等价性矩阵</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须有这层回归网</b>：R-2 把散落的 <c>JsonElement</c> 形态 switch 收敛为
/// 「<c>ToolArgumentNormalizer</c> 的判定原语 + <c>ToolArgs</c> 取值门面 + 三处薄委托」。
/// 收敛本身可能引入<b>静默语义漂移</b>（例如某处原本只认 JSON <c>true</c>，收敛后开始认字符串
/// <c>"true"</c>）——若只断言"薄委托与门面相等"，那么两者<b>一起错</b>时用例仍绿（假绿）。
/// 故本矩阵对<b>每一类入参形态</b>都钉住期望值（不是"两边相等"而是"两边都等于这个口径"）。
/// </para>
/// <para>
/// 覆盖的 6 处形态认知位置（R1.3 评审清单 + 本轮补齐的第 7 处）：
/// <list type="number">
/// <item><c>ToolArgs.OptionalBool</c>/<c>OptionalInt</c>/<c>OptionalString</c>（取值门面，基准）；</item>
/// <item><c>ToolPagination.ReadFetchAll</c>（原私有 <c>TryReadBool</c>）；</item>
/// <item><c>ToolPagination.ReadMaxItems</c>（原私有 <c>TryReadInt</c>）；</item>
/// <item><c>ToolDryRun.IsRequested(IReadOnlyDictionary)</c>；</item>
/// <item><c>ToolSearchTools</c> 的 limit 读取（原私有 <c>TryReadInt</c>，第 7 处副本）；</item>
/// <item><c>ToolArgsDigester.Describe</c>（摘要形态词汇，经判定原语）；</item>
/// <item><c>ToolArgumentNormalizer.IsJsonString</c>/<c>IsJsonArray</c>（判定原语本体）。</item>
/// </list>
/// </para>
/// </remarks>
public class ToolArgumentFormEquivalenceTests
{
    // ────────── 布尔形态：fetch_all / dry_run ──────────

    /// <summary>
    /// 布尔型入参的每类真实形态（<c>bool</c> / JSON <c>true|false</c> / 字符串）在两个入口上口径一致。
    /// </summary>
    /// <param name="value">入参值（<see langword="null"/> = 参数缺失）。</param>
    /// <param name="expected">期望语义（缺失/无法识别 → <see langword="false"/>）。</param>
    [Theory]
    [MemberData(nameof(BoolForms))]
    public void ReadFetchAll_ShouldUseTheSameFormVocabulary_AsArgsFacade(object? value, bool expected)
    {
        var arguments = Arguments(("fetch_all", value));

        ToolPagination.ReadFetchAll(arguments).Should().Be(
            expected,
            "fetch_all 的形态词汇必须与生成的 *Args.Unpack（走 ToolArgs）一致——"
            + "两条路径对同一入参判成不同形态，会让『自动翻页』在不同调用点上开关相反");

        (ToolArgs.OptionalBool(arguments, "fetch_all") ?? false).Should().Be(expected, "取值门面是基准口径");
    }

    /// <summary>
    /// 执行链侧（未强类型解包的原始参数字典）与执行器侧的 <c>dry_run</c> 判断口径一致。
    /// </summary>
    /// <remarks>
    /// 分叉的后果最严重：执行器认为"这是预演"（不下发），执行链认为"这是实发"
    /// （参与重试/幂等/审计），就会出现"预演被计成实发"或反之。
    /// </remarks>
    [Theory]
    [MemberData(nameof(BoolForms))]
    public void DryRunIsRequested_ShouldUseTheSameFormVocabulary_AsArgsFacade(object? value, bool expected)
    {
        var arguments = Arguments(("dry_run", value));

        ToolDryRun.IsRequested(arguments).Should().Be(
            expected, "执行链读取 dry_run 的口径必须与执行器解包口径一致");

        (ToolArgs.OptionalBool(arguments, "dry_run") ?? false).Should().Be(expected, "取值门面是基准口径");
    }

    /// <summary>布尔形态词汇表（含"无法识别 → 视为未提供"的负向形态）。</summary>
    public static TheoryData<object?, bool> BoolForms() => new()
    {
        { null, false }, // 参数缺失
        { true, true },
        { false, false },
        { Json("true"), true }, // JSON 布尔（tool_call 的真实形态）
        { Json("false"), false },
        { "true", true }, // 托管字符串形态（宿主/测试直接构造）
        { "false", false },
        { "TRUE", true }, // bool.TryParse 大小写不敏感
        { "yes", false }, // 非布尔文本 → 视为未提供（不抛）
        { 1, false }, // 数字形态不是布尔（不猜测）
        { Json("1"), false },
    };

    // ────────── 整数形态：max_items / limit ──────────

    /// <summary>
    /// 整型入参的每类真实形态在两处入口上口径一致（<c>int</c> / JSON 数字 / 数字字符串）。
    /// </summary>
    [Theory]
    [MemberData(nameof(IntForms))]
    public void ReadMaxItems_ShouldUseTheSameFormVocabulary_AsArgsFacade(object? value, int? expected)
    {
        var arguments = Arguments(("max_items", value));

        ToolPagination.ReadMaxItems(arguments).Should().Be(
            expected, "max_items 的形态词汇必须与生成的 *Args.Unpack（走 ToolArgs）一致");

        ToolArgs.OptionalInt(arguments, "max_items").Should().Be(expected, "取值门面是基准口径");
    }

    /// <summary>
    /// <b>与旧 <c>TryReadInt</c> 的有意口径变更（R-2）</b>：参数<b>存在但无法解析</b>为整数时必须抛
    /// （执行链转结构化错误），不再静默降级为"未提供"。
    /// </summary>
    /// <remarks>
    /// 旧私有副本返回 <see langword="null"/> ⇒ 静默使用默认值 ⇒ 模型把 <c>"max_items": "abc"</c>
    /// 当成"按默认取 200 条"，而它想要的可能是"取全部"。抛错让模型自查，与
    /// <c>ToolArgs.OptionalInt</c> 的既有语义（"0 与未提供是两种语义"）对齐。
    /// </remarks>
    [Theory]
    [InlineData("abc")]
    [InlineData("1.5")]
    [InlineData(1.5)]
    [InlineData(true)]
    public void ReadMaxItems_ShouldThrow_WhenValueCannotBeParsedAsInteger(object? value)
    {
        var arguments = Arguments(("max_items", value));

        var act = () => ToolPagination.ReadMaxItems(arguments);

        act.Should().Throw<ArgumentException>(
            "『提供但无法解析』不得静默降级为『未提供』——那是把模型的传参错误变成一次看似成功的默认行为"
            + "（由执行链转结构化错误回填模型，与生成的 *Args.Unpack 同语义）");
    }

    /// <summary>整型形态词汇表。</summary>
    public static TheoryData<object?, int?> IntForms() => new()
    {
        { null, null }, // 参数缺失
        { 5, 5 },
        { 0, 0 }, // 「0」与「未提供」是两种语义：此处保留 0，由 ResolveMaxItems 决定是否回落默认
        { 7L, 7 },
        { Json("5"), 5 },
        { Json("\"5\""), 5 }, // 数字字符串（模型常把整型写成字符串）
        { "5", 5 },
    };

    // ────────── 判定原语本体 ──────────

    /// <summary>
    /// 判定原语（<c>IsJsonString</c>/<c>IsJsonArray</c>）与遍历中心（<c>EnumerateTexts</c>）对同一形态的
    /// 判定必须一致——原语是中心自己的底层实现，两者分叉意味着"中心也另有一套判定"（R-2 的收敛目的落空）。
    /// </summary>
    [Fact]
    public void ShapePrimitives_ShouldAgreeWithEnumerationCenter()
    {
        var text = Json("\"hello\"");
        ToolArgumentNormalizer.IsJsonString(text, out var parsed).Should().BeTrue("JSON 文本元素");
        parsed.Should().Be("hello");
        ToolArgumentNormalizer.EnumerateTexts("text", text).Should().Equal([("text", "hello")]);

        var array = Json("[\"a\",\"b\"]");
        ToolArgumentNormalizer.IsJsonArray(array, out var arrayElement).Should().BeTrue("JSON 数组元素");
        arrayElement.GetArrayLength().Should().Be(2);
        ToolArgumentNormalizer.EnumerateTexts("texts", array)
            .Should().Equal([("texts[0]", "a"), ("texts[1]", "b")]);

        var number = Json("5");
        ToolArgumentNormalizer.IsJsonString(number, out var notText).Should().BeFalse("数字不是文本");
        notText.Should().BeNull();
        ToolArgumentNormalizer.IsJsonArray(number, out _).Should().BeFalse("数字不是数组");
        ToolArgumentNormalizer.EnumerateTexts("n", number).Should().BeEmpty("数值/布尔/null 不产出文本");

        // 负向自证：null 与托管 string 的判定（原语只认 JsonElement，托管形态由中心单独识别）。
        ToolArgumentNormalizer.IsJsonString(null, out _).Should().BeFalse();
        ToolArgumentNormalizer.IsJsonString("hello", out _).Should().BeFalse("托管 string 不经原语");
        ToolArgumentNormalizer.EnumerateTexts("text", "hello").Should().Equal([("text", "hello")]);
    }

    /// <summary>
    /// 摘要器（审计通道）的形态词汇经判定原语得出：JSON 文本元素按 <c>str(长度)</c>、数组按元素数，
    /// 不能退化成笼统的 <c>json</c>（那会让审计看不出"这是文本还是结构"）。
    /// </summary>
    [Fact]
    public void Digest_ShouldDescribeJsonForms_AfterPrimitiveDelegation()
    {
        ToolArgsDigester.Digest(Arguments(("text", Json("\"hello\"")))).Should().Be("text=str(5)");
        ToolArgsDigester.Digest(Arguments(("texts", Json("[\"a\",\"b\"]")))).Should().Be("texts=array(2)");
        ToolArgsDigester.Digest(Arguments(("text", "hello"))).Should().Be("text=str(5)");
        ToolArgsDigester.Digest(Arguments(("flag", true))).Should().Be("flag=bool", "托管 bool 形态");

        // 负向钉住（有意保留的粗粒度）：JSON 数字/布尔只归为 json——摘要器**不猜**语义类型，
        // 因为它的职责是"让审计看出这是标量还是结构"而非复刻取值门面（后者有自己的强类型转换）。
        ToolArgsDigester.Digest(Arguments(("count", Json("5")))).Should().Be("count=json");
        ToolArgsDigester.Digest(Arguments(("flag", Json("true")))).Should().Be("flag=json");
    }

    // ────────── 辅助 ──────────

    /// <summary>构造 JSON 元素（tool_call 反序列化后的真实形态）。<c>Clone</c> 以脱离 <see cref="JsonDocument"/> 生命周期。</summary>
    private static JsonElement Json(string raw) => JsonDocument.Parse(raw).RootElement.Clone();

    private static Dictionary<string, object?> Arguments(params (string Key, object? Value)[] items)
    {
        var map = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (key, value) in items)
        {
            // 值为 null 的项**不写入字典**：语义是"参数缺失"（模型 tool_call 的缺省形态）。
            if (value is not null)
            {
                map[key] = value;
            }
        }

        return map;
    }
}
