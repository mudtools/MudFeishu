// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R-1 / B-1 / B-2：<b>出站唯一出口</b>守卫——确保 <c>ToolResultPipeline</c> 是回填模型文本的
/// 唯一构造点，「预算与截断标记」不再是可以忘记写的一步。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么原守卫失效（Q-1，R1.3 评审发现）</b>：上一版守卫的三条断言把扫描目标
/// <b>写死为单文件</b> <c>Internal/ToolExecutor.cs</c>，于是 8 个"绕过 <c>ToolExecutor</c> 助手、
/// 直调 <c>FeishuToolResult.FromText</c>"的执行器（<c>SchemaReadTools</c> / <c>CapabilityLookupTools</c> /
/// <c>ToolSearchTools</c> / <c>KnowledgeSearchTools</c> / <c>DocxSheetsDriveWriteTools</c> …）
/// <b>完全不在守卫视野内</b>——这才是 B-1 长期存活的机制原因。本版把扫描面扩到
/// <c>Internal/</c> <b>全目录</b>，并补上行为级断言（不只做文本匹配）。
/// </para>
/// <para>
/// <b>断言分层（Readme §6 防假绿铁律）</b>：
/// <list type="bullet">
/// <item>行为断言（①–③）：<c>ToolResultPipeline</c> 在超预算时确实回填标记——这是"必经"性质，必须行为断言；</item>
/// <item>结构断言（④–⑤）：<c>Internal/</c> 与执行链<b>不得</b>再出现"裸截断 + FromText"形态——
/// 只用于禁止特定代码形态，不用于证明"必经"；</item>
/// <item>等价性（⑥）：<c>ToolError</c> 只允许一个构造点（B-2 三处归一）。</item>
/// </list>
/// </para>
/// </remarks>
public class TruncationVisibilityContractGuards
{
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

    // ───────────────────────── 行为断言（①–③）─────────────────────────

    /// <summary>
    /// ① 纯文本超预算时，唯一出口必须回填 <c>Truncated=true</c> 且给出非空原因。
    /// </summary>
    [Fact]
    public void Ok_ShouldBackfillTruncated_WhenOverBudget()
    {
        var result = ToolResultPipeline.Ok(new string('x', 500), maxLength: 100);

        result.Truncated.Should().BeTrue(
            "超预算文本经唯一出口时必须回填截断标记（B-1 的 8 处静默截断即此处漏标记）");
        result.TruncationReason.Should().NotBeNullOrEmpty("截断必须对模型可见原因");
    }

    /// <summary>
    /// ② JSON 信封超预算时同上（<c>OkJson</c> 路径——手写信封的唯一合法形态）。
    /// </summary>
    [Fact]
    public void OkJson_ShouldBackfillTruncated_WhenOverBudget()
    {
        var big = new JsonArray();
        for (var i = 0; i < 200; i++)
        {
            big.AddNode(JsonValue.Create($"条目 {i} —— 一段足够长的占位文本，用于把信封撑过预算"));
        }

        var result = ToolResultPipeline.OkJson(new JsonObject { ["items"] = big }, maxLength: 200);

        result.Truncated.Should().BeTrue("超预算信封经唯一出口时必须回填截断标记");
        result.TruncationReason.Should().NotBeNullOrEmpty("截断必须对模型可见原因");
    }

    /// <summary>
    /// ③ 未超预算时不得误标（防"标记恒为 true"这种反向假事实）。
    /// </summary>
    [Fact]
    public void Ok_ShouldNotMark_WhenWithinBudget()
    {
        var result = ToolResultPipeline.Ok("短文本", maxLength: 1000);

        result.Truncated.Should().BeFalse("未超预算不应产生截断标记");
        result.TruncationReason.Should().BeNull();
    }

