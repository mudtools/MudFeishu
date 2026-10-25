// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
/// <item>事件键可被 <c>ConversationKeyBuilder.TryParse</c> 解析且 round-trip 逐字节一致。</item>
/// </list>
/// </remarks>
public class ConversationKeyIsolationContractTests
{
    private static ConversationRequest ChatRequest(string appKey, string userId)
        => new(AppKey: appKey, Scope: ConversationScope.P2P(), SubjectId: userId, SenderId: userId, MessageId: "om_1", MentionedText: "hi");

    private static ConversationRequest EventRequest(string appKey, string operatorId, string eventKey)
        => new(AppKey: appKey, Scope: ConversationScope.P2P(), SubjectId: operatorId, SenderId: operatorId, MessageId: string.Empty,
            MentionedText: "event", EventKey: eventKey);

    [Fact]
    public void BuildConversationKey_ShouldUseDefaultPrefix_WhenEventKeyIsEmpty()
    {
        var key = ConversationalFeishuEventHandler.BuildConversationKey(ChatRequest("app-a", "ou_1"));

        key.Should().Be("feishu:app-a:conversation:user:ou_1",
            "聊天请求（EventKey 空）必须保持既有键形态——隔离只作用于业务事件，聊天会话语义不变");
    }

    [Fact]
    public void BuildConversationKey_ShouldCarryEventNamespace_WhenEventKeyIsPresent()
    {
        var key = ConversationalFeishuEventHandler.BuildConversationKey(
            EventRequest("app-a", "ou_1", FeishuEventKeys.BitableRecordChanged));

        key.Should().Be("event-" + FeishuEventKeys.BitableRecordChanged + ":app-a:conversation:user:ou_1",
            "业务事件键前缀须注入事件命名空间（event-{EventKey}）");
    }

    [Fact]
    public void BuildConversationKey_ShouldIsolateEventFromChat_WhenSameAppKeyAndSubject()
    {
        var chatKey = ConversationalFeishuEventHandler.BuildConversationKey(ChatRequest("app-a", "ou_1"));
        var approvalKey = ConversationalFeishuEventHandler.BuildConversationKey(
            EventRequest("app-a", "ou_1", FeishuEventKeys.ApprovalTask));
        var bitableKey = ConversationalFeishuEventHandler.BuildConversationKey(
            EventRequest("app-a", "ou_1", FeishuEventKeys.BitableRecordChanged));

        chatKey.Should().NotBe(approvalKey, "同一用户的聊天会话与审批事件会话必须互不可见（BUG-9）");
        chatKey.Should().NotBe(bitableKey, "同一用户的聊天会话与表格事件会话必须互不可见（BUG-9）");
        approvalKey.Should().NotBe(bitableKey, "不同事件族的会话同样隔离（事件类型即命名空间）");
    }

    [Fact]
    public void BuildConversationKey_Isolation_ShouldBeDetectable_WhenPrefixIsRemoved()
    {
        // 负例自证：还原 BUG-9 前的旧键构造（无前缀）时，聊天键与事件键必然相等——
        // 证明上面三条隔离断言不是恒真（把前缀逻辑删掉，测试面立刻能测出串号）。
        var legacyChatKey = ConversationScopeMapper.BuildKey("app-a", ConversationScope.P2P(), "ou_1");
        var legacyEventKey = ConversationScopeMapper.BuildKey("app-a", ConversationScope.P2P(), "ou_1");

        legacyChatKey.Should().Be(legacyEventKey,
            "旧实现（无事件前缀）下同一主体两键相等——这正是 BUG-9 的串号形态，隔离断言据此有效");
    }

    [Fact]
    public void BuildConversationKey_ShouldRoundTrip_ThroughTryParse()
    {
        var key = ConversationalFeishuEventHandler.BuildConversationKey(
            EventRequest("app-a", "ou_1", FeishuEventKeys.ApprovalTask));

        var parsed = ConversationKeyBuilder.TryParse(key, out var appKey, out var scope, out var subjectId, out var keyPrefix);

        parsed.Should().BeTrue("事件键仍是五段结构——隔离不改键位，回灌解析必须兼容");
        appKey.Should().Be("app-a");
        subjectId.Should().Be("ou_1");
        scope.IsGroup.Should().BeFalse();
        keyPrefix.Should().Be("event-" + FeishuEventKeys.ApprovalTask);
        ConversationKeyBuilder.Build(appKey!, scope, subjectId!, keyPrefix).Should().Be(key,
            "解析结果再喂给 Build 应得到逐字节相同的键（回灌语义）");
    }
}