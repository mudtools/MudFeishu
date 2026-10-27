// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// BUG-9 契约测试：业务事件会话键与聊天会话键隔离——同一 <c>(appKey, userId)</c> 的
/// 聊天历史与事件会话（审批任务 / 表格记录变更）必须落在不同键空间，双向防上下文污染。
/// </summary>
/// <remarks>
/// 守卫项（负例自证内置）：
/// <list type="bullet">
/// <item>聊天请求（EventKey 空）键构造与既有默认前缀逐字节一致（回归保护）。</item>
/// <item>业务事件（EventKey 非空）键带 <c>event-{EventKey}</c> 前缀，与聊天键不同。</item>
/// <item>负例自证以「默认前缀键的字面量」为独立基准，而不是拿生产代码的两个输出互比。</item>
/// <item>事件键可被 <c>ConversationKeyBuilder.TryParse</c> 解析且 round-trip 逐字节一致。</item>
/// </list>
/// </remarks>
public class ConversationKeyIsolationContractTests
{
    /// <summary>聊天会话（默认前缀）键的字面量基准——独立于被测代码，前缀逻辑丢失时本基准不变。</summary>
    private const string ChatKeyLiteral = "feishu:app-a:conversation:user:ou_1";

    private static ConversationRequest ChatRequest(string appKey, string userId)
        => new(AppKey: appKey, Scope: ConversationScope.P2P(), SubjectId: userId, SenderId: userId, MessageId: "om_1", MentionedText: "hi");

    private static ConversationRequest EventRequest(string appKey, string operatorId, string eventKey)
        => new(AppKey: appKey, Scope: ConversationScope.P2P(), SubjectId: operatorId, SenderId: operatorId, MessageId: string.Empty,
            MentionedText: "event", EventKey: eventKey);

    [Fact]
    public void BuildConversationKey_ShouldUseDefaultPrefix_WhenEventKeyIsEmpty()
    {
        var key = ConversationKeyPolicy.Build(ChatRequest("app-a", "ou_1"));

        key.Should().Be(ChatKeyLiteral,
            "聊天请求（EventKey 空）必须保持既有键形态——隔离只作用于业务事件，聊天会话语义不变");
    }

    [Fact]
    public void BuildConversationKey_ShouldCarryEventNamespace_WhenEventKeyIsPresent()
    {
        var key = ConversationKeyPolicy.Build(
            EventRequest("app-a", "ou_1", FeishuEventKeys.BitableRecordChanged));

        key.Should().Be("event-" + FeishuEventKeys.BitableRecordChanged + ":app-a:conversation:user:ou_1",
            "业务事件键前缀须注入事件命名空间（event-{EventKey}）");
    }

    [Fact]
    public void BuildConversationKey_ShouldIsolateEventFromChat_WhenSameAppKeyAndSubject()
    {
        var chatKey = ConversationKeyPolicy.Build(ChatRequest("app-a", "ou_1"));
        var approvalKey = ConversationKeyPolicy.Build(
            EventRequest("app-a", "ou_1", FeishuEventKeys.ApprovalTask));
        var bitableKey = ConversationKeyPolicy.Build(
            EventRequest("app-a", "ou_1", FeishuEventKeys.BitableRecordChanged));

        chatKey.Should().NotBe(approvalKey, "同一用户的聊天会话与审批事件会话必须互不可见（BUG-9）");
        chatKey.Should().NotBe(bitableKey, "同一用户的聊天会话与表格事件会话必须互不可见（BUG-9）");
        approvalKey.Should().NotBe(bitableKey, "不同事件族的会话同样隔离（事件类型即命名空间）");
    }