    /// <summary>
    /// ③' <b>边界回归</b>：截断器会<b>附加</b>标记文本（如 101 字符截到 100 再附约 80 字符提示），
    /// 因此"截断后长度 &lt; 原文长度"这一判据会漏判——唯一出口必须用"是否超预算"判定。
    /// </summary>
    /// <remarks>
    /// 金丝雀：把 <c>IsOverBudget</c> 换成"截断后长度比较"即红。
    /// </remarks>
    [Fact]
    public void Ok_ShouldMarkTruncation_EvenWhenMarkerMakesTextLonger()
    {
        var original = new string('x', 101);
        var result = ToolResultPipeline.Ok(original, maxLength: 100);

        result.Truncated.Should().BeTrue(
            "截断标记会让结果比原文更长——用长度比较判定会漏判（这正是本守卫存在的理由）");
        result.ToString().Length.Should().BeGreaterThan(100, "截断器在保留预算内文本后会附加提示");
    }

    /// <summary>
    /// ③'' <c>maxLength &lt;= 0</c> = 无预算（写工具单 ID 迷你信封的既有语义）——
    /// 不得因为把 0 传给截断器而截成 1 个字符。
    /// </summary>
    [Fact]
    public void Ok_ShouldNotTruncate_WhenBudgetIsZero()
    {
        var text = new string('y', 300);
        var result = ToolResultPipeline.Ok(text, maxLength: 0);

        result.ToString().Should().Be(text, "maxLength=0 表示无预算，不是'截断到 1 个字符'");
        result.Truncated.Should().BeFalse();
    }

    // ───────────────────── 结构断言（④–⑤，扩面后的核心）─────────────────────

    /// <summary>
    /// ④ <c>Internal/</c> <b>全目录</b>不得出现"裸截断调用 + FromText"组合——
    /// 这是 B-1 两类（8 处静默截断 + 12 处无预算回执）的共同形态。
    /// </summary>
    /// <remarks>
    /// 判据窗口覆盖整条语句（<c>[^;]*?</c> + Singleline），故多行调用同样被逮住。
    /// 白名单为<b>空</b>：合法形态只有 <c>ToolResultPipeline.Ok/OkJson/OkReceipt</c>。
    /// </remarks>
    [Fact]
    public void InternalExecutors_ShouldNotBypassEgressPipeline()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal");
        var files = Directory.GetFiles(directory, "*.cs", SearchOption.TopDirectoryOnly);

        files.Should().NotBeEmpty("未扫到执行器源文件——路径错了（假绿），请先修守卫");

        var pattern = new Regex(
            @"FeishuToolResult\.FromText\([^;]*?(ToolResultText\.Truncate|ToolResultJson\.ToText)",
            RegexOptions.Singleline);

