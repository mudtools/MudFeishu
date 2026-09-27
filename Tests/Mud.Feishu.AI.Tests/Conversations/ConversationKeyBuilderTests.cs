// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// <see cref="ConversationKeyBuilder"/> 键治理：等价性、维度隔离、租户前缀、转义与回灌（Phase 0 §7）。
/// </summary>
public class ConversationKeyBuilderTests
{
    [Fact]
    public void Build_ShouldBeDeterministic_WhenSameInput()
    {
        var a = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");
        var b = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");

        a.Should().Be(b, "同输入必须产出同键（等价性）");
        a.Should().Be("feishu:app-a:conversation:chat:oc_1");
    }

    [Fact]
    public void Build_ShouldIsolateGroupAndUserDimensions()
    {
        // 同一主体 ID 在群聊（chat 段）与单聊（user 段）下键互异。
        var group = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");
        var p2p = ConversationKeyBuilder.Build("app-a", ConversationScope.P2P(), "oc_1");

        group.Should().NotBe(p2p);
        group.Should().Contain(":conversation:chat:");
        p2p.Should().Contain(":conversation:user:");
    }

    [Fact]
    public void Build_ShouldIsolateApps()
    {
        // FU-2 教训：键必须带应用/租户维度，不同 appKey 键互异。
        var a = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");
        var b = ConversationKeyBuilder.Build("app-b", ConversationScope.Group(), "oc_1");

        a.Should().NotBe(b);
    }

    [Fact]
    public void Build_ShouldFallbackAppKeyToDefault_WhenBlank()
    {
        var key = ConversationKeyBuilder.Build(string.Empty, ConversationScope.Group(), "oc_1");

        key.Should().StartWith("feishu:default:", "空白 appKey 兜底 default（对齐 TokenKeyBuilder 语义）");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSubjectIdBlank()
    {
        var act = () => ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), " ");

        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("oc:1")]
    [InlineData("oc*1")]
    [InlineData("oc?1")]
    [InlineData("oc[1]")]
    [InlineData(@"oc\1")]
    [InlineData("oc\\:1")]
    public void Build_ShouldEscapeSeparatorAndGlobMetacharacters(string subjectId)
    {
        var key = ConversationKeyBuilder.Build("app:a", ConversationScope.Group(), subjectId);

        // 键的固定形态段不能被注入破坏：恰好 5 段。
        var rawSegments = key.Split(':');
        rawSegments.Length.Should().BeGreaterThan(5, "含 ':' 的段必须被转义而不是切分段");

        // 回灌：解析出的 subjectId 与原始输入一致。
        ConversationKeyBuilder.TryParse(key, out _, out var scope, out var parsedSubjectId, out _)
            .Should().BeTrue();
        parsedSubjectId.Should().Be(subjectId);
    }

    [Fact]
    public void Build_ShouldRoundTripViaTryParse()
    {
        var key = ConversationKeyBuilder.Build("my-app", ConversationScope.P2P(), "ou_42", keyPrefix: "env1");

        var parsed = ConversationKeyBuilder.TryParse(key, out var appKey, out var scope, out var subjectId, out var keyPrefix);

        parsed.Should().BeTrue("回灌语义：Build 的产物必须可解析");
        appKey.Should().Be("my-app");
        scope.IsGroup.Should().BeFalse();
        subjectId.Should().Be("ou_42");
        keyPrefix.Should().Be("env1");

        // 回灌再构造：逐字节一致。
        ConversationKeyBuilder.Build(appKey!, scope, subjectId!, keyPrefix)
            .Should().Be(key);
    }

    [Fact]
    public void TryParse_ShouldReturnFalse_WhenKeyIsForeignOrCorrupted()
    {
        ConversationKeyBuilder.TryParse(null, out _, out _, out _, out _).Should().BeFalse();
        ConversationKeyBuilder.TryParse("", out _, out _, out _, out _).Should().BeFalse();
        ConversationKeyBuilder.TryParse("feishu:app-a:token:access", out _, out _, out _, out _)
            .Should().BeFalse("非会话键（如令牌键）不得误判为会话键");
        ConversationKeyBuilder.TryParse("feishu:app-a:conversation:weird:oc_1", out _, out _, out _, out _)
            .Should().BeFalse("未知维度不得解析成功");
    }

    [Fact]
    public void Build_ShouldThrow_WhenSegmentTooLong()
    {
        var act = () => ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), new string('x', 257));

        act.Should().Throw<ArgumentException>("超长键段防 DoS（对齐 TokenKeyBuilder）");
    }

    [Fact]
    public void ComposeNamespace_ShouldPrefixAndValidate()
    {
        var key = ConversationKeyBuilder.Build("app-a", ConversationScope.Group(), "oc_1");
        var composed = ConversationKeyBuilder.ComposeNamespace("feishu:conversation", key);

        composed.Should().Be("feishu:conversation:" + key);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("*glob")]
    public void ComposeNamespace_ShouldThrow_WhenPrefixInvalid(string prefix)
    {
        var act = () => ConversationKeyBuilder.ComposeNamespace(prefix, "feishu:app-a:conversation:chat:oc_1");

        act.Should().Throw<ArgumentException>("R-01 护栏：空前缀/通配符前缀会破坏键空间");
    }
}
