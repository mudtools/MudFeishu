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
/// R7 / C2：多维表格记录变更上下文装配器——<b>只含变化字段</b>、预算截断、平台枚举码转中文与降级语义。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么单列这一组</b>：事件载荷里 <c>before_value</c>/<c>after_value</c> 都是<b>整行</b>字段数组，
/// 直接灌进 prompt 会同时吃掉预算并淹没"到底哪个字段变了"。所以本装配器唯一的重要不变量是
/// "只有生产者登记的变化字段才出现"——而这条不变量只要有人图省事改成"全量灌入"就会静默失效，
/// 故用用例钉住。
/// </para>
/// </remarks>
public class BitableRecordContextAssemblerTests
{
    private static ConversationRequest Request(
        string? eventKey = FeishuEventKeys.BitableRecordChanged,
        IReadOnlyList<FeishuEventFact>? facts = null)
        => new(
            AppKey: "cli_app_a",
            Scope: ConversationScope.P2P(),
            SubjectId: "ou_operator",
            SenderId: "ou_operator",
            MessageId: string.Empty,
            MentionedText: "多维表格记录变更通知：…",
            EventKey: eventKey,
            EventFacts: facts);

    private static List<FeishuEventFact> Facts() =>
    [
        new(BitableRecordContextAssembler.FileTokenKey, "bascnTbl123"),
        new(BitableRecordContextAssembler.TableIdKey, "tblABC"),
        new(BitableRecordContextAssembler.RevisionKey, "12"),
        new(BitableRecordContextAssembler.OperatorKey, "ou_operator"),
        new(BitableRecordContextAssembler.ActionPrefix + "0", "record_edited rec1"),
        new(BitableRecordContextAssembler.DiffPrefix + "0.0", "fldA: \"甲\" → \"乙\""),
    ];

    [Fact]
    public async Task Assemble_Should_IncludeFactsDiffAndUntrustedHeader()
    {
        var fragment = await new BitableRecordContextAssembler().AssembleAsync(Request(facts: Facts()));

        fragment.Should().NotBeNullOrWhiteSpace("记录变更事件必须产出结构化片段（否则 C2 未覆盖该域）");
        fragment!.Should().StartWith(ContextBudgets.UntrustedHeader, "表格字段值是他人可写内容，必须显式标注");
        fragment.Should().Contain("多维表格").And.Contain("bascnTbl123");
        fragment.Should().Contain("数据表").And.Contain("tblABC");
        fragment.Should().Contain("版本号").And.Contain("12");
        fragment.Should().Contain("操作人").And.Contain("ou_operator");
        fragment.Should().Contain("修改记录 rec1", "平台枚举码必须翻成模型可读的中文");
        fragment.Should().Contain("字段差异").And.Contain("fldA");
        fragment.Should().Contain("bitable.get_records_by_ids", "片段应给出下一步动作");
    }

    /// <summary>平台三态枚举码全覆盖：新增 / 删除 / 修改。</summary>
    [Theory]
    [InlineData("record_added", "新增记录 rec9")]
    [InlineData("record_deleted", "删除记录 rec9")]
    [InlineData("record_edited", "修改记录 rec9")]
    public async Task Assemble_Should_TranslateActionCodes(string action, string expected)
    {
        var facts = new List<FeishuEventFact>
        {
            new(BitableRecordContextAssembler.ActionPrefix + "0", $"{action} rec9"),
        };

        var fragment = await new BitableRecordContextAssembler().AssembleAsync(Request(facts: facts));

        fragment.Should().NotBeNull();
        fragment!.Should().Contain(expected);
    }

    /// <summary>未知枚举码不得被吞掉（原样展示优于静默丢弃——否则模型看不到发生了变更）。</summary>
    [Fact]
    public async Task Assemble_Should_KeepUnknownActionCode()
    {
        var facts = new List<FeishuEventFact>
        {
            new(BitableRecordContextAssembler.ActionPrefix + "0", "record_teleported rec9"),
        };

        var fragment = await new BitableRecordContextAssembler().AssembleAsync(Request(facts: facts));

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("record_teleported rec9");
    }

