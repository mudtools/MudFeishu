// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// B2 错误契约守卫：确保结构化错误载荷的正确性与一致性。
/// </summary>
/// <remarks>
/// <para>
/// 守卫四条（方案 §3.B2）：
/// <list type="number">
/// <item><c>ToolErrorCategory</c> 与 <c>ToolErrorSubtype</c> 为闭集常量（新增需评审）；</item>
/// <item>全部执行器错误路径必产结构化载荷（金丝雀：注掉一处即红）；</item>
/// <item>首行必为合法 JSON（解析断言）；</item>
/// <item>载荷不在截断范围内（构造超长正文验证）。</item>
/// </list>
/// </para>
/// </remarks>
public class ToolErrorContractGuards
{
    /// <summary>
    /// 守卫 ①：ToolErrorCategory 必须恰好有 8 个值（闭集，新增需评审 + 守卫更新）。
    /// </summary>
    [Fact]
    public void ToolErrorCategory_ShouldHaveExactlyEightValues()
    {
        var enumValues = Enum.GetValues<ToolErrorCategory>();
        enumValues.Should().HaveCount(8,
            "ToolErrorCategory 必须为 8 类闭集（validation/policy/authorization/confirmation/retryable/api/internal/content_safety），"
            + "新增需评审并同批更新本断言");
    }

    /// <summary>
    /// 守卫 ②：ToolErrorSubtype 的常量不得为空字符串。
    /// </summary>
    [Fact]
    public void ToolErrorSubtype_Constants_ShouldNotBeEmpty()
    {
        var subtypeFields = typeof(ToolErrorSubtype)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        subtypeFields.Should().NotBeEmpty("ToolErrorSubtype 必须有常量定义");

        foreach (var field in subtypeFields)
        {
            var value = (string?)field.GetValue(null);
            value.Should().NotBeNullOrEmpty($"ToolErrorSubtype.{field.Name} 不得为空字符串");
        }
    }

    /// <summary>
    /// 守卫 ②b（PM 裁定 DP-R7-4）：<c>ToolErrorSubtype</c> 的每个常量都必须有<b>产出点或用途</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么要机械断言</b>：错误子类是对宿主的承诺（"你可以按 subtype 分支"）。
    /// 一个声明了却从不产出的子类，等于承诺了一条走不通的分支：宿主写了 <c>if</c> 却永远不进，
    /// 而文档表格里它看起来"已实现"。实测（2026-10-10）发现两处幽灵子类
    /// （<c>missing_scope</c> / <c>sanitizer_rejected</c>），已按裁定删除。
    /// </para>
    /// <para>
    /// <b>口径（刻意宽松）</b>：常量被引用一次即算"有用途"——既可以是 <c>Subtype</c> 实参，
    /// 也可以是策略拒绝的<b>文案前缀</b>（如 <c>$"{Token}: …"</c>）。目的是拦住"完全没人用"的常量，
    /// 而不是限制使用形态。
    /// </para>
    /// </remarks>
    [Fact]
    public void ToolErrorSubtype_Constants_ShouldAllHaveProducers()
    {
        var root = FindRepositoryRoot();

        var sources = new[] { "Mud.Feishu.AI.Tools", "Mud.Feishu.AI" }
            .Select(relative => Path.Combine(root, relative))
            .Where(Directory.Exists)
            .SelectMany(directory => Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories))
            .Select(File.ReadAllText)
            .ToArray();

        sources.Should().NotBeEmpty("扫描面不得为空（否则本守卫恒绿）");

        var constantNames = typeof(ToolErrorSubtype)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(static field => field.Name)
            .ToArray();
        constantNames.Should().NotBeEmpty("ToolErrorSubtype 必须有常量定义（空集会让本守卫恒绿）");

        var ghosts = FindGhostSubtypes(constantNames, string.Join('\n', sources));

