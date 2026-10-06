// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-6</b>：条件必填（<c>anyOf</c>）的<b>语义</b>守卫 —— 不比文本，按 JSON Schema 语义判定。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须做语义判定而不能比文本</b>：本项实施时首版就产出了一个<b>语义反转</b>的产物
/// —— <c>"anyOf": [ {"required":["user_id","room_id"]}]</c>。它在 JSON Schema 里的含义是
/// "<b>两者都必填</b>"（AND），而声明意图是"<b>二选一</b>"（OR）。
/// 文本上"有 anyOf、有 required"⇒任何文本断言都会放行；语义上却是反的 ⇒ 模型被结构化地
/// 告知"必须同时给 user_id 和 room_id"，正确用法永远不命中——<b>比不表达更坏</b>。
/// </para>
/// <para>
/// 所以本守卫实现一个最小 <c>anyOf</c>/<c>required</c> 求值器，直接问：
/// 「只给 a，满足吗？」「只给 b，满足吗？」「都不给，满足吗？」正确答案必须是
/// <b>true / true / false</b>。语义反转会让首问变成 false，从而立刻变红。
/// </para>
/// <para>
/// <b>为什么不引JsonSchema.Net 之类的库</b>：工具面只需 <c>required</c> + <c>anyOf</c> 两个
/// 关键字的求值（十余行）；引第三方校验库会给测试链新增外部依赖与版本治理成本。
/// <b>只实现用到的部分，并明确注释这是求值器而非通用校验器。</b>
/// </para>
/// </remarks>
public class AnyOfSemanticContractTests
{
    private static readonly string GoldenPath = Path.Combine(
        FindRepositoryRoot(), "Mud.Feishu.AI.FeishuTools", "FeishuToolSchemas.golden.txt");

    /// <summary>
    /// 基线：带 <c>anyOf</c> 的工具恰为 <b>3 个</b>（三处条件必填），且组成员与运行时校验一致。
    /// </summary>
    [Fact]
    public void AnyOfBearingTools_ShouldMatchRegisteredBaseline()
    {
        var expected = new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["calendar.find_free_slots"] = ["user_id", "room_id"],
            ["contact.resolve_user"] = ["emails", "mobiles"],
            ["task.update_task"] = ["summary", "description", "due"],
        };

        var actual = ReadAnyOfDeclarations();

        // 反向自证：解析器必须真的读得到 anyOf，否则下面的等价断言是空对空。
        actual.Should().HaveCount(
            expected.Count,
            "未从 golden 解析到预期数量的 anyOf 声明——解析器已失效（假绿），请先修守卫");

