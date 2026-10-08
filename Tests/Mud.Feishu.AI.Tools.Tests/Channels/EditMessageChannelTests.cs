// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Mud.Feishu.AI.Tools.Channels;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.Tools.Tests.Channels;

/// <summary>
/// 分片编辑流式通道测试（Phase 2 §3.1）：占位消息创建、分片缓冲编辑（累计全文）、
/// 失败隔离（单次编辑失败不中断）、Flush 收尾、租户上下文切换。
/// </summary>
public class EditMessageChannelTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();
    private readonly List<string> _scopeLog = [];

    public EditMessageChannelTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Callback((string appKey) => _scopeLog.Add($"scope:{appKey}"))
            .Returns(DisposableScope);
    }

    private static IDisposable DisposableScope() => new ScopedRelease();

    private sealed class ScopedRelease : IDisposable
    {
        public void Dispose()
        {
            // 作用域释放无外部可观察行为（租户上下文切换断言经 scope 日志覆盖）。
        }
    }

    private EditMessageChannel CreateChannel(int chunkLength = 200)
        => new(
            _messageClient.Object,
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test", MaxStreamChunkLength = chunkLength }),
            NullLogger<EditMessageChannel>.Instance,
            // 默认 0 间隔：恢复「仅分片阈值」语义（生产默认 800ms 由速率钳制用例单独覆盖）。
            TimeSpan.Zero);

    private EditMessageChannel CreateRateClampedChannel(int chunkLength = 200)
        => new(
            _messageClient.Object,
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test", MaxStreamChunkLength = chunkLength }),
            NullLogger<EditMessageChannel>.Instance);

    private void SetupSendMessageOk(string messageId = "om_stream_1")
        => _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), "chat_id", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = messageId },
            });

    private void SetupEditOk()
        => _messageClient
            .Setup(c => c.EditMessageAsync(It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_stream_1" },
            });

    private static string? CapturedText(string? content)
    {
        var json = JsonDocument.Parse(content!);
        return json.RootElement.GetProperty("text").GetString();
    }

    [Fact]
    public async Task Begin_ShouldCreatePlaceholderTextMessage_AndReturnMessageId()
    {
        SetupSendMessageOk("om_stream_1");
        SendMessageRequest? captured = null;
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), "chat_id", It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string _, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_stream_1" },
            });

        var messageId = await CreateChannel().BeginAsync("appA", "oc_group1");

        messageId.Should().Be("om_stream_1");
        captured.Should().NotBeNull();
        captured!.ReceiveId.Should().Be("oc_group1");
        captured.MsgType.Should().Be("text");
        CapturedText(captured!.Content).Should().NotBeNullOrEmpty("占位消息须有初始内容（空文本会被飞书拒绝）");
    }

    [Fact]
    public async Task Begin_ShouldThrow_WhenApiFails_SoHandlerCanFallback()
    {
        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 230001, Msg = "无发送权限" });

        var act = async () => await CreateChannel().BeginAsync("appA", "oc_group1");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*230001*", "Begin 失败向上抛——事件处理器回退非流式（模型尚未调用，零重复成本）");
    }

    [Fact]
    public async Task WriteStream_ShouldBufferByChunkLength_AndEditWithCumulativeText()
    {
        SetupSendMessageOk();
        var edits = new List<string?>();
        _messageClient
            .Setup(c => c.EditMessageAsync(It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, EditMessageRequest request, CancellationToken __) => edits.Add(CapturedText(request.Content)))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_stream_1" },
            });

        var channel = CreateChannel(chunkLength: 10);
        await channel.BeginAsync("appA", "oc_group1");

        await channel.WriteStreamAsync("appA", "oc_group1", "om_stream_1", "0123456789", CancellationToken.None); // 10 < 10? = 10 → 编辑
        await channel.WriteStreamAsync("appA", "oc_group1", "om_stream_1", "abc", CancellationToken.None);          // 13 ≥ 10 → 编辑
        await channel.FlushAsync("appA", "oc_group1", "om_stream_1", CancellationToken.None);                       // 收尾编辑

        edits.Should().HaveCount(3, "达到分片阈值即编辑一次，Flush 落地最终全文");
        edits[0].Should().Be("0123456789");
        edits[1].Should().Be("0123456789abc", "编辑内容为累计全文（Edit 为整体替换语义）");
        edits[2].Should().Be("0123456789abc");
    }

    [Fact]
    public async Task WriteStream_ShouldNotEdit_BeforeThreshold()
    {
        SetupSendMessageOk();
        SetupEditOk();

        var channel = CreateChannel(chunkLength: 100);
        await channel.BeginAsync("appA", "oc_group1");
        await channel.WriteStreamAsync("appA", "oc_group1", "om_stream_1", "短增量", CancellationToken.None);

        _messageClient.Verify(
            c => c.EditMessageAsync(It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never, "未达分片阈值不触发编辑（避免逐 token 编辑触发飞书频率限制）");
    }

    [Fact]
    public async Task WriteStream_ShouldIsolateEditFailures_WithoutBreakingStream()
    {
        SetupSendMessageOk();
        _messageClient
            .Setup(c => c.EditMessageAsync(It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("编辑接口 5xx"));

        var channel = CreateChannel(chunkLength: 1);
        await channel.BeginAsync("appA", "oc_group1");

        var act = async () => await channel.WriteStreamAsync("appA", "oc_group1", "om_stream_1", "abc", CancellationToken.None);
        await act.Should().NotThrowAsync("单次分片编辑失败不中断模型流（失败隔离，接口契约）");
    }

    [Fact]
    public async Task Channel_ShouldSwitchTenantScope_PerApiCall()
    {
        SetupSendMessageOk();
        SetupEditOk();

        var channel = CreateChannel(chunkLength: 1);
        await channel.BeginAsync("appA", "oc_group1");
        await channel.WriteStreamAsync("appA", "oc_group1", "om_stream_1", "abc", CancellationToken.None);

        _scopeLog.Should().OnlyContain(s => s == "scope:appA", "每次下游调用前经 BeginScope 切换租户上下文（TMA2-20）");
        _scopeLog.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    [Fact]
    public async Task Flush_ShouldDeliverFinalText_EvenWhenNoDeltaArrived()
    {
        SetupSendMessageOk();
        SetupEditOk();

        var channel = CreateChannel();
        await channel.BeginAsync("appA", "oc_group1");
        await channel.FlushAsync("appA", "oc_group1", "om_stream_1", CancellationToken.None);

        _messageClient.Verify(
            c => c.EditMessageAsync(It.IsAny<string>(), It.IsAny<EditMessageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never, "零增量时 Flush 不再编辑（占位消息保持初始状态，不产生空文本编辑错误）");
    }
}
