// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// T4-2（WP4 / D-3 / R-5）：无状态确认令牌的单元测试——签发/验签回环、过期、
/// <b>绑定矩阵</b>（换工具/换参数/换应用/换用户均失效）与防伪造。
/// </summary>
public class ToolConfirmationTokenTests
{
    private const string Secret = "unit-test-host-secret";
    private static readonly DateTimeOffset Now = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IssueAndValidate_ShouldRoundTrip_WithSameBinding()
    {
        var digest = ToolConfirmationToken.ComputeArgumentsDigest(new Dictionary<string, object?> { ["text"] = "你好" });
        var token = ToolConfirmationToken.Issue(Secret, "im.send_message", digest, "appA", "user1", Now);

        token.Should().NotBeNullOrEmpty("密钥已配置时必须签发令牌");
        ToolConfirmationToken.TryValidate(token, Secret, "im.send_message", digest, "appA", "user1", Now)
            .Should().BeTrue("同绑定、未过期、签名一致的令牌必须验签通过");
    }

    [Fact]
    public void Issue_ShouldReturnNull_WhenSecretMissing()
    {
        ToolConfirmationToken.Issue(null, "im.send_message", "digest", "appA", null, Now)
            .Should().BeNull("密钥未配置 = 能力降级，不签发");
        ToolConfirmationToken.Issue("  ", "im.send_message", "digest", "appA", null, Now)
            .Should().BeNull();
    }

    [Fact]
    public void TryValidate_ShouldFail_WhenExpired()
    {
        var token = ToolConfirmationToken.Issue(Secret, "im.send_message", "digest", "appA", null, Now);

        ToolConfirmationToken.TryValidate(token, Secret, "im.send_message", "digest", "appA", null, Now.AddMinutes(11))
            .Should().BeFalse("默认有效期 10 分钟，过期即失效");
    }

    [Fact]
    public void TryValidate_ShouldFail_WhenBindingChanges()
    {
        var digest = ToolConfirmationToken.ComputeArgumentsDigest(new Dictionary<string, object?> { ["text"] = "你好" });
        var token = ToolConfirmationToken.Issue(Secret, "im.send_message", digest, "appA", "user1", Now);

        // 绑定矩阵（R-5）：换工具 / 换参数 / 换应用 / 换用户 → 全部失效（模型无法搬运批准）。
        ToolConfirmationToken.TryValidate(token, Secret, "im.send_message", digest, "appA", "user2", Now)
            .Should().BeFalse("换用户即失效");
        ToolConfirmationToken.TryValidate(token, Secret, "im.send_message", digest, "appB", "user1", Now)
            .Should().BeFalse("换应用即失效");
        ToolConfirmationToken.TryValidate(token, Secret, "bitable.add_record", digest, "appA", "user1", Now)
            .Should().BeFalse("换工具即失效");
        ToolConfirmationToken.TryValidate(token, Secret, "im.send_message", "digest-changed", "appA", "user1", Now)
            .Should().BeFalse("换参数即失效");
    }

    [Fact]
    public void TryValidate_ShouldFail_WhenTamperedOrGarbage()
    {
        var digest = ToolConfirmationToken.ComputeArgumentsDigest(new Dictionary<string, object?> { ["text"] = "你好" });
        var token = ToolConfirmationToken.Issue(Secret, "im.send_message", digest, "appA", null, Now)!;

        var tampered = "v1." + (long.Parse(token.Split('.')[1]) + 1) + "." + token.Split('.')[2];
        ToolConfirmationToken.TryValidate(tampered, Secret, "im.send_message", digest, "appA", null, Now)
            .Should().BeFalse("篡改过期时刻必须验签失败");

        ToolConfirmationToken.TryValidate("not-a-token", Secret, "im.send_message", digest, "appA", null, Now)
            .Should().BeFalse("垃圾输入 fail-closed");
        ToolConfirmationToken.TryValidate(null, Secret, "im.send_message", digest, "appA", null, Now)
            .Should().BeFalse("空令牌 fail-closed");
    }

    [Fact]
    public void ComputeArgumentsDigest_ShouldExcludeConfirmToken_AndBeValueSensitive()
    {
        var withToken = new Dictionary<string, object?>
        {
            ["text"] = "你好", [ToolConfirmationToken.ArgumentName] = "v1.123.ABCD",
        };
        var withoutToken = new Dictionary<string, object?> { ["text"] = "你好" };

        ToolConfirmationToken.ComputeArgumentsDigest(withToken)
            .Should().Be(ToolConfirmationToken.ComputeArgumentsDigest(withoutToken),
                "confirm_token 必须排除在摘要外——否则带令牌重试的参数变化会让令牌永远验不过");

        ToolConfirmationToken.ComputeArgumentsDigest(new Dictionary<string, object?> { ["text"] = "你好" })
            .Should().NotBe(ToolConfirmationToken.ComputeArgumentsDigest(new Dictionary<string, object?> { ["text"] = "你们好" }),
                "摘要必须值敏感（与审计用的掩码摘要不同）——同形状不同值不得共享批准");
    }
}
