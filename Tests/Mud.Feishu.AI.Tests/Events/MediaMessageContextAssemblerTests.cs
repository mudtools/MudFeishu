// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.AgentTools;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.Events;

namespace Mud.Feishu.AI.Tests.Events;

/// <summary>
/// T3 媒体消息上下文装配器用例。
/// </summary>
/// <remarks>
/// 覆盖：6 类 message_type 分支 + 转写失败降级 + 超限截断 + 预算三级闸 + 软缺席 + untrusted 标注。
/// </remarks>
public class MediaMessageContextAssemblerTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();
    private readonly Mock<IFeishuMediaTranscriber> _transcriber = new();
    private readonly MediaInputOptions _options = new();

    public MediaMessageContextAssemblerTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new DisposableScope());
    }

    private MediaMessageContextAssembler CreateSut(IFeishuMediaTranscriber? transcriber = null)
        => new(_messageClient.Object, _scopeFactory.Object, transcriber, _options);

    private static ConversationRequest Request(string msgType, string content)
        => new("appA", ConversationScope.Group(), "oc_test", "ou_sender", "om_1", "hello",
            ChatId: "oc_test",
            EventKey: "im.message",
            EventFacts: [new EventFact("message_type", msgType), new EventFact("content", content)]);

    [Fact]
    public async Task Assemble_TextMessage_ShouldReturnNull()
    {
        // Act
        var result = await CreateSut().AssembleAsync(Request("text", "{\"text\":\"hello\"}"));

        // Assert
        result.Should().BeNull("text 类型不处理——既有 ExtractMentionedText 路径");
    }

    [Fact]
    public async Task Assemble_Image_WithTranscriber_ShouldReturnOcrText()
    {
        // Arrange
        _transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<MediaTranscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaTranscription("识别到的文字内容", "ocr", false));

        // Act
        var result = await CreateSut(_transcriber.Object).AssembleAsync(
            Request("image", "{\"image_key\":\"img_test_key\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("[图片内容｜OCR]");
        result.Should().Contain("识别到的文字内容");
        result.Should().Contain(ContextBudgets.UntrustedHeader);
    }

    [Fact]
    public async Task Assemble_Image_WithoutTranscriber_ShouldReturnPlaceholder()
    {
        // Act
        var result = await CreateSut(transcriber: null).AssembleAsync(
            Request("image", "{\"image_key\":\"img_test_key\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("[用户发送了一张图片", "转写器未注册时应输出占位说明");
        result.Should().NotContain("img_test_key", "占位说明不应泄露 file key");
    }

    [Fact]
    public async Task Assemble_Image_OcrDisabled_ShouldReturnPlaceholder()
    {
        // Arrange
        _options.EnableImageOcr = false;

        // Act
        var result = await CreateSut(_transcriber.Object).AssembleAsync(
            Request("image", "{\"image_key\":\"img_test_key\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("OCR 未启用");
        _transcriber.Verify(
            t => t.TranscribeAsync(It.IsAny<MediaTranscriptionRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Assemble_Audio_WithTranscriber_ShouldReturnSttText()
    {
        // Arrange
        _transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<MediaTranscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaTranscription("语音转写结果", "stt", false));

        // Act
        var result = await CreateSut(_transcriber.Object).AssembleAsync(
            Request("audio", "{\"file_key\":\"audio_test_key\",\"duration\":1000}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("[语音转写]");
        result.Should().Contain("语音转写结果");
    }

    [Fact]
    public async Task Assemble_Audio_SttDisabled_ShouldReturnPlaceholder()
    {
        // Arrange
        _options.EnableAudioStt = false;

        // Act
        var result = await CreateSut(_transcriber.Object).AssembleAsync(
            Request("audio", "{\"file_key\":\"audio_test_key\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("STT 未启用");
    }

    [Fact]
    public async Task Assemble_File_ShouldReturnMetadata()
    {
        // Act
        var result = await CreateSut().AssembleAsync(
            Request("file", "{\"file_key\":\"file_test\",\"file_name\":\"report.pdf\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("report.pdf", "文件类型应输出文件名元信息");
    }

    [Fact]
    public async Task Assemble_Post_ShouldExtractTitleAndText()
    {
        // Arrange
        var postContent = """{"title":{"element":"会议纪要"},"content":[{"tag":"text","text":"这是正文内容"}]}""";

        // Act
        var result = await CreateSut().AssembleAsync(Request("post", postContent));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("会议纪要");
        result.Should().Contain("这是正文内容");
    }

    [Fact]
    public async Task Assemble_Sticker_ShouldReturnPlaceholder()
    {
        // Act
        var result = await CreateSut().AssembleAsync(Request("sticker", "{\"sticker_key\":\"stk_1\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("表情包");
        result.Should().NotContain("stk_1", "不应泄露 sticker key");
    }

    [Fact]
    public async Task Assemble_TranscriberReturnsNull_ShouldReturnPlaceholder()
    {
        // Arrange
        _transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<MediaTranscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((MediaTranscription?)null);

        // Act
        var result = await CreateSut(_transcriber.Object).AssembleAsync(
            Request("image", "{\"image_key\":\"img_test\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain("暂无法识别内容", "转写返回 null 应降级为占位说明");
    }

    [Fact]
    public async Task Assemble_LongText_ShouldBeTruncated()
    {
        // Arrange
        var longText = new string('x', _options.PerItemLength + 100);
        _transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<MediaTranscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MediaTranscription(longText, "ocr", false));

        // Act
        var result = await CreateSut(_transcriber.Object).AssembleAsync(
            Request("image", "{\"image_key\":\"img_test\"}"));

        // Assert
        result.Should().NotBeNull();
        result.Should().Contain(AssemblerBudgets.TruncationMarker, "超 PerItemLength 应截断");
    }

    [Fact]
    public async Task Assemble_Exception_ShouldReturnNull_NotThrow()
    {
        // Arrange
        _transcriber
            .Setup(t => t.TranscribeAsync(It.IsAny<MediaTranscriptionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("network error"));

        // Act
        var result = await CreateSut(_transcriber.Object).AssembleAsync(
            Request("image", "{\"image_key\":\"img_test\"}"));

        // Assert
        result.Should().BeNull("异常应被捕获并返回 null——不阻断对话");
    }

    [Fact]
    public async Task Assemble_Order_ShouldBe15()
    {
        // Act & Assert
        var sut = CreateSut();
        sut.Order.Should().Be(15, "MediaMessage Order=15（SenderInfo=10 → MediaMessage=15 → QuoteMessage=20）");
    }

    [Fact]
    public async Task Assemble_NoEventFacts_ShouldReturnNull()
    {
        // Arrange: request without EventFacts
        var request = new ConversationRequest("appA", ConversationScope.Group(), "oc_test", "ou_sender", "om_1", "hello", ChatId: "oc_test");

        // Act
        var result = await CreateSut().AssembleAsync(request);

        // Assert
        result.Should().BeNull("无 EventFacts 时装配器无贡献");
    }

    private sealed class DisposableScope : IDisposable
    {
        public void Dispose() { }
    }
}
