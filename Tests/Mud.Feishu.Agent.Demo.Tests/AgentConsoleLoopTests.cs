// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// <see cref="AgentConsoleLoop"/> 的纯函数逻辑：命令识别边界（ADR-07）与 R5-12 挂起守卫。
/// </summary>
public class AgentConsoleLoopTests
{
    /// <summary>
    /// 未匹配已知命令的 <c>/</c> 前缀输入必须判为**普通输入**（照常发给模型，ADR-07）。
    /// </summary>
    /// <param name="input">输入文本。</param>
    [Theory]
    [InlineData("/unknown")]
    [InlineData("/")]
    [InlineData("普通问题")]
    [InlineData("prefix /help")]
    [InlineData("   ")]
    [InlineData("?")]
    public void IsSlashCommand_ShouldReturnFalse_ForUnknownToken(string input)
    {
        AgentConsoleLoop.IsSlashCommand(input, out _).Should().BeFalse(
            "首 token 不匹配已知命令时不得拦截（模型侧可能真的需要该输入）");
    }

    /// <summary>命令与别名都必须被识别，且别名归一为规范名（别名同样以 <c>/</c> 开头）。</summary>
    /// <param name="input">输入文本。</param>
    /// <param name="expected">期望的规范命令名。</param>
    [Theory]
    [InlineData("/help", "help")]
    [InlineData("/?", "help")]
    [InlineData("/APPROVE demo-approval-001", "approve")]
    [InlineData("/y demo-approval-001", "approve")]
    [InlineData("/quit", "exit")]
    [InlineData("/scenario kb-digest", "scenario")]
    [InlineData("/v", "verbose")]
    public void IsSlashCommand_ShouldResolveAlias_ToCanonicalName(string input, string expected)
    {
        AgentConsoleLoop.IsSlashCommand(input, out var command).Should().BeTrue();

        command.Name.Should().Be(expected);
    }

    /// <summary>
    /// 容忍开头的 BOM（<c>Trim()</c> 不去 BOM，<c>char.IsWhiteSpace('\uFEFF') == false</c>）。
    /// </summary>
    /// <remarks>
    /// 缺陷原始形态：<c>pwsh -Value @("/help", …)</c> 写出的脚本文件带 BOM，重定向给
    /// <c>dotnet run &lt; script.txt</c> 后首行变成 <c>"\uFEFF/help"</c>，被当成普通输入发给模型
    /// （表现是"第一条命令莫名不生效，且会真的去调模型"）。
    /// </remarks>
    [Fact]
    public void IsSlashCommand_Should_TolerateLeadingBom()
    {
        "\uFEFF/help".Should().NotBe("/help", "先固定前提：Trim() 不去 BOM");

        AgentConsoleLoop.IsSlashCommand("\uFEFF/help", out var command).Should().BeTrue();
        command.Name.Should().Be("help");

        AgentConsoleLoop.NormalizeInput("\uFEFF /exit ").Should().Be("/exit");
    }

    /// <summary>参数必须原样保留（关联号含连字符与数字，不能被拆分）。</summary>
    [Fact]
    public void IsSlashCommand_ShouldKeepArgumentVerbatim()
    {
        AgentConsoleLoop.IsSlashCommand("/approve demo-approval-007", out var command).Should().BeTrue();

        command.Argument.Should().Be("demo-approval-007");
    }

    /// <summary>挂起态下必须阻断新问题（否则一次 /abandon 无法清理多个框架待审批记录）。</summary>
    [Fact]
    public void ShouldBlockTurn_Should_ReturnTrue_When_PendingApproval()
    {
        AgentConsoleLoop.ShouldBlockTurn(hasPendingApproval: true).Should().BeTrue();
        AgentConsoleLoop.ShouldBlockTurn(hasPendingApproval: false).Should().BeFalse();
    }

    /// <summary>命令集必须与设计文档 §5.2 的命令表条目一一对应（17 条）。</summary>
    [Fact]
    public void CommandNames_ShouldMatchTheDocumentedCommandSet()
    {
        string[] documented =
        [
            "help", "tools", "guidance", "pending", "approve", "deny", "abandon",
            "policy", "dryrun", "audit", "export", "usage", "history", "reset",
            "scenario", "verbose", "exit",
        ];

        AgentConsoleLoop.CommandNames.Should().BeEquivalentTo(documented);
        AgentConsoleLoop.CommandNames.Should().HaveCount(17, "设计文档 §5.2 的命令表共 17 行");
        AgentConsoleLoop.CommandNames.Should().OnlyHaveUniqueItems();
    }
}
