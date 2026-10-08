// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.Abstractions.Conversations;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Tools.Channels;
using Mud.Feishu.DataModels.CardMessageStream;

namespace Mud.Feishu.AI.Tools.Tests.Channels;

/// <summary>
/// 应用消息卡片流通道测试（AI-FD-D12 P2D-2a）：Create→Update→终态全链、失败隔离、
/// 目标解析（单聊=发送者 open_id、群聊不适用）、租户上下文切换。
/// </summary>
public class CardStreamMessageChannelTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV2AppCardMessageStream> _cardClient = new();
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();

    public CardStreamMessageChannelTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new DisposableScope());
    }

    private sealed class DisposableScope : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private CardStreamMessageChannel CreateChannel(int chunkLength = 10, TimeSpan? interval = null)
        => new(
            _cardClient.Object,
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test", MaxStreamChunkLength = chunkLength }),
            NullLogger<CardStreamMessageChannel>.Instance,
            interval ?? TimeSpan.Zero);

    private void SetupCreateOk(string bizId = "biz_1")
        => _cardClient
            .Setup(c => c.CreateCardMessageStreamAsync(It.IsAny<CreateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateAppCardMessageStreamResult>
            {
                Code = 0,
                Data = new CreateAppCardMessageStreamResult { BizId = bizId },
            });

    private void SetupUpdateOk()
        => _cardClient
            .Setup(c => c.UpdateCardMessageStreamAsync(It.IsAny<UpdateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<UpdateAppCardMessageStreamResult>
            {
                Code = 0,
                Data = new UpdateAppCardMessageStreamResult(),
            });

    [Fact]
    public async Task Begin_ShouldCreateCardWithReceiverUser_AndReturnBizId()
    {
        SetupCreateOk("biz_stream_1");
        CreateAppCardMessageStreamRequest? captured = null;
        _cardClient
            .Setup(c => c.CreateCardMessageStreamAsync(It.IsAny<CreateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((CreateAppCardMessageStreamRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<CreateAppCardMessageStreamResult>
            {
                Code = 0,
                Data = new CreateAppCardMessageStreamResult { BizId = "biz_stream_1" },
            });

        var bizId = await CreateChannel().BeginAsync("appA", "ou_receiver");

        bizId.Should().Be("biz_stream_1");
        captured.Should().NotBeNull();
        captured!.UserIds.Should().ContainSingle("应用消息卡片按用户 open_id 投放").Which.Should().Be("ou_receiver");
        captured.AppMessageCard!.Preview.Should().NotBeNullOrEmpty("占位卡片须有初始预览");
    }

    [Fact]
    public async Task Begin_ShouldThrow_WhenApiFails_SoChainCanFallback()
    {
        _cardClient
            .Setup(c => c.CreateCardMessageStreamAsync(It.IsAny<CreateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateAppCardMessageStreamResult> { Code = 99991672, Msg = "卡片流能力未开通" });

        var act = async () => await CreateChannel().BeginAsync("appA", "ou_receiver");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*99991672*", "Begin 失败向上抛——降级链尝试编辑通道 / 事件处理器回退非流式");
    }

    [Fact]
    public async Task WriteStream_AndFlush_ShouldUpdatePreviewWithCumulativeText()
    {
        SetupCreateOk();
        SetupUpdateOk();
        var previews = new List<string?>();
        var userIds = new List<string>();
        _cardClient
            .Setup(c => c.UpdateCardMessageStreamAsync(It.IsAny<UpdateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((UpdateAppCardMessageStreamRequest request, string _, CancellationToken __) =>
            {
                var card = request.FeedCards![0];
                previews.Add(card.AppMessageCard!.Preview);
                userIds.Add(card.UserId);
            })
            .ReturnsAsync(new FeishuApiResult<UpdateAppCardMessageStreamResult>
            {
                Code = 0,
                Data = new UpdateAppCardMessageStreamResult(),
            });

        var channel = CreateChannel(chunkLength: 10);
        await channel.BeginAsync("appA", "ou_receiver");
        await channel.WriteStreamAsync("appA", "oc_1", "biz_1", "0123456789", CancellationToken.None);
        await channel.WriteStreamAsync("appA", "oc_1", "biz_1", "abc", CancellationToken.None);
        await channel.FlushAsync("appA", "oc_1", "biz_1", CancellationToken.None);

        previews.Should().HaveCount(3, "达到分片阈值更新两次，Flush 落地最终全文（与编辑通道缓冲语义同构）");
        previews[0].Should().Be("0123456789");
        previews[1].Should().Be("0123456789abc");
        previews[2].Should().Be("0123456789abc");
        userIds.Should().OnlyContain(u => u == "ou_receiver", "更新按 Begin 登记的投放用户执行");
    }

    [Fact]
    public async Task UpdateFailure_ShouldNotThrow_FailureIsolation()
    {
        SetupCreateOk();
        _cardClient
            .Setup(c => c.UpdateCardMessageStreamAsync(It.IsAny<UpdateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("更新接口 5xx"));

        var channel = CreateChannel(chunkLength: 1);
        await channel.BeginAsync("appA", "ou_receiver");

        var act = async () => await channel.WriteStreamAsync("appA", "oc_1", "biz_1", "abc", CancellationToken.None);
        await act.Should().NotThrowAsync("单次更新失败不中断模型流（失败隔离，接口契约）");
    }

    [Fact]
    public void ResolveStreamTarget_ShouldReturnSenderOpenId_ForP2p_AndNullForGroup()
    {
        var channel = CreateChannel();

        channel.ResolveStreamTarget(new ConversationRequest(
            "appA", ConversationScope.P2P(), "ou_user", "ou_sender", "om_1", null)).Should().Be("ou_sender",
            "单聊：应用消息卡片投放对象 = 发送者 open_id（P2D-2a 目标语义）");

        channel.ResolveStreamTarget(new ConversationRequest(
            "appA", ConversationScope.Group(), "oc_group", "ou_sender", "om_1", null)).Should().BeNull(
            "群聊：卡片流不适用，降级链转编辑通道");
    }

    [Fact]
    public async Task Channel_ShouldSwitchTenantScope_PerApiCall()
    {
        SetupCreateOk();
        SetupUpdateOk();
        var scopeLog = new List<string>();
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Callback((string appKey) => scopeLog.Add($"scope:{appKey}"))
            .Returns(new DisposableScope());

        var channel = CreateChannel(chunkLength: 1);
        await channel.BeginAsync("appA", "ou_receiver");
        await channel.WriteStreamAsync("appA", "oc_1", "biz_1", "abc", CancellationToken.None);

        scopeLog.Should().OnlyContain(s => s == "scope:appA", "每次下游调用前经 BeginScope 切换租户上下文（TMA2-20）");
        scopeLog.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Update_ShouldApplyMinInterval_WhenRateClamped()
    {
        SetupCreateOk();
        SetupUpdateOk();
        var updateCount = 0;
        _cardClient
            .Setup(c => c.UpdateCardMessageStreamAsync(It.IsAny<UpdateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback(() => updateCount++)
            .ReturnsAsync(new FeishuApiResult<UpdateAppCardMessageStreamResult>
            {
                Code = 0,
                Data = new UpdateAppCardMessageStreamResult(),
            });

        // 生产默认 800ms 最小间隔：分片阈值快速越限的第二次写入应被钳制缓冲（P2D-2c）。
        var channel = CreateChannel(chunkLength: 1, interval: BufferedMessageChannel.MinUpdateInterval);
        await channel.BeginAsync("appA", "ou_receiver");
        await channel.WriteStreamAsync("appA", "oc_1", "biz_1", "a", CancellationToken.None);
        await channel.WriteStreamAsync("appA", "oc_1", "biz_1", "b", CancellationToken.None);

        updateCount.Should().Be(1, "两次下游更新间隔 < 800ms 时继续缓冲（速率自适应钳制）");

        await channel.FlushAsync("appA", "oc_1", "biz_1", CancellationToken.None);
        updateCount.Should().Be(2, "Flush 终态无条件落地");
    }
}
