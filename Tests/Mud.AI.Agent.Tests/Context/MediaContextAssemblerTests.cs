// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Agent.Context;

namespace Mud.AI.Agent.Tests.Context;

/// <summary>
/// <see cref="MediaContextAssembler"/>（N4 · E7 机制）的自有测试：
/// 事实提取 / text 短路 / 截断 / untrusted 头 / 失败隔离跳过（取消原样传播）。
/// </summary>
/// <remarks>产品子类（媒体类型判定 + 转写器适配 + 占位降级）的行为覆盖在产品侧。</remarks>
public class MediaContextAssemblerTests
{
    /// <summary>最小产品子类替身：image 类型回文本、否则抛错。</summary>
    private sealed class TestMediaAssembler : MediaContextAssembler
    {
        public TestMediaAssembler()
            : base(
                order: 15,
                untrustedHeader: "[UNTRUSTED]",
                truncationMarker: "…(截断)",
                perItemLength: 10)
        {
        }

        protected override Task<string?> TranscribeAsync(
            ConversationRequest request, string messageType, string content, CancellationToken cancellationToken)
            => messageType == "image"
                ? Task.FromResult<string?>(new string('图', 30))
                : throw new InvalidOperationException("转写失败");
    }

    private static ConversationRequest NewRequest(string msgType, string content)
        => new(
            AppKey: "t1",
            Scope: ConversationScope.Group(),
            SubjectId: "chat-1",
            SenderId: "user-1",
            MessageId: "m1",
            MentionedText: "问题",
            EventFacts:
            [
                new EventFact("message_type", msgType),
                new EventFact("content", content),
            ]);

    [Fact]
    public async Task AssembleAsync_ShouldWrapTranscription_WithHeaderAndTruncation()
    {
        var fragment = await new TestMediaAssembler().AssembleAsync(NewRequest("image", "{}"));

        fragment.Should().StartWith("[UNTRUSTED]").And.Contain("[媒体内容]").And.Contain("…(截断)",
            "转写文本超单条长度闸 ⇒ 截断");
    }

    [Fact]
    public async Task AssembleAsync_ShouldSkip_TextTypeOrMissingFacts()
    {
        (await new TestMediaAssembler().AssembleAsync(NewRequest("text", "{}"))).Should().BeNull(
            "text 类型不处理（触发文本已由回合管线装配）");

        var empty = new ConversationRequest(
            AppKey: "t1", Scope: ConversationScope.Group(), SubjectId: "c", SenderId: "u", MessageId: "m", MentionedText: "问题");
        (await new TestMediaAssembler().AssembleAsync(empty)).Should().BeNull();
    }

    [Fact]
    public async Task AssembleAsync_ShouldSkip_WhenTranscriberFails()
    {
        var fragment = await new TestMediaAssembler().AssembleAsync(NewRequest("audio", "{}"));

        fragment.Should().BeNull("失败隔离：转写抛错记日志后跳过，不阻断对话");
    }
}
