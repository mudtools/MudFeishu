// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Context;

/// <summary>
/// <see cref="SenderInfoContextAssembler"/>（E8 迁G）的自有测试：ID 形态注入与脱敏边界。
/// </summary>
public class SenderInfoContextAssemblerTests
{
    [Fact]
    public async Task AssembleAsync_ShouldIncludeSenderAndScope()
    {
        var request = new ConversationRequest(
            AppKey: "t1",
            Scope: ConversationScope.Group(),
            SubjectId: "chat-1",
            SenderId: "user-1",
            MessageId: "m1",
            MentionedText: "问题");

        var fragment = await new SenderInfoContextAssembler().AssembleAsync(request);

        fragment.Should().Contain("user-1").And.Contain("群聊");
    }

    [Fact]
    public async Task AssembleAsync_ShouldReturnNull_WithoutSender()
    {
        var request = new ConversationRequest(
            AppKey: "t1",
            Scope: ConversationScope.P2P(),
            SubjectId: "chat-1",
            SenderId: null,
            MessageId: "m1",
            MentionedText: "问题");

        var fragment = await new SenderInfoContextAssembler().AssembleAsync(request);

        fragment.Should().BeNull("无发送者 ⇒ 本装配器无贡献");
    }

    [Fact]
    public void Order_ShouldBeLowest_BaseFact()
    {
        new SenderInfoContextAssembler().Order.Should().Be(SenderInfoContextAssembler.DefaultOrder);
    }
}
