// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

using Mud.Feishu.DataModels.Docx;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / F-2</b>：取值闭集的 golden 侧守卫（<b>B-1 ↔ F-2 联动锁</b>）。
/// </summary>
/// <remarks>
/// <see cref="BlockTypes"/> 是 <c>static class</c>（<c>TypeKind≠Enum</c>）。若判断被改回
/// "只认 <c>TypeKind == Enum</c>"（R-4 的失败模式），闭集会<b>静默消失</b>、模型退回猜数字。
/// </remarks>
public class ToolEnumClosedSetContractTests
{
    private static readonly string Golden = Path.Combine(
        FindRepositoryRoot(), "Mud.Feishu.AI.FeishuTools", "FeishuToolSchemas.golden.txt");

    /// <summary>核心断言：<c>block_type</c> 的 Schema 闭集与 <see cref="BlockTypes"/> 完全一致。</summary>
    [Fact]
    public void BlockType_ShouldCarryExactlyTheBlockTypesClosedSet()
    {
        var parameters = ReadSchemas()["docx.append_blocks"];

        parameters["block_type"].ValueKind.Should().Be(
            JsonValueKind.Object,
            "block_type 的 Schema 片段不是对象（复合 DTO 降级路径？）——无法承载 enum 闭集");

        parameters["block_type"].TryGetProperty("enum", out var values).Should().BeTrue(
            "block_type 未携带 enum 闭集 ⇒ 模型只能靠描述散文猜块类型（F-2 未落地或 R-4 回归）");

        var actual = values.EnumerateArray()
            .Select(static v => v.GetString())
            .Where(static v => !string.IsNullOrEmpty(v))
            .OrderBy(static v => v, StringComparer.Ordinal)
            .ToArray();

        actual.Should().Equal(
            BlockTypes.All.Select(BlockTypes.GetName).OrderBy(static n => n, StringComparer.Ordinal).ToArray(),
            "Schema 闭集与 BlockTypes 不一致——同一事实的两个面必须同步");

        actual.Should().NotContain("value__", "闭集混入了 enum 实例字段 value__（过滤失效）");
    }

    /// <summary>基线：已标注闭集的参数点位恰为 1 个；新增须显式更新基线。</summary>
    [Fact]
    public void ClosedSetPoints_ShouldMatchRegisteredBaseline()
    {
        var points = ReadSchemas()
            .SelectMany(kv => kv.Value
                .Where(static p => HasEnum(p.Value))
                .Select(p => $"{kv.Key}.{p.Key}"))
            .OrderBy(static p => p, StringComparer.Ordinal)
            .ToArray();

        points.Should().HaveCount(
            1,
            "已标注闭集的参数点位从 1 变为 {0} 个：{1}；新增属有意的契约收紧，请更新基线",
            points.Length,
            string.Join(" | ", points));

        points.Should().Contain("docx.append_blocks.block_type");
    }

    /// <summary>反向自证：解析器必须真的读到 parameters 与 enum，否则上面两条会假绿。</summary>
    [Fact]
    public void Reader_ShouldSeeParametersAndEnums_OtherwiseTheGuardsAreFalseGreen()
    {
        var schemas = ReadSchemas();
        schemas.Should().HaveCountGreaterThan(50, "解析到的工具数过少——解析规则已失效（假绿）");
        schemas.Values.SelectMany(static p => p.Values)
            .Any(static s => HasEnum(s))
            .Should().BeTrue("未解析到任何 enum 闭集——闭集解析已失效（假绿）");
    }

    /// <summary>该参数 Schema 是否带 <c>enum</c> 闭集。</summary>
    /// <remarks>
    /// ⚠️ <b>必须先判 <c>ValueKind</c></b>：golden 里存在<b>参数值不是对象</b>的形态，
    /// 而 <c>JsonElement.TryGetProperty</c> 对非 Object 元素**直接抛**
    /// <c>ThrowJsonElementWrongTypeException</c>，不是返回 false。
    /// </remarks>
    private static bool HasEnum(JsonElement schema)
        => schema.ValueKind == JsonValueKind.Object && schema.TryGetProperty("enum", out _);

    /// <summary>读取 golden 全部描述符的"工具名 → parameters 属性表"。</summary>
    private static Dictionary<string, Dictionary<string, JsonElement>> ReadSchemas()
    {
        var result = new Dictionary<string, Dictionary<string, JsonElement>>(StringComparer.Ordinal);

        foreach (var line in File.ReadLines(Golden))
        {
            var start = line.IndexOf('{');
            var end = line.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                continue;
            }

            using var document = JsonDocument.Parse(line[start..(end + 1)]);
            var root = document.RootElement;

            // ⚠️ 必须逐项校验 ValueKind：golden 里个别行的"首 { 到末 }"跨度会覆盖到
            //   描述文本中内嵌的花括号，此时取到的 name 不是字符串 ⇒ GetString() 抛
            //   ThrowJsonElementWrongTypeException。宁可跳过该行（表现为可能漏检），
            //   也不让解析器整体崩掉；过度跳过由"工具数 > 50"的反向自证拦住。
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("name", out var nameElement)
                || nameElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var name = nameElement.GetString();

            if (string.IsNullOrEmpty(name)
                || !root.TryGetProperty("parameters", out var parameters)
                || parameters.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            // ⚠️ parameters 的子键是 type / properties / required / anyOf，
            //   **参数定义在 properties 之下**（anyOf 才是 parameters 的直接子键）。
            if (!parameters.TryGetProperty("properties", out var properties)
                || properties.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            var map = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            foreach (var property in properties.EnumerateObject())
            {
                map[property.Name] = property.Value.Clone();
            }

            result[name!] = map;
        }

        return result;
    }

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
}
