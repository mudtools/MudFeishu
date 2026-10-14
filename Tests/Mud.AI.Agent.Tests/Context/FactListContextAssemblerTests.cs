// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Context;

namespace Mud.AI.Agent.Tests.Context;

/// <summary>
/// <see cref="FactListContextAssembler"/>（N4 · E5/E6 机制）的自有测试：
/// 事实列表机制（空值跳过 / 逐行扣预算 / 截断标记 / 条数闸 / untrusted 头）与钩子覆写。
/// </summary>
/// <remarks>产品子类（审批 / 多维表格）的行为覆盖在 AI.Tests 的 <c>ApprovalContextAssemblerTests</c>。</remarks>
public class FactListContextAssemblerTests
{
    /// <summary>最小产品子类替身：固定事件键 + 标签表。</summary>
    private sealed class TestAssembler(string eventKey = "test.event") : FactListContextAssembler(
        eventKey: eventKey,
        order: 200,
        totalLength: 200,
        fieldPreviewLength: 20,
        untrustedHeader: "[UNTRUSTED]",
        factLabels: new Dictionary<string, string>(StringComparer.Ordinal) { ["k1"] = "字段一" },
        hint: "- 建议: 用 test.tool 查详情。")
    {
        public string EventKey => eventKey;
    }

    private static ConversationRequest NewRequest(params (string Key, string? Value)[] facts)
        => NewRequest("test.event", facts);

    private static ConversationRequest NewRequest(string? eventKey, params (string Key, string? Value)[] facts)
        => new(
            AppKey: "t1",
            Scope: ConversationScope.Group(),
            SubjectId: "chat-1",
            SenderId: "user-1",
            MessageId: "m1",
            MentionedText: "问题",
            EventKey: eventKey,
            EventFacts: facts.Select(f => new EventFact(f.Key, f.Value)).ToList());

    [Fact]
    public async Task AssembleAsync_ShouldRenderLabelledLines_WithUntrustedHeader()
    {
        var fragment = await new TestAssembler().AssembleAsync(NewRequest(
            ("k1", "值一"),
            ("k2", "值二")));

        fragment.Should().StartWith("[UNTRUSTED]", "untrusted 标注是防提示注入的硬约束");
        fragment.Should().Contain("- 字段一: 值一", "登记键用展示名");
        fragment.Should().Contain("- k2: 值二", "未登记键原样展示键名");
        fragment.Should().Contain("- 建议: 用 test.tool 查详情。", "建议随片段输出");
    }

    [Fact]
    public async Task AssembleAsync_ShouldSkipEmptyValues_AndTruncateLongValues()
    {
        var fragment = await new TestAssembler().AssembleAsync(NewRequest(
            ("k1", null),
            ("k2", new string('长', 50))));

        fragment.Should().NotContain("k1", "缺字段降级：跳过空值");
        fragment.Should().Contain("…", "单行超预览上限 ⇒ 截断");
    }

    [Fact]
    public async Task AssembleAsync_ShouldReturnNull_ForOtherEventOrNoFacts()
    {
        (await new TestAssembler().AssembleAsync(NewRequest(eventKey: "other.event", ("k1", "v")))).Should().BeNull();
        (await new TestAssembler().AssembleAsync(NewRequest())).Should().BeNull("无载荷 ⇒ 无贡献");
        (await new TestAssembler().AssembleAsync(NewRequest(("k1", null)))).Should().BeNull("全部字段为空 ⇒ 等价无载荷");
    }

    [Fact]
    public void Constructor_ShouldRejectEmptyEventKey()
    {
        var act = () => new TestAssembler(eventKey: string.Empty);
        act.Should().Throw<ArgumentException>("事件族判别键是产品事实，必须显式注入");
    }
}
