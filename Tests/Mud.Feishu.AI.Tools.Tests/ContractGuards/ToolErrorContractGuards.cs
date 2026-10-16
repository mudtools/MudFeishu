// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

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
    // 守卫 ①（原 ToolErrorCategory_ShouldHaveExactlyEightValues）已删除（测试体系治理 B1，
    // 见《单元测试体系过度测试分析与解决方案》§三.4；该文档已迁 Mud.AI 仓）："恰好 8 个值"被两处
    // **等式守卫**完全包含——ToolErrorDocContractGuards.DocumentCategories_ShouldEqual_CodeClosedSet
    // 与 McpResourceContractGuards.ErrorContractSummary_ClosedSets_ShouldMatchCodeClosure
    // 都把 CategoryLiteral 映射结果与 8 字面量闭集逐一比对，多一个/少一个值都报红。
    // 计数型断言只留一份真相语义，不再有独立副本。

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

        // v2.3 迁移：错误契约族（ToolError / ToolErrorClassifier / ToolErrorText / ToolResultPipeline…）
        // 中立化迁出飞书仓（Mud.AI.Agent → 新仓）——本仓扫描面收窄为**产品侧**两工程。
        // 中立侧产出点由新仓 ToolErrorContractGuards 的同名守卫持有；两面互补，缺一即覆盖不全。
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

        var combined = string.Join('\n', sources);

        // 中立独有子类（产品侧按设计无产出点）：其产出点随错误契约族迁出本仓，故从产品面幽灵判据中减去。
        // 反缩水：每个被减去的名称必须确实在产品面内无引用——若哪天产品侧真的产出了它，
        // 说明它已不再是「中立独有」，必须从本清单移除（否则产品面的真实幽灵会被这条豁免掩盖）。
        var neutralOnlySubtypes = new[] { "ShapeMismatch", "Transient", "Timeout", "Unexpected" };
        foreach (var name in neutralOnlySubtypes)
        {
            System.Text.RegularExpressions.Regex.IsMatch(
                    combined, $@"ToolErrorSubtype\.{System.Text.RegularExpressions.Regex.Escape(name)}\b")
                .Should().BeFalse(
                    $"ToolErrorSubtype.{name} 被登记为「中立独有」（产品面无产出点）——"
                    + "产品侧既然引用了它，请把它从本豁免清单移除，否则产品面的真实幽灵会被掩盖");
        }

        var candidates = constantNames.Where(
            name => !neutralOnlySubtypes.Contains(name, StringComparer.Ordinal));
        var ghosts = FindGhostSubtypes(candidates, combined);

        ghosts.Should().BeEmpty(
            "以下错误子类常量在产品面全无引用（幽灵子类）——它们对宿主是「承诺了却永远走不到」的分支。"
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

    // 守卫 ⑤（原 ToolExecutor_ErrorPaths_ShouldUseFromStructuredError）已迁出飞书仓
    // （v2.3 迁移）：执行器骨架与唯一出口随骨架中立化迁入新仓 Mud.AI.Agent/Tools——
    // 判据锚点全在中立文件，故由新仓 ToolErrorContractGuards.ToolExecutor_ErrorPaths_ShouldUseFromStructuredError 持有。

    // 守卫 ⑥（原 CategoryLiteral_ShouldMapAllEnumValues）已删除（同守卫 ①，测试体系治理 B1）：
    // 两处等式守卫把 CategoryLiteral 的输出与字面量闭集逐一比对——空串/错映射/未映射
    // （CategoryLiteral 的 `_` 兜底返回 "internal"）都会使等式破缺而报红，独立非空断言被完全包含。

    /// <summary>
    /// 守卫 ⑦（R-3 / BUG-8）：<c>suggestions</c> 存在时必须进入<b>首行 JSON 载荷</b>。
    /// </summary>
    /// <remarks>
    /// BUG-8 的判据是"模型可<b>结构化</b>恢复"——建议若只出现在人类可读正文里，
    /// 模型仍须解析文本，等于没解决问题。故断言它落在载荷里且可机械解析。
    /// </remarks>
    [Fact]
    public void Serialize_ShouldCarrySuggestions_WhenPresent()
    {
        var error = new ToolError(
            Category: "validation",
            Subtype: "invalid_args",
            Retryable: false,
            Attempts: 1,
            Tool: "docx.append_blocks",
            Suggestions: ["参数 block_type 的合法取值：Text / Heading1 / Divider"]);

        var json = ToolErrorPayloadSerializer.Serialize(error);

        json.Should().Contain("\"suggestions\":[", "恢复候选必须进入载荷（BUG-8：不得只放人类可读正文）");

        using var document = JsonDocument.Parse(json);
        var suggestions = document.RootElement
            .GetProperty("tool_error")
            .GetProperty("suggestions")
            .EnumerateArray()
            .Select(static e => e.GetString())
            .ToArray();

        suggestions.Should().ContainSingle().Which.Should().Be("参数 block_type 的合法取值：Text / Heading1 / Divider");
    }

    /// <summary>守卫 ⑦ 的反向：无候选时<b>不得</b>凭空产出空数组（避免"看起来有建议"的噪声）。</summary>
    [Fact]
    public void Serialize_ShouldOmitSuggestionsKey_WhenAbsent()
    {
        var json = ToolErrorPayloadSerializer.Serialize(new ToolError("api", "upstream_error", false));

        json.Should().NotContain("suggestions", "没有事实来源时不得产出该键（空数组会被模型误读为'有建议但为空'）");
    }

    /// <summary>
    /// 守卫 ⑧（预算单向上界）：候选是自由文本，必须被条数/长度上界化，且
    /// <b>宁可丢候选也不得突破 512 上界</b>——分类字段（category/subtype/retryable）恒在。
    /// </summary>
    /// <remarks>
    /// 这是 BUG-8 引入自由文本后唯一的硬风险：若为了塞进建议而牺牲分类字段，
    /// 载荷的"可分支"价值反而归零（没有分类，建议也无从解读）。
    /// </remarks>
    [Fact]
    public void Serialize_ShouldNeverExceedPayloadBound_EvenWithHugeSuggestions()
    {
        var error = new ToolError(
            Category: "validation",
            Subtype: "invalid_args",
            Retryable: false,
            ApiCode: 99991663,
            Attempts: 7,
            Trace: new string('t', 48),
            Tool: "docx.append_blocks",
            Suggestions: [.. Enumerable.Range(0, 12).Select(static i => new string('建', 400) + i)]);

        var json = ToolErrorPayloadSerializer.Serialize(error);

        json.Length.Should().BeLessThanOrEqualTo(
            ToolErrorPayloadSerializer.MaxPayloadBytes,
            "候选超长时必须丢候选，而不是突破载荷上界");

        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement.GetProperty("tool_error");
        payload.GetProperty("category").GetString().Should().Be("validation",
            "丢候选也不得丢分类——那是模型可分支的最小集");
        payload.GetProperty("subtype").GetString().Should().Be("invalid_args");
        payload.TryGetProperty("retryable", out _).Should().BeTrue();
    }

    /// <summary>守卫 ⑧ 的自证：判据确实能报出"超界"（否则本条是假门禁）。</summary>
    [Fact]
    public void PayloadBound_ShouldDetectOverlongPayload_OtherwiseGuardIsFalseGreen()
    {
        // 直接构造一个"绕开序列化器"的超界载荷，确认本守卫用到的判据（长度比较）真的会报红。
        var oversized = new string('x', ToolErrorPayloadSerializer.MaxPayloadBytes + 1);

        oversized.Length.Should().BeGreaterThan(
            ToolErrorPayloadSerializer.MaxPayloadBytes,
            "判据失效：长度比较报不出超界（假绿）");
    }

    // ────────── E-1 / E-2 / E-5（差距补齐批次） ──────────

    /// <summary>
    /// <b>E-1</b>：授权类拒绝必须带 <c>required_scopes</c>（工具声明的 scope 事实投影）。
    /// </summary>
    /// <remarks>
    /// 判据落在<b>载荷</b>而非正文：模型要能"机械地"读到权限事实（正文可能被预算截断）。
    /// </remarks>
    [Fact]
    public async Task AuthorizationDenial_ShouldCarryRequiredScopes_InPayload()
    {
        var binding = new FeishuToolBinding(
            new NoopScopeFactory(),
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

        var result = await binding.ExecuteAsync(
            new FeishuToolDefinition(
                "im.send_message", "发消息", ["im:message"], true, FeishuToolRisk.Write, "tenant",
                (_, _, _) => Task.FromResult(ToolResult.FromText("ok"))),
            new Dictionary<string, object?> { ["text"] = "hi" },
            new FeishuToolContext("appA"),
            _ => Task.FromResult(ToolResult.FromText("ok")));

        var text = result.ToString()!;
        text.Should().Contain("\"category\":\"authorization\"");
        text.Should().Contain("\"required_scopes\":[\"im:message\"]",
            "授权拒绝必须投影工具声明的 scope（E-1：结构化事实，不是猜测的缺失权限）");
    }

    /// <summary>
    /// <b>E-2</b>：码表（codemeta）命中的码必须给出"语义 + 行动"候选；未收录的码<b>不得</b>猜测。
    /// </summary>
    /// <remarks>
    /// 码表自 2026-10-14 起在<b>飞书侧</b>（<c>FeishuApiErrorVocabulary</c>；独立产品线纪律）——
    /// 候选经 <c>ToolApiFailure.Suggestions</c> 随解包结果进入中立层，故断言改在读数上。
    /// </remarks>
    [Fact]
    public void CodeMeta_ShouldEnrichKnownCode_AndStayNeutralForUnknownCode()
    {
        var known = FeishuApiErrorVocabulary.Classify(99991400).Suggestions;
        known.Should().NotBeNull("99991400（系统繁忙）有实证：必须给出语义 + 行动");
        known!.Should().ContainSingle().Which.Should().Contain("系统繁忙").And.Contain("建议：");

        FeishuApiErrorVocabulary.Classify(99999999).Suggestions.Should().BeNull(
            "未收录的码不得产出候选（猜测性码表会把模型引向错误处置）");
        FeishuApiErrorVocabulary.Classify(null).Suggestions.Should().BeNull();
    }

    /// <summary>码表条目必须有域标注、含义与行动（缺一即为不可用条目）。</summary>
    [Fact]
    public void CodeMeta_Entries_ShouldAllBeUsable()
    {
        FeishuApiErrorVocabulary.All.Should().NotBeEmpty("码表不得为空（空表 = E-2 未落地）");

        foreach (var entry in FeishuApiErrorVocabulary.All)
        {
            entry.Code.Should().BeGreaterThan(0);
            entry.Meaning.Should().NotBeNullOrWhiteSpace($"码 {entry.Code} 必须给出含义");
            entry.Action.Should().NotBeNullOrWhiteSpace($"码 {entry.Code} 必须给出行动（否则模型只知道「错在哪」）");

            // 域标注为 null 表示"跨域通用码"（平台级），非 null 时必须是真实工具域。
            if (entry.Domain is not null)
            {
                FeishuToolNames.All
                    .Select(static name => name.Substring(0, name.IndexOf(".", StringComparison.Ordinal)))
                    .Should().Contain(
                        entry.Domain,
                        $"码 {entry.Code} 标注的域「{entry.Domain}」必须是真实工具域（僵尸域标注 = 该码的适用面不可信）");
            }
        }
    }

    /// <summary>
    /// <b>E-5</b>：可重试类错误必须带 <c>retry_after_seconds</c>（有产出点时），
    /// 且该字段只在可重试类出现（不得给确定性失败一个"建议等待"）。
    /// </summary>
    [Fact]
    public void RetryAfterSeconds_ShouldBeProducedForRetryable_AndAbsentOtherwise()
    {
        var retryable = ToolErrorFactory.Create(
            "mail.list_messages", ToolErrorCategory.Retryable, ToolErrorSubtype.RateLimited,
            retryAfterSeconds: 3);
        retryable.RetryAfterSeconds.Should().Be(3);
        retryable.Retryable.Should().BeTrue();

        var validation = ToolErrorFactory.Create(
            "mail.list_messages", ToolErrorCategory.Validation, ToolErrorSubtype.InvalidArgs);
        validation.RetryAfterSeconds.Should().BeNull("确定性失败不得带「建议等待」（那会诱导模型无意义重试）");
    }

    /// <summary>码表命中的码即使载荷预算吃紧，也要优先保住分类字段（降级顺序单向）。</summary>
    [Fact]
    public void PayloadDegradation_ShouldDropScopesBeforeCategories()
    {
        var error = new ToolError(
            Category: "authorization",
            Subtype: "authorization_denied",
            Retryable: false,
            Attempts: 1,
            Tool: "im.send_message",
            Suggestions: [new string('建', 400)],
            RequiredScopes: [.. Enumerable.Range(0, 6).Select(static i => $"scope:very_long_{i}")]);

        var json = ToolErrorPayloadSerializer.Serialize(error);

        json.Length.Should().BeLessThanOrEqualTo(ToolErrorPayloadSerializer.MaxPayloadBytes);
        using var document = JsonDocument.Parse(json);
        var payload = document.RootElement.GetProperty("tool_error");
        payload.GetProperty("category").GetString().Should().Be("authorization");
        payload.GetProperty("subtype").GetString().Should().Be("authorization_denied");
    }

    /// <summary>
    /// <b>传输层状态码的接线（独立产品线纪律的端到端证据）</b>：中立的 <c>Mud.AI.Agent</c>
    /// 已不认识 <c>Mud.HttpUtils.ApiException</c>，状态码必须由 <c>FeishuTransportFaultAdapter</c>
    /// 经中立端口 <c>ITransportFault</c> 传入分类器。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么必须是行为断言（不是源码文本断言）</b>：漏接适配时异常会落到中立分类器的
    /// <c>HttpRequestException</c> 兜底分支（<c>retryable/transient</c>），403 被误判为可重试，
    /// 模型将对一个确定性失败无意义重试——而"<c>ToolErrorClassifier.Classify(ex)</c> 少了第二个实参"
    /// 这种漂移在文本断言下极易假绿（注释里也写着同一个名字）。
    /// </para>
    /// <para>本用例同时是 <c>FeishuTransportFaultAdapter</c> 的防腐层证据：换任何 HTTP 组件（不只有飞书这套），
    /// 只要适配层给出状态码，分类结果不变。</para>
    /// </remarks>
    [Fact]
    public async Task TransportForbidden_ShouldBeClassifiedAsAuthorization()
    {
        var binding = new FeishuToolBinding(
            new NoopScopeFactory(),
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

        var result = await binding.ExecuteAsync(
            new FeishuToolDefinition(
                "im.get_history_messages", "读消息", [], false, FeishuToolRisk.Read, "tenant",
                (_, _, _) => Task.FromResult(ToolResult.FromText("ok"))),
            new Dictionary<string, object?>(),
            new FeishuToolContext("appA"),
            _ => throw new Mud.HttpUtils.ApiException(System.Net.HttpStatusCode.Forbidden, "无权限"));

        result.Error.Should().NotBeNull("异常路径必须产出结构化载荷（总体设计 §4 不变式：不抛裸异常）");
        result.Error!.Category.Should().Be(
            "authorization",
            "403 必须由适配层经中立端口进入分类器（漏接 ⇒ 被判成 retryable/transient，模型会无意义重试）");
        result.Error!.Subtype.Should().Be("authorization_denied");

        var text = result.ToString()!;
        text.Should().Contain("(forbidden)");
        text.Should().NotContain("(retryable)", "确定性失败不得被回填成可重试语义");
    }

    /// <summary>
    /// <b>业务码读数的接线（独立产品线纪律的第二条端到端证据）</b>：中立的 <c>Mud.AI.Agent</c>
    /// 已不认识飞书码表，"码 → 段位 + 码级候选"必须由 <c>FeishuApiResultReader</c> 经
    /// <c>ToolApiFailure</c> 随解包结果带出，再由执行器填进载荷。
    /// </summary>
    /// <remarks>
    /// 漏接读数时 99991400（系统繁忙；可能以 <b>HTTP 200 + JSON 错误体</b>返回，故不走 HTTP 段位判定）
    /// 会落到中性的 <c>api/upstream_error</c>——模型因此放弃一个"本该退避重试"的调用。
    /// 从<b>读信封</b>一路断言到载荷，故"读取器忘了带读数"或"执行器忘了转发读数"任一处漂移都会报红。
    /// </remarks>
    [Fact]
    public void ApiCodeReading_ShouldTravelFromReaderToPayload()
    {
        var outcome = FeishuApiResultReader.Read(new FeishuApiResult<System.Text.Json.Nodes.JsonObject>
        {
            Code = 99991400,
            Msg = "系统繁忙",
        });

        var result = new ToolExecutor("mail.list_messages", 4096)
            .FromApi(outcome, static _ => new System.Text.Json.Nodes.JsonObject());

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("retryable", "99991400 有实证（系统繁忙）⇒ 必须可重试");
        result.Error.Subtype.Should().Be("rate_limited");
        result.Error.ApiCode.Should().Be(99991400, "code 必须透出（宿主/模型按码排查）");
        result.Error.Suggestions.Should().NotBeNull("码级候选（语义 + 行动）必须随读数透出（E-2）");

        result.ToString().Should().Contain("(retryable)");
    }

    private sealed class NoopScopeFactory : IFeishuAppContextScopeFactory
    {
        public IDisposable BeginScope(string appKey) => new NoopScope();

        private sealed class NoopScope : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}