        ghosts.Should().BeEmpty(
            "以下错误子类常量全仓无引用（幽灵子类）——它们对宿主是「承诺了却永远走不到」的分支。"
            + "修法：接线到真实产出点，或删除（新增需评审 + 本守卫覆盖）：{0}",
            string.Join(" | ", ghosts));
    }

    /// <summary>守卫 ②b 的自证：判据必须真的能报出"无人引用"的常量（否则本条是假门禁）。</summary>
    [Fact]
    public void GhostSubtypeDetection_ShouldReportUnreferencedConstant()
    {
        var ghosts = FindGhostSubtypes(
            ["UsedSubtype", "GhostSubtype"],
            "var x = ToolErrorSubtype.UsedSubtype; var y = ToolErrorSubtype.GhostSubtypeX;");

        ghosts.Should().ContainSingle().Which.Should().Be(
            "GhostSubtype",
            "判据必须精确匹配（GhostSubtypeX 不得让 GhostSubtype 被误判为已引用）");
    }

    /// <summary>幽灵子类判据（纯函数，便于自证）：返回在给定源码里找不到引用的常量名。</summary>
    private static IReadOnlyList<string> FindGhostSubtypes(
        IEnumerable<string> constantNames,
        string combinedSource)
        => [.. constantNames.Where(name => !System.Text.RegularExpressions.Regex.IsMatch(
            combinedSource,
            $@"ToolErrorSubtype\.{System.Text.RegularExpressions.Regex.Escape(name)}\b"))];

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }

    /// <summary>
    /// 守卫 ③：ToolErrorPayloadSerializer.Serialize 的输出必须是合法 JSON 首行。
    /// </summary>
    [Fact]
    public void Serialize_ShouldProduceValidJsonLine()
    {
        var error = new ToolError(
            Category: "retryable",
            Subtype: "rate_limited",
            Retryable: true,
            RetryAfterSeconds: 3,
            ApiCode: 99991400,
            Attempts: 1,
            Trace: "abc123",
            Tool: "mail.list_messages");

        var json = ToolErrorPayloadSerializer.Serialize(error);

        // 首行必须以 { 开头
        json.Should().StartWith("{", "载荷首行必须以 { 开头（合法 JSON）");

        // 必须可被 JsonDocument 解析
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow("载荷首行必须是合法 JSON（模型与宿主均可机械消费）");

        // 不得包含换行符（首行约束）
        json.Should().NotContain("\n", "载荷首行不得包含换行符（正文截断不得吃掉载荷）");
        json.Should().NotContain("\r", "载荷首行不得包含回车符");
    }

    /// <summary>
    /// 守卫 ④：载荷大小不得超过 MaxPayloadBytes 上界。
    /// </summary>
    [Fact]
    public void Serialize_ShouldNotExceedMaxPayloadBytes()
    {
        // 即使所有字段都填充最长的合理值，载荷也不应超过上界。
        var error = new ToolError(
            Category: "content_safety",
            Subtype: "injected_content_blocked",
            Retryable: false,
            RetryAfterSeconds: null,
            ApiCode: 99991663,
            Attempts: 99,
            Trace: new string('x', 64),  // 模拟较长的 trace
            Tool: "feishu.schema_read");

        var json = ToolErrorPayloadSerializer.Serialize(error);

        json.Length.Should().BeLessThanOrEqualTo(ToolErrorPayloadSerializer.MaxPayloadBytes,
            $"载荷必须 ≤ {ToolErrorPayloadSerializer.MaxPayloadBytes} 字节（正文截断不得吃掉载荷）");
    }

    /// <summary>
    /// 守卫 ⑤：ToolExecutor 的错误路径必须经过 FromStructuredError（源码结构断言）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 金丝雀：注掉 <c>FromApiOutcomeError</c> 中的 <c>FromStructuredError</c> 调用即红。
    /// </para>
    /// <para>
    /// <b>R-1 修订</b>：载荷序列化与 <c>FromError</c> 构造<b>已下沉到唯一出口</b>
    /// （<c>ToolErrorFactory</c> / <c>ToolResultPipeline</c>），故本守卫的判据同步改为
    /// "执行器骨架<b>委派</b>唯一出口 + 出口自身确实序列化"——原判据盯的是执行器骨架里的实现细节，
    /// 收口后它会变成"要求重复实现"的反向约束。
    /// </para>
    /// </remarks>
    [Fact]
    public void ToolExecutor_ErrorPaths_ShouldUseFromStructuredError()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "ToolExecutor.cs"));

        source.Should().Contain("FromStructuredError(",
            "ToolExecutor 的错误路径必须经过 FromStructuredError（工具名 + 分类文案的唯一入口）");
        source.Should().Contain("ToolResultPipeline.Error(",
            "ToolExecutor 必须把载荷构造委派给唯一出口 ToolResultPipeline（R-1 起不再自行拼装）");

        var pipeline = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Tools", "ToolResultPipeline.cs"));

        pipeline.Should().Contain("ToolErrorPayloadSerializer.Serialize(",
            "唯一出口必须调用 ToolErrorPayloadSerializer.Serialize 构造首行 JSON");
        pipeline.Should().Contain("FeishuToolResult.FromError(",
            "唯一出口必须通过 FeishuToolResult.FromError(ToolError, ...) 构造带结构化载荷的结果");
    }

    /// <summary>
    /// 守卫 ⑥：ToolErrorCategory 的枚举值必须与 CategoryLiteral 映射一致。
    /// </summary>
    [Fact]
    public void CategoryLiteral_ShouldMapAllEnumValues()
    {
        foreach (var category in Enum.GetValues<ToolErrorCategory>())
        {
            var literal = FeishuToolBinding.CategoryLiteral(category);
            literal.Should().NotBeNullOrEmpty(
                $"CategoryLiteral 必须为 {category} 返回非空字符串");
        }
    }
}

