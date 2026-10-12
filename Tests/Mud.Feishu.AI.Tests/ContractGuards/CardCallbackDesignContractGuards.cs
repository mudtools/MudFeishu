// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.IO;
using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// T11 / D11 卡片回调回流设计稿守卫（登记，不实施）。
/// </summary>
/// <remarks>
/// T11 的前置条件在组件仓（<c>Mud.Feishu.EventCallback</c>），本仓只做设计稿登记。
/// 守卫验证：①设计稿文档存在；②3 个重启条件全部登记；③设计稿含 <c>CardActionConversationalEventHandler</c> 与 <c>RunApprovalContinuationAsync</c> 引用。
/// </remarks>
public class CardCallbackDesignContractGuards
{
    private const string DesignDocName = "MudFeishu-AI智能体能力修复与完善方案-20261012.md";

    private static string FindDesignDoc()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var matches = dir.GetFiles(DesignDocName, SearchOption.AllDirectories);
            if (matches.Length > 0)
            {
                return matches[0].FullName;
            }

            dir = dir.Parent;
        }

        return string.Empty;
    }

    /// <summary>
    /// 守卫 ①：设计稿文档存在。
    /// </summary>
    [Fact]
    public void CardCallbackDesignDoc_ShouldExist()
    {
        var docPath = FindDesignDoc();
        docPath.Should().NotBeEmpty(
            "T11 设计稿必须登记在方案文档中（附录 C）");
    }

    /// <summary>
    /// 守卫 ②：附录 C 存在且含 3 个重启条件。
    /// </summary>
    [Fact]
    public void AppendixC_ShouldContain_ThreeRestartConditions()
    {
        var docPath = FindDesignDoc();
        if (string.IsNullOrEmpty(docPath))
        {
            return; // 守卫 ①已报告
        }

        var content = File.ReadAllText(docPath);

        content.Should().Contain("附录 C", "T11 设计稿必须登记在附录 C");

        // 3 个重启条件的关键词
        content.Should().Contain("card.action.trigger",
            "重启条件 ①：组件仓需新增 card.action.trigger 事件 DTO");
        content.Should().Contain("签名校验",
            "重启条件 ②：卡片回调签名校验可用");
        content.Should().Contain("按钮即批准",
            "重启条件 ③：产品确认按钮即批准的合规性");
    }

    /// <summary>
    /// 守卫 ③：设计稿含 CardActionConversationalEventHandler 与 RunApprovalContinuationAsync。
    /// </summary>
    [Fact]
    public void DesignDoc_ShouldContain_HandlerName_AndContinuationMethod()
    {
        var docPath = FindDesignDoc();
        if (string.IsNullOrEmpty(docPath))
        {
            return;
        }

        var content = File.ReadAllText(docPath);

        content.Should().Contain("CardActionConversationalEventHandler",
            "设计稿必须登记 handler 类名");
        content.Should().Contain("RunApprovalContinuationAsync",
            "设计稿必须引用审批续跑方法");
        content.Should().Contain("PendingApprovalStore",
            "设计稿必须引用待审批存储");
    }

    /// <summary>
    /// 守卫 ④：本仓不含 CardActionConversationalEventHandler 实现（前置未满足）。
    /// </summary>
    [Fact]
    public void CardActionHandler_ShouldNotBeImplemented_Yet()
    {
        // T11 前置条件未满足，实现不应存在于源代码中。
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
        var handlerType = allAssemblies
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "CardActionConversationalEventHandler");

        handlerType.Should().BeNull(
            "T11 前置条件未满足，CardActionConversationalEventHandler 不应已实现");
    }
}
