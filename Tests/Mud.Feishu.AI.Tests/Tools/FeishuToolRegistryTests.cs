// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.AI.Tests.Tools;

/// <summary>
/// 工具注册表白名单语义（收进注册表不启用，MapTool 显式启用）。
/// </summary>
public class FeishuToolRegistryTests
{
    private static FeishuToolDefinition Definition(string name) => new(
        Name: name,
        Description: "desc",
        RequiredScopes: ["bitable:app:readonly"],
        IsWrite: false,
        Handler: (_, _, _) => Task.FromResult("ok"));

    [Fact]
    public void Register_ThenMapTool_ShouldEnable()
    {
        var registry = new FeishuToolRegistry()
            .Register(Definition("bitable.query_records"));

        registry.IsEnabled("bitable.query_records").Should().BeFalse("收进注册表不启用");
        registry.EnabledTools.Should().BeEmpty();

        registry.MapTool("bitable.query_records");

        registry.IsEnabled("bitable.query_records").Should().BeTrue();
        registry.EnabledTools.Should().ContainSingle(t => t.Name == "bitable.query_records");
    }

    [Fact]
    public void MapTool_ShouldThrow_WhenNotRegistered()
    {
        var act = () => new FeishuToolRegistry().MapTool("ghost.tool");

        act.Should().Throw<KeyNotFoundException>("白名单不得静默放行未注册工具");
    }

    [Fact]
    public void Register_ShouldRejectDuplicateName()
    {
        var act = () => new FeishuToolRegistry()
            .Register(Definition("x.y"))
            .Register(Definition("x.y"));

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void AllTools_ShouldIncludeUnmapped()
    {
        var registry = new FeishuToolRegistry()
            .Register(Definition("a.a"))
            .Register(Definition("b.b"))
            .MapTool("a.a");

        registry.AllTools.Should().HaveCount(2);
        registry.EnabledTools.Should().ContainSingle();
    }
}

/// <summary>
/// 授权结果契约：三态枚举 + 便捷构造。
/// </summary>
public class AuthorizationResultTests
{
    [Fact]
    public void Deny_ShouldCarryReason()
    {
        var result = AuthorizationResult.Deny("未授权的写操作");

        result.Decision.Should().Be(AuthorizationDecision.Denied);
        result.Reason.Should().Be("未授权的写操作");
    }

    [Fact]
    public void NeedsUserConfirmation_ShouldBeThirdState()
    {
        // Phase 3 HITL 三态契约统一（Phase 3 §3.4）：同类型扩展，不另立结果类型。
        AuthorizationResult.Confirm("需要用户批准").Decision
            .Should().Be(AuthorizationDecision.NeedsUserConfirmation);
        Enum.GetNames<AuthorizationDecision>().Should().HaveCount(3);
    }
}