    /// <summary>非本事件族 / 无载荷 / 全空值：一律 null（不注入噪声）。</summary>
    [Theory]
    [InlineData(FeishuEventKeys.ApprovalTask)]
    [InlineData("task_updated")]
    [InlineData(null)]
    public async Task Assemble_Should_ReturnNull_ForOtherEvents(string? eventKey)
    {
        (await new BitableRecordContextAssembler().AssembleAsync(Request(eventKey: eventKey, facts: Facts())))
            .Should().BeNull();
    }

    [Fact]
    public async Task Assemble_Should_ReturnNull_WhenFactsMissingOrAllBlank()
    {
        var assembler = new BitableRecordContextAssembler();

        (await assembler.AssembleAsync(Request(facts: null))).Should().BeNull();
        (await assembler.AssembleAsync(Request(facts: []))).Should().BeNull();

        var blankOnly = new List<FeishuEventFact>
        {
            new(BitableRecordContextAssembler.FileTokenKey, null),
            new(BitableRecordContextAssembler.TableIdKey, " "),
        };
        (await assembler.AssembleAsync(Request(facts: blankOnly))).Should().BeNull(
            "全部字段为空时等价于无载荷——不得注入一个只有标题的片段");
    }

    /// <summary>单行超长（字段值是 JSON 字符串，可能很长）：截断 + 省略号，且不突破总预算。</summary>
    [Fact]
    public async Task Assemble_Should_TruncateLongFieldValue_WithMarker()
    {
        var facts = new List<FeishuEventFact>
        {
            new(
                BitableRecordContextAssembler.DiffPrefix + "0.0",
                "fldA: " + new string('x', ContextBudgets.BitableRecordFieldPreviewLength * 4)),
        };

        var fragment = await new BitableRecordContextAssembler().AssembleAsync(Request(facts: facts));

        fragment.Should().NotBeNull();
        fragment!.Should().Contain("…", "超长字段值必须留下省略号（模型据此知道内容不完整）");
        fragment.Length.Should().BeLessThan(
            ContextBudgets.BitableRecordTotalLength,
            "单行不得突破总预算（预算逐行扣减）");
    }

    /// <summary>行数超上限（批量导入会一次带很多条）：截断并留标记。</summary>
    [Fact]
    public async Task Assemble_Should_BoundFactCount_AndTotalLength()
    {
        var facts = new List<FeishuEventFact>();
        for (var i = 0; i < ContextBudgets.MaxFactsPerAssembler * 3; i++)
        {
            facts.Add(new FeishuEventFact(BitableRecordContextAssembler.DiffPrefix + $"0.{i}", $"fld{i}: 甲 → 乙"));
        }

        var fragment = await new BitableRecordContextAssembler().AssembleAsync(Request(facts: facts));

        fragment.Should().NotBeNull();
        fragment!.Length.Should().BeLessThanOrEqualTo(
            ContextBudgets.BitableRecordTotalLength + ContextBudgets.UntrustedHeader.Length + 64,
            "总预算必须被逐行扣减（否则一次批量导入就能把 prompt 挤爆）");
        fragment.Should().Contain(ContextBudgets.TruncationMarker, "触达上限必须显式标记（不静默丢内容）");
    }

    /// <summary>装配顺序：与审批装配器同级（事件事实是本轮问题的背景）。</summary>
    [Fact]
    public void Order_Should_BeAfterKnowledgeAssembler()
    {
        var order = new BitableRecordContextAssembler().Order;

        order.Should().Be(BitableRecordContextAssembler.DefaultOrder);
        order.Should().BeGreaterThan(KnowledgeContextAssembler.DefaultOrder);
        order.Should().Be(new ApprovalContextAssembler().Order, "同一族的业务事件装配器保持同级顺序");
    }
}