        actual.Should().BeEquivalentTo(
            expected,
            "带 anyOf 的工具集或成员集发生了变化——新增/改名条件必填是<b>有意的契约变更</b>，"
            + "请同步更新本基线，并确认对应的执行器运行时校验也已同步");
    }

    /// <summary>
    /// <b>核心语义断言</b>：对每个 <c>anyOf</c> 组 ——
    /// 只给其中一个成员<b>必须满足</b>；一个都不给<b>必须不满足</b>。
    /// </summary>
    [Fact]
    public void AnyOf_ShouldMeanAtLeastOne_NotAllRequired()
    {
        var violations = new List<string>();

        foreach (var (toolName, schema) in ReadSchemasWithAnyOf())
        {
            var branches = ReadAnyOfBranchNames(schema);
            if (branches.Count == 0)
            {
                violations.Add($"{toolName}：解析不到 anyOf 分支");
                continue;
            }

            // 关键：payload 必须**先满足顶层 required**（如 find_free_slots 的 time_min/time_max），
            // 否则失败原因会落在 required 上而非 anyOf，测的就不是 anyOf 语义了。
            var topRequired = ReadTopLevelRequired(schema);

            // ① 只给 a（+ 顶层必填）⇒ 必须满足（这是"至少一个"的定义）。
            foreach (var only in branches)
            {
                var payload = SatisfyingBasePayload(topRequired);
                payload[only] = "x";

                if (!Satisfies(schema, payload))
                {
                    violations.Add(
                        $"{toolName}：满足顶层 required 后只给 '{only}' 却<b>不</b>满足 anyOf ⇒ 语义不是『至少一个』"
                        + $"（分支：{string.Join(" | ", branches)}）");
                }
            }

            // ② 一个都不给（但顶层必填已满足）⇒ 必须不满足（否则约束形同虚设）。
            if (Satisfies(schema, SatisfyingBasePayload(topRequired)))
            {
                violations.Add(
                    $"{toolName}：满足顶层 required 但一个 anyOf 成员都不给也通过 ⇒ 约束失效"
                    + $"（分支：{string.Join(" | ", branches)}）");
            }
        }

        violations.Should().BeEmpty(
            "anyOf 语义错误。JSON Schema 中 {{\"required\":[\"a\",\"b\"]}} 是 AND，"
            + "正确编码『至少一个』必须每个成员一个分支："
            + "{{\"anyOf\":[{{\"required\":[\"a\"]}},{{\"required\":[\"b\"]}}]}}：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// <b>求值器反向自证</b>：必须能<b>识别出</b> AND 形态的错误编码，
    /// 否则"求值器恒返回 true"会让上一条断言假绿。
    /// </summary>
    [Fact]
    public void Evaluator_ShouldRejectTheAndShapedEncoding_OtherwiseTheSemanticGuardIsFalseGreen()
    {
        // 反例：把"二选一"错误编码成单分支 AND（本项首版实现正是如此）。
        const string andShaped = """
            {"type":"object","properties":{"user_id":{"type":"string"},"room_id":{"type":"string"}},
             "anyOf":[{"required":["user_id","room_id"]}]}
            """;

        using var andDoc = JsonDocument.Parse(andShaped);
        var andSchema = andDoc.RootElement.Clone();

        // 只给 user_id ⇒ AND 形态下**必须不满足**（room_id 缺失）。
        Satisfies(andSchema, new Dictionary<string, object?> { ["user_id"] = "x" })
            .Should().BeFalse("AND 形态下只给一个成员本应不满足——若求值器说满足，说明它恒返回 true（假绿）");

        // 两个都给 ⇒ 满足。
        Satisfies(andSchema, new Dictionary<string, object?> { ["user_id"] = "x", ["room_id"] = "y" })
            .Should().BeTrue("AND 形态下两个都给本应满足");

        // 正确形态（每成员一分支）⇒ 只给一个即满足。
        const string orShaped = """
            {"type":"object","properties":{"user_id":{"type":"string"},"room_id":{"type":"string"}},
             "anyOf":[{"required":["user_id"]},{"required":["room_id"]}]}
            """;
        using var orDoc = JsonDocument.Parse(orShaped);
        var orSchema = orDoc.RootElement.Clone();

        Satisfies(orSchema, new Dictionary<string, object?> { ["user_id"] = "x" })
            .Should().BeTrue("OR 形态下只给一个成员本应满足");
        Satisfies(orSchema, new Dictionary<string, object?> { ["room_id"] = "y" })
            .Should().BeTrue("OR 形态下只给另一个成员也应满足");
        Satisfies(orSchema, new Dictionary<string, object?>())
            .Should().BeFalse("OR 形态下什么都不给应不满足");
    }

    /// <summary>
    /// 条件必填<b>不得</b>退化为"全部必填"：<c>anyOf</c> 分支里的成员<b>禁止</b>再出现在
    /// 顶层 <c>required</c> 中（两个关键字叠加会要求同时满足 ⇒ 语义冲突）。
    /// </summary>
    [Fact]
    public void AnyOfMembers_ShouldNotAlsoAppearInTopLevelRequired()
    {
        var violations = new List<string>();

        foreach (var (toolName, schema) in ReadSchemasWithAnyOf())
        {
            var topRequired = ReadTopLevelRequired(schema);
            var branchNames = ReadAnyOfBranchNames(schema);
            var intersection = topRequired.Intersect(branchNames, StringComparer.Ordinal).ToArray();

            if (intersection.Length > 0)
            {
                violations.Add($"{toolName}：{string.Join(" / ", intersection)} 同时出现在 required 与 anyOf");
            }
        }

        violations.Should().BeEmpty(
            "anyOf 成员同时是顶层 required ⇒ 两个关键字叠加会要求『既必填又至少一个』，语义冲突：{0}",
            string.Join(" | ", violations));
    }

    // ────────── golden 读取 ──────────

    /// <summary>读取 golden 中所有带 <c>anyOf</c> 的描述符（工具名 → parameters Schema 根元素）。</summary>
    private static List<(string ToolName, JsonElement Schema)> ReadSchemasWithAnyOf()
    {
        var results = new List<(string, JsonElement)>();

        foreach (var line in File.ReadLines(GoldenPath))
        {
            if (!line.Contains("\"anyOf\"", StringComparison.Ordinal))
            {
                continue;
            }

            var payload = ExtractJsonPayload(line);
            if (payload is null)
            {
                continue;
            }

            using var document = JsonDocument.Parse(payload);
            var root = document.RootElement;

            if (!root.TryGetProperty("parameters", out var parameters)
                || !parameters.TryGetProperty("anyOf", out _))
            {
                continue;
            }

            // 工具名以字段取，不解析常量名（常量名是派生的，会漂移）。
            var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "?" : "?";
            results.Add((name, parameters.Clone()));
        }

        return results;
    }

    /// <summary>读取 golden 中每个工具声明的 <c>anyOf</c> 成员集（工具名 → 成员名列表）。</summary>
    private static Dictionary<string, string[]> ReadAnyOfDeclarations()
        => ReadSchemasWithAnyOf()
            .ToDictionary(
                static item => item.ToolName,
                static item => ReadAnyOfBranchNames(item.Schema).ToArray(),
                StringComparer.Ordinal);

    /// <summary>从 golden 行中取出 JSON 负载（第一个 '{' 到最后一个 '}'）。</summary>
    private static string? ExtractJsonPayload(string line)
    {
        var start = line.IndexOf('{');
        var end = line.LastIndexOf('}');
        return start >= 0 && end > start ? line[start..(end + 1)] : null;
    }

    /// <summary>取出 <c>anyOf</c> 各分支的成员名（每分支取其 <c>required</c> 的全部元素）。</summary>
    private static List<string> ReadAnyOfBranchNames(JsonElement schema)
    {
        var names = new List<string>();
        if (!schema.TryGetProperty("anyOf", out var branches) || branches.ValueKind != JsonValueKind.Array)
        {
            return names;
        }

        foreach (var branch in branches.EnumerateArray())
        {
            names.AddRange(ReadRequired(branch));
        }

        return names;
    }

    /// <summary>读取某 Schema 节点的 <c>required</c> 数组。</summary>
    private static List<string> ReadRequired(JsonElement schema)
    {
        var names = new List<string>();
        if (!schema.TryGetProperty("required", out var required) || required.ValueKind != JsonValueKind.Array)
        {
            return names;
        }

        foreach (var item in required.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var value = item.GetString();
                if (!string.IsNullOrEmpty(value))
                {
                    names.Add(value);
                }
            }
        }

        return names;
    }

    /// <summary>读取顶层 <c>required</c> 集合。</summary>
    private static HashSet<string> ReadTopLevelRequired(JsonElement schema)
        => new(ReadRequired(schema), StringComparer.Ordinal);

    /// <summary>
    /// 构造一个"已满足顶层 required"的基底载荷（值统一为占位串）。
    /// </summary>
    /// <remarks>
    /// <b>为什么必须先满足顶层 required</b>：<c>find_free_slots</c> 的顶层
    /// <c>required</c> 含 <c>time_min/time_max</c>。若只放 <c>user_id</c>，
    /// 求值器会在<b>顶层 required</b> 上失败，于是断言变成"anyOf 是否存在"，
    /// <b>完全测不到 anyOf 语义</b>（假绿）。
    /// </remarks>
    private static Dictionary<string, object?> SatisfyingBasePayload(HashSet<string> topRequired)
    {
        var payload = new Dictionary<string, object?>();
        foreach (var name in topRequired)
        {
            payload[name] = "x";
        }

        return payload;
    }

    // ────────── 最小 anyOf/required 求值器 ──────────

    /// <summary>
    /// 最小求值器：顶层 <c>required</c>（全部存在）+ <c>anyOf</c>（任一分支满足）。
    /// </summary>
    /// <remarks>
    /// <b>刻意只实现这两个关键字</b>：工具参数 Schema 在本仓只需这两个即可表达全部条件必填；
    /// 泛化成完整 JSON Schema 校验器既无必要，也会引入"半实现"的误导风险。
    /// 求值器的反向自证见
    /// <see cref="Evaluator_ShouldRejectTheAndShapedEncoding_OtherwiseTheSemanticGuardIsFalseGreen"/>。
    /// </remarks>
    private static bool Satisfies(JsonElement schema, IReadOnlyDictionary<string, object?> payload)
    {
        foreach (var name in ReadRequired(schema))
        {
            if (!payload.ContainsKey(name))
            {
                return false;
            }
        }

        if (!schema.TryGetProperty("anyOf", out var branches) || branches.ValueKind != JsonValueKind.Array)
        {
            return true;
        }

        foreach (var branch in branches.EnumerateArray())
        {
            if (ReadRequired(branch).All(payload.ContainsKey))
            {
                return true;
            }
        }

        return false;
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