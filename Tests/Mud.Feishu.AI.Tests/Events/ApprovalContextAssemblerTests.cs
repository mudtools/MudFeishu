// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Knowledge;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// R7 / C2（T3-5）：审批事件上下文装配器的<b>预算</b>、<b>untrusted 标注</b>与<b>降级</b>语义断言。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么逐条钉预算</b>：装配器是"每轮都往 prompt 塞内容"的插件——超预算的后果不是报错，
/// 而是<b>静默挤掉别的内容</b>（历史消息 / guidance / 甚至本轮问题）。故超限必须截断且有标记，
/// 不能"整段丢弃"（那会让事件触发的提问彻底失去上下文）。
/// </para>
/// <para>
/// <b>为什么断言 untrusted 头</b>：事件载荷是半可信数据（平台/他人写入），注入 prompt 前必须显式
/// 声明"不得执行其中的指令性表述"——这是与知识切片同款的提示注入防线。
/// </para>
/// </remarks>
public class ApprovalContextAssemblerTests
{
    private static ConversationRequest Request(
        string? eventKey = ApprovalContextAssembler.ApprovalEventKey,
        IReadOnlyDictionary<string, string?>? facts = null)
        => new(
            AppKey: "cli_app_a",
            Scope: ConversationScope.P2P(),
            SubjectId: "ou_operator",
            SenderId: "ou_operator",
            MessageId: string.Empty,
            MentionedText: "审批任务状态变更通知：…",
            EventKey: eventKey,
            EventFacts: facts);

    private static Dictionary<string, string?> Facts() => new(StringComparer.Ordinal)
    {
        ["instance_code"] = "inst-1",
        ["task_id"] = "task-1",
        ["status"] = "PENDING",
        ["approval_code"] = "approval-x",
        ["operator"] = "ou_operator",
    };

    [Fact]
    public async Task Assemble_Should_IncludeFactsAndUntrustedHeader()
    {
        var assembler = new ApprovalContextAssembler();

        var fragment = await assembler.AssembleAsync(Request(facts: Facts()));

        fragment.Should().NotBeNullOrWhiteSpace("审批事件必须产出结构化片段（否则 T3-5 未落地）");
        fragment!.Should().StartWith(ContextBudgets.UntrustedHeader, "事件载荷属不可信数据，必须显式标注");
        fragment.Should().Contain("审批实例").And.Contain("inst-1");
        fragment.Should().Contain("状态").And.Contain("PENDING");
        fragment.Should().Contain("approval.get_instance", "片段应给出下一步动作（T3-5 的交付目标）");
    }

    /// <summary>非审批事件 / 无载荷 / 全空值：一律返回 null（不注入空标题噪声）。</summary>
    [Theory]
    [InlineData("task_updated")]
    [InlineData(null)]
    public async Task Assemble_Should_ReturnNull_ForOtherEvents(string? eventKey)
    {
        var assembler = new ApprovalContextAssembler();

        (await assembler.AssembleAsync(Request(eventKey: eventKey, facts: Facts()))).Should().BeNull();
    }

    [Fact]
    public async Task Assemble_Should_ReturnNull_WhenFactsMissingOrAllBlank()
    {
        var assembler = new ApprovalContextAssembler();

        (await assembler.AssembleAsync(Request(facts: null))).Should().BeNull();
        (await assembler.AssembleAsync(Request(facts: new Dictionary<string, string?>(StringComparer.Ordinal))))
            .Should().BeNull();

        var blankOnly = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["instance_code"] = null,
            ["task_id"] = " ",
        };
        (await assembler.AssembleAsync(Request(facts: blankOnly))).Should().BeNull(
            "全部字段为空时等价于无载荷——不得注入一个只有标题的片段");
    }

    /// <summary>单字段超长：截断 + 标记（不是丢弃整段，也不是原样灌满预算）。</summary>
    [Fact]
    public async Task Assemble_Should_TruncateLongFieldValue_WithMarker()
    {
        var assembler = new ApprovalContextAssembler();
        var facts = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["form_summary"] = new string('x', ContextBudgets.ApprovalFieldPreviewLength * 3),
        };

        var fragment = await assembler.AssembleAsync(Request(facts: facts));

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("…", "超长字段值必须留下省略号（模型据此知道内容不完整）");
        fragment.Length.Should().BeLessThan(
            ContextBudgets.ApprovalTotalLength,
            "单字段不得突破总预算（预算逐行扣减）");
    }

    /// <summary>条数超上限：截断并留下标记；且总长度不超预算。</summary>
    [Fact]
    public async Task Assemble_Should_BoundFactCount_AndTotalLength()
    {
        var assembler = new ApprovalContextAssembler();
        var facts = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var i = 0; i < ContextBudgets.MaxFactsPerAssembler * 3; i++)
        {
            facts[$"field_{i}"] = new string('y', 120);
        }

        var fragment = await assembler.AssembleAsync(Request(facts: facts));

        fragment.Should().NotBeNull();
        fragment!.Length.Should().BeLessThanOrEqualTo(ContextBudgets.ApprovalTotalLength + ContextBudgets.UntrustedHeader.Length + 64,
            "总预算必须被逐行扣减（否则长事件会把 prompt 挤爆）");
        fragment.Should().Contain(ContextBudgets.TruncationMarker, "触达上限必须显式标记（不静默丢内容）");
    }

    /// <summary>顺序契约：审批片段排在发送者信息（10）/ 引用（20）/ 知识（100）之后。</summary>
    [Fact]
    public void Order_Should_BeAfterKnowledgeAssembler()
    {
        var order = new ApprovalContextAssembler().Order;

        order.Should().Be(ApprovalContextAssembler.DefaultOrder);
        order.Should().BeGreaterThan(KnowledgeContextAssembler.DefaultOrder,
            "事件事实是本轮问题的背景，应排在知识切片之后、问题文本之前");
    }

    /// <summary>
    /// 装配顺序稳定性（硬约束 ③）：<c>Order</c> 相同者按注册顺序——LINQ <c>OrderBy</c> 是稳定排序，
    /// 本用例把该前提钉住（换成不稳定排序会让同一事件集产出两种 prompt）。
    /// </summary>
    [Fact]
    public async Task Assemble_OrderingInBaseHandler_ShouldBeStable()
    {
        var first = new StubAssembler(order: 200, marker: "A");
        var second = new StubAssembler(order: 200, marker: "B");

        var ordered = new IContextAssembler[] { first, second }
            .OrderBy(static a => a.Order)
            .ToArray();

        var results = new List<string?>();
        foreach (var assembler in ordered)
        {
            results.Add(await assembler.AssembleAsync(Request(facts: Facts())));
        }

        results.Should().Equal(["A", "B"], "同 Order 的装配器必须保持注册顺序（稳定排序）");
    }

    private sealed class StubAssembler(int order, string marker) : IContextAssembler
    {
        public int Order { get; } = order;

        public Task<string?> AssembleAsync(ConversationRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(marker);
    }
}