    /// <summary>
    /// 负例自证：前缀逻辑一旦丢失，事件键必然退化成<b>默认前缀字面量</b>。
    /// </summary>
    /// <remarks>
    /// 用固定字面量作独立基准（而不是 <c>BuildKey(无前缀)</c> 与 <c>BuildKey(无前缀)</c> 相比）：
    /// 后者的两边都由被测代码计算，恒等式成立与否与被测行为无关，测不出任何回归。
    /// </remarks>
    [Fact]
    public void BuildConversationKey_Isolation_ShouldBeDetectable_WhenPrefixIsRemoved()
    {
        var chatKey = ConversationKeyPolicy.Build(ChatRequest("app-a", "ou_1"));
        var approvalKey = ConversationKeyPolicy.Build(
            EventRequest("app-a", "ou_1", FeishuEventKeys.ApprovalTask));

        // 基准自证：默认前缀形态确实是字面量那条（否则下面两条断言都在拿错的东西比）。
        chatKey.Should().Be(ChatKeyLiteral, "默认前缀的字面量基准必须先成立，负例才有判别力");
        ConversationKeyPolicy.PrefixFor(eventKey: null).Should().BeNull("聊天请求不注入前缀段");

        approvalKey.Should().StartWith(
            ConversationKeyPolicy.EventNamespaceMarker + FeishuEventKeys.ApprovalTask + ":",
            "事件键必须以事件命名空间开头——前缀丢失即串号（BUG-9 回归）");
        approvalKey.Should().NotBe(ChatKeyLiteral, "事件键退化成默认前缀字面量即为串号形态");
    }

    [Fact]
    public void BuildConversationKey_ShouldRoundTrip_ThroughTryParse()
    {
        var key = ConversationKeyPolicy.Build(
            EventRequest("app-a", "ou_1", FeishuEventKeys.ApprovalTask));

        var parsed = ConversationKeyBuilder.TryParse(key, out var appKey, out var scope, out var subjectId, out var keyPrefix);

        parsed.Should().BeTrue("事件键仍是五段结构——隔离不改键位，回灌解析必须兼容");
        appKey.Should().Be("app-a");
        subjectId.Should().Be("ou_1");
        scope.IsGroup.Should().BeFalse();
        keyPrefix.Should().Be(ConversationKeyPolicy.EventNamespaceMarker + FeishuEventKeys.ApprovalTask);
        ConversationKeyBuilder.Build(appKey!, scope, subjectId!, keyPrefix).Should().Be(key,
            "解析结果再喂给 Build 应得到逐字节相同的键（回灌语义）");
    }

    /// <summary>
    /// 前缀注入不得破坏键结构：事件键含分隔符/通配符时按冻结契约转义，
    /// 且 <c>TryParse</c> 逐字节还原（安全属性：注入的是<b>前缀段内容</b>，不是新的键位）。
    /// </summary>
    [Theory]
    [InlineData("approval:task")]
    [InlineData("bitable.*")]
    [InlineData("a[b]?c")]
    public void BuildConversationKey_ShouldEscapeEventKey_AndRoundTrip(string eventKey)
    {
        var key = ConversationKeyPolicy.Build(EventRequest("app-a", "ou_1", eventKey));

        key.Should().Contain("\\", "事件键里的分隔符/通配符必须转义（否则键段被注入，Redis SCAN 模式也会被污染）");
        ConversationKeyBuilder.TryParse(key, out var appKey, out var scope, out var subjectId, out var keyPrefix)
            .Should().BeTrue("转义后仍是五段结构");
        appKey.Should().Be("app-a");
        subjectId.Should().Be("ou_1");
        scope.IsGroup.Should().BeFalse();
        keyPrefix.Should().Be(ConversationKeyPolicy.EventNamespaceMarker + eventKey, "前缀段必须原样还原");
    }

    /// <summary>超长事件键触发键段长度护栏（不是静默截断——截断会让两个事件族撞进同一会话）。</summary>
    [Fact]
    public void BuildConversationKey_ShouldRejectOverlongEventKey()
    {
        var act = () => ConversationKeyPolicy.Build(
            EventRequest("app-a", "ou_1", new string('x', 300)));

        act.Should().Throw<ArgumentException>("超长键段必须显式拒绝（截断即两个事件族串号）");
    }
}
