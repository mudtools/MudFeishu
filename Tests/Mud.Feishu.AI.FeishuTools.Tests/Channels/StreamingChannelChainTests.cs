// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Channels;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.FeishuTools.Channels;
using Mud.Feishu.AI.FeishuTools.Events;

namespace Mud.Feishu.AI.FeishuTools.Tests.Channels;

/// <summary>
/// 流式通道降级链测试（AI-FD-D12 P2D-2a）：卡片流能力不可用自动降级编辑通道（事件处理器零感知）、
/// 群聊跳过卡片流直走编辑通道、全部失败上抛、messageId 生命周期互斥。
/// </summary>
public class StreamingChannelChainTests
{
    [Fact]
    public async Task Begin_ShouldFallbackToEditChannel_WhenCardBeginFails()
    {
        var card = new Mock<IMessageChannel>();
        var edit = new Mock<IMessageChannel>();
        card.Setup(c => c.BeginAsync("appA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("卡片流能力未开通"));
        edit.Setup(c => c.BeginAsync("appA", "oc_group", It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_edit_1");

        var chain = new StreamingChannelChain(null, card.Object, edit.Object);
        var messageId = await chain.BeginAsync("appA", "oc_group");

        messageId.Should().Be("om_edit_1", "卡片流 Begin 失败自动降级编辑通道");
        card.Verify(c => c.BeginAsync("appA", It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        edit.Verify(c => c.BeginAsync("appA", "oc_group", It.IsAny<CancellationToken>()), Times.Once);

        // 降级后同一 messageId 生命周期定向到编辑通道（互斥，无混用态）。
        await chain.WriteStreamAsync("appA", "oc_group", "om_edit_1", "增量", CancellationToken.None);
        edit.Verify(c => c.WriteStreamAsync("appA", "oc_group", "om_edit_1", "增量", It.IsAny<CancellationToken>()), Times.Once);
        card.Verify(c => c.WriteStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Begin_ShouldSkipCardChannel_ForGroupEvent_ViaAmbientContext()
    {
        var card = new Mock<IMessageChannel>();
        var edit = new Mock<IMessageChannel>();
        edit.Setup(c => c.BeginAsync("appA", "oc_group", It.IsAny<CancellationToken>()))
            .ReturnsAsync("om_edit_2");

        var resolverCard = new Mock<IMessageChannel>();
        resolverCard.As<IMessageChannelTargetResolver>()
            .Setup(r => r.ResolveStreamTarget(It.IsAny<ConversationRequest>()))
            .Returns<ConversationRequest>(request => request.Scope.IsGroup ? null : request.SenderId);

        var chain = new StreamingChannelChain(null, resolverCard.Object, edit.Object);
        var request = new ConversationRequest(
            "appA", ConversationScope.Group(), "oc_group", "ou_sender", "om_1", null, ChatId: "oc_group");

        using var _ = StreamingRequestContext.Begin(request);
        var messageId = await chain.BeginAsync("appA", "oc_group");

        messageId.Should().Be("om_edit_2", "群聊卡片流目标解析为不适用 → 直接走编辑通道");
    }

    [Fact]
    public async Task Begin_ShouldUseCardTarget_ForP2pEvent_ViaAmbientContext()
    {
        var card = new Mock<IMessageChannel>();
        card.As<IMessageChannelTargetResolver>()
            .Setup(r => r.ResolveStreamTarget(It.IsAny<ConversationRequest>()))
            .Returns<ConversationRequest>(request => request.Scope.IsGroup ? null : request.SenderId);
        card.Setup(c => c.BeginAsync("appA", "ou_sender", It.IsAny<CancellationToken>()))
            .ReturnsAsync("biz_1");
        var edit = new Mock<IMessageChannel>();

        var chain = new StreamingChannelChain(null, card.Object, edit.Object);
        var request = new ConversationRequest(
            "appA", ConversationScope.P2P(), "ou_sender", "ou_sender", "om_1", null, ChatId: "oc_p2p");

        using var _ = StreamingRequestContext.Begin(request);
        var messageId = await chain.BeginAsync("appA", "oc_p2p");

        messageId.Should().Be("biz_1", "单聊：卡片流子通道按自身语义解析为发送者 open_id（P2D-2b 解锁单聊流式）");
        card.Verify(c => c.BeginAsync("appA", "ou_sender", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Begin_ShouldThrowLastException_WhenAllChannelsFail()
    {
        var card = new Mock<IMessageChannel>();
        var edit = new Mock<IMessageChannel>();
        card.Setup(c => c.BeginAsync("appA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("卡片失败"));
        edit.Setup(c => c.BeginAsync("appA", It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("编辑失败"));

        var chain = new StreamingChannelChain(null, card.Object, edit.Object);
        var act = async () => await chain.BeginAsync("appA", "oc_group");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*编辑失败*", "全部子通道失败上抛最后异常——事件处理器回退非流式路径");
    }

    /// <summary>
    /// R3-15：未登记的 messageId 必须 fail-fast——不得静默回退首选通道
    /// （回退会把"降级期由编辑通道承载的消息"的增量写到卡片流，属静默错投）。
    /// </summary>
    [Fact]
    public async Task WriteStream_ShouldThrow_WhenMessageIdNotRegistered()
    {
        var card = new Mock<IMessageChannel>();
        var edit = new Mock<IMessageChannel>();

        var chain = new StreamingChannelChain(null, card.Object, edit.Object);

        var write = async () => await chain.WriteStreamAsync(
            "appA", "oc_group", "om_never_begun", "增量", CancellationToken.None);
        var flush = async () => await chain.FlushAsync(
            "appA", "oc_group", "om_never_begun", CancellationToken.None);

        await write.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*未在本链登记*");
        await flush.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*未在本链登记*");

        card.Verify(
            c => c.WriteStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never, "未登记 messageId 不得落到任何子通道（含首选通道）");
        edit.Verify(
            c => c.WriteStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        card.Verify(
            c => c.FlushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        edit.Verify(
            c => c.FlushAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