        var violations = new List<string>();
        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            // 显式限定 Match 类型：本工程全局 using 了 Moq，其 Moq.Match 与 Regex.Match 同名（CS0104）。
            foreach (System.Text.RegularExpressions.Match m in pattern.Matches(source))
            {
                violations.Add($"{Path.GetFileName(file)}: {Collapse(m.Value)}");
            }
        }

        violations.Should().BeEmpty(
            "以下出站路径绕过了唯一出口 ToolResultPipeline（截断标记/预算会静默丢失——B-1）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// ⑤ 反向自证：上述正则必须<b>真的</b>能命中被禁止的形态，否则 ④ 会因"扫不到"而假绿。
    /// </summary>
    [Fact]
    public void Guard_ShouldActuallyMatchTheBannedShape()
    {
        const string canary =
            "return FeishuToolResult.FromText(\n    ToolResultText.TruncateJson(ToolResultJson.ToText(envelope), n));";

        new Regex(
            @"FeishuToolResult\.FromText\([^;]*?(ToolResultText\.Truncate|ToolResultJson\.ToText)",
            RegexOptions.Singleline)
            .IsMatch(canary)
            .Should().BeTrue("金丝雀：判据或形态变了，④ 会静默失效");
    }

    /// <summary>
    /// ⑤' <c>ToolExecutor</c> 的预算是<b>委托</b>给唯一出口的，而不是自己再实现一遍。
    /// </summary>
    [Fact]
    public void ToolExecutor_ShouldDelegateBudgetToTheSingleEgress()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "ToolExecutor.cs"));

        source.Should().Contain("ToolResultPipeline.",
            "FromApi/FromPlainText/FromPagedResult 必须委托唯一出口（R-1）");
        source.Should().NotContain("ToolResultText.Truncate",
            "预算实现不得在执行器骨架里再写一份（形态单源）");
    }

    // ───────────────────── 错误载荷单一构造点（⑥，B-2 三处归一）─────────────────────

    /// <summary>
    /// ⑥ <c>new ToolError(...)</c> 只允许出现在唯一工厂内（B-2：此前 3 处同构拷贝）。
    /// </summary>
    [Fact]
    public void ToolError_ShouldBeConstructedOnlyAtTheSingleFactory()
    {
        var withNewToolError = new List<string>();
        foreach (var project in new[] { "Mud.Feishu.AI.Tools" })
        {
            foreach (var file in Directory.GetFiles(
                         Path.Combine(FindRepositoryRoot(), project), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(@"\obj\", StringComparison.Ordinal)
                    || file.Contains(@"\bin\", StringComparison.Ordinal))
                {
                    continue;
                }

                // 唯一合法构造点：工厂自身。
                if (string.Equals(Path.GetFileName(file), "ToolResultPipeline.cs", StringComparison.Ordinal))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                source.Should().NotContain("new ToolError(", $"{Path.GetFileName(file)} 必须经 ToolErrorFactory");
                if (source.Contains("new ToolError(", StringComparison.Ordinal))
                {
                    withNewToolError.Add(file);
                }
            }
        }

        withNewToolError.Should().BeEmpty(
            "ToolError 载荷只允许在 ToolErrorFactory 内构造——其他位置的拷贝会在契约演进时静默漂移（B-2）");
    }

    /// <summary>
    /// ⑥' 工厂输出冻结（三处归一的<b>等价性基线</b>）：字段语义与归一前逐字一致。
    /// </summary>
    /// <remarks>
    /// 这份基线就是"3 处拷贝合并为 1 处"的安全性证据——任何字段语义漂移（如
    /// <c>Retryable</c> 的派生口径、<c>Attempts</c> 的默认值）都会在此报红。
    /// </remarks>
    [Fact]
    public void ToolErrorFactory_ShouldProduceTheFrozenPayloadShape()
    {
        var retryable = ToolErrorFactory.Create("demo.tool", ToolErrorCategory.Retryable, "rate_limited");
        retryable.Category.Should().Be("retryable");
        retryable.Subtype.Should().Be("rate_limited");
        retryable.Retryable.Should().BeTrue("Retryable 由 category 派生（归一前的口径）");
        retryable.RetryAfterSeconds.Should().BeNull();
        retryable.ApiCode.Should().BeNull();
        retryable.Attempts.Should().Be(1, "默认尝试次数为 1（归一前的口径）");
        retryable.Trace.Should().BeNull("默认无追踪号");
        retryable.Tool.Should().Be("demo.tool");

        var validation = ToolErrorFactory.Create(
            "demo.tool", ToolErrorCategory.Validation, "invalid_args", apiCode: 123, trace: "abc", attempts: 3);
        validation.Retryable.Should().BeFalse("非 Retryable 分类不得标记可重试");
        validation.ApiCode.Should().Be(123);
        validation.Trace.Should().Be("abc", "catch 路径必须能携带追踪号（3 处拷贝中唯一的差异）");
        validation.Attempts.Should().Be(3, "重试路径必须能回填实际尝试次数");
    }

    /// <summary>把多行匹配折叠成单行，便于断言消息可读（匹配本身不受影响）。</summary>
    private static string Collapse(string value)
        => Regex.Replace(value, @"\s+", " ").Trim();
}
