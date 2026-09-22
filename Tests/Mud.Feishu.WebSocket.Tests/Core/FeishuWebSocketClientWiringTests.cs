// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Services;
using Mud.Feishu.WebSocket.Handlers;

namespace Mud.Feishu.WebSocket.Tests.Core;

/// <summary>
/// <see cref="FeishuWebSocketClient"/> 的装配（wire-up）与重连状态重置回归测试。
/// </summary>
/// <remarks>
/// 覆盖审查报告点名的两处缺口：
/// <list type="number">
/// <item><b>去重器装配</b>：<c>FeishuWebSocketClient</c> 曾把 <c>null</c> 传给事件处理器，
/// 导致 <c>EventDeduplication</c> 配置（默认 InMemory）在 WebSocket 路径上**完全失效**，
/// 服务端重发事件必然重复消费（历史 P1-5）；</item>
/// <item><b>重连状态重置</b>：重连后必须清空序号验证器 / 二进制半包 / SeqID 缓存，
/// 否则新连接的首条消息会命中旧连接的残留状态（曾因"重置晚于首帧"而存在竞态）。</item>
/// </list>
/// <para>
/// <b>为什么用结构断言 + 行为断言组合</b>：客户端在构造函数内直接 <c>new MessageRouter(...)</c> 与
/// <c>new FeishuEventMessageHandler(...)</c>，<b>没有任何注入点</b>，无法用 Moq 拦截；
/// 因此"是否传入了同一个去重器实例"只能通过读取私有字段来断言（<c>Assert.Same</c> 语义），
/// 而"去重是否真的生效"由既有 <c>FeishuEventMessageHandlerTests</c> 的行为用例覆盖。
/// 二者组合才闭环：前者防"传 null 回归"，后者防"去重逻辑回归"。
/// </para>
/// </remarks>
public class FeishuWebSocketClientWiringTests
{
    private static readonly MethodInfo ResetStateOnReconnectAsyncMethod =
        typeof(FeishuWebSocketClient).GetMethod("ResetStateOnReconnectAsync",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo MessageRouterField =
        typeof(FeishuWebSocketClient).GetField("_messageRouter", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo HandlersField =
        typeof(MessageRouter).GetField("_handlers", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static readonly FieldInfo DeduplicatorField =
        typeof(FeishuEventMessageHandler).GetField("_deduplicator", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static FeishuWebSocketClient CreateClient(
        IFeishuEventDeduplicator? deduplicator = null,
        IFeishuSeqIDDeduplicator? seqIdDeduplicator = null,
        MessageSequenceValidator? validator = null)
    {
        var factoryMock = new Mock<IFeishuEventHandlerFactory>();
        factoryMock.Setup(x => x.GetHandler(It.IsAny<string>())).Returns(Mock.Of<IFeishuEventHandler>());
        factoryMock.Setup(x => x.GetHandlers(It.IsAny<string>())).Returns(new List<IFeishuEventHandler>());

        return new FeishuWebSocketClient(
            NullLogger<FeishuWebSocketClient>.Instance,
            factoryMock.Object,
            NullLoggerFactory.Instance,
            eventDeduplicator: deduplicator,
            options: new FeishuWebSocketOptions(),
            seqIdDeduplicator: seqIdDeduplicator,
            sequenceValidator: validator);
    }

    private static FeishuEventMessageHandler FindEventMessageHandler(FeishuWebSocketClient client)
    {
        var router = (MessageRouter)MessageRouterField.GetValue(client)!;
        var handlers = (List<IMessageHandler>)HandlersField.GetValue(router)!;

        return handlers.OfType<FeishuEventMessageHandler>().Single();
    }

    /// <summary>
    /// 历史 P1-5 回归点：注入的事件去重器必须**原样**传给 <see cref="FeishuEventMessageHandler"/>。
    /// </summary>
    [Fact]
    public void FeishuWebSocketClient_ShouldWireEventDeduplicator_IntoEventHandler()
    {
        // Arrange
        var deduplicator = new Mock<IFeishuEventDeduplicator>().Object;

        // Act
        var client = CreateClient(deduplicator: deduplicator);

        // Assert
        var handler = FindEventMessageHandler(client);
        var wired = DeduplicatorField.GetValue(handler);

        wired.Should().BeSameAs(deduplicator,
            "EventDeduplication 配置在 WebSocket 路径上必须真实生效——" +
            "改造前此处恒为 null，服务端重发事件必然被重复消费（历史 P1-5）");
    }

    /// <summary>
    /// 未注入去重器时必须是 <c>null</c>（而不是某个隐式默认实现），保持"显式配置才生效"的语义。
    /// </summary>
    [Fact]
    public void FeishuWebSocketClient_ShouldLeaveDeduplicatorNull_WhenNotInjected()
    {
        var handler = FindEventMessageHandler(CreateClient());

        DeduplicatorField.GetValue(handler).Should().BeNull(
            "去重器未注入时应为 null（由统一去重中间件或工厂装配层负责），不得静默替换为其它实现");
    }

    /// <summary>
    /// 重连状态重置：序号验证器必须被清空，使"上一连接见过的序号"在新连接上重新可处理。
    /// </summary>
    /// <remarks>
    /// 用**可观测行为**断言而非"调用了 Reset 方法"：先让验证器记住 42，
    /// 重置后同序号必须再次被判为 <c>Valid</c>（若未重置会返回 Duplicate）。
    /// </remarks>
    [Fact]
    public async Task ResetStateOnReconnect_ShouldResetValidatorProcessorAndSeqIdCache()
    {
        // Arrange
        var validator = new MessageSequenceValidator(NullLogger<MessageSequenceValidator>.Instance, new FeishuWebSocketOptions());
        validator.ValidateSequence(42).Should().Be(SequenceValidationResult.Valid, "前置条件：首次见到 42");

        var seqIdDeduplicator = new Mock<IFeishuSeqIDDeduplicator>();
        seqIdDeduplicator.Setup(x => x.ClearCacheAsync()).Returns(Task.CompletedTask);

        var client = CreateClient(seqIdDeduplicator: seqIdDeduplicator.Object, validator: validator);

        // Act
        await (Task)ResetStateOnReconnectAsyncMethod.Invoke(client, null)!;

        // Assert ①：序号验证器已重置（游标与滑动窗口一并清空）
        validator.ValidateSequence(42).Should().Be(SequenceValidationResult.Valid,
            "重连后新连接的序号序列重新开始，旧连接的游标必须被清空（否则首条消息会命中 Duplicate）");

        // Assert ②：SeqID 去重缓存已清空（且只清一次）
        seqIdDeduplicator.Verify(x => x.ClearCacheAsync(), Times.Once,
            "重连必须清空 SeqID 去重缓存，否则新连接的首批帧会被旧连接的记录误判为重复");
    }
}
