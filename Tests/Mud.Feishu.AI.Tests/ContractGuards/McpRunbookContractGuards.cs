// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// T6 / D6 MCP 多租户 Runbook 守卫。
/// </summary>
/// <remarks>
/// 验证文档存在 + 关键章节标题。
/// </remarks>
public class McpRunbookContractGuards
{
    // 测试运行时工作目录可能是 bin/Debug/net8.0/，需向上回溯到仓库根
    private static readonly string RunbookPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..",
            "documents/AIAgent/MCP多租户部署Runbook.md"));

    [Fact]
    public void Runbook_ShouldExist()
    {
        File.Exists(RunbookPath).Should().BeTrue(
            $"MCP 多租户部署 Runbook 必须存在于 {RunbookPath}");
    }

    [Fact]
    public void Runbook_ShouldContainKeySections()
    {
        if (!File.Exists(RunbookPath))
        {
            throw new FileNotFoundException($"Runbook 不存在: {RunbookPath}");
        }

        var content = File.ReadAllText(RunbookPath);

        content.Should().Contain("进程 = 租户边界", "文档必须说明进程隔离模型");
        content.Should().Contain("写工具 HITL 语义对照表", "文档必须包含 HITL 语义对照");
        content.Should().Contain("stdio 线协议排障清单", "文档必须包含排障清单");
        content.Should().Contain("stdout 污染", "排障清单必须包含 stdout 污染");
        content.Should().Contain("NeedsUserConfirmation", "文档必须引用执行链授权器语义");
    }
}
