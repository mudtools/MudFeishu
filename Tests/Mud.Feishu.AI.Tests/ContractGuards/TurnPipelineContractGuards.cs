// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.ContractGuards;

/// <summary>
/// N4 事件管线中立化的守卫（方案 v2 §6.4）——迁移后本仓只保留产品薄壳半边：
/// <list type="bullet">
/// <item><see cref="TurnPipeline_ShouldNotHardcodeProductPolicy"/>：产品薄壳必须仍持有租户政策
/// （中立管线不得内建产品政策的半边随实现迁入新仓）。</item>
/// </list>
/// </summary>
public class TurnPipelineContractGuards
{
    /// <summary>
    /// <b>TurnPipeline_ShouldNotHardcodeProductPolicy</b>：中立回合管线不得含
    /// <c>AllowMissingAppKey</c> / <c>AllowToolsWithoutAppKey</c> / appKey 判断分支。
    /// </summary>
    /// <remarks>
    /// <para>
    /// E1（897 行状态机拆分）最易被"顺手简化"破坏的点：多租户分级是<b>产品政策</b>，
    /// 必须经 <see cref="Mud.AI.Agent.Events.ConversationalTurnOptions.ValidateRequestPolicies"/>
    /// 委托由产品薄壳注入（③ 式）。管线内一旦出现判断分支，中立包就焊死了某一家的租户语义。
    /// </para>
    /// <para>
    /// 正向锚：产品薄壳必须<b>仍然持有</b>该政策（含 <c>AllowMissingAppKey</c> 判据）——
    /// 迁移走样成「管线删了、壳也没了」时本守卫同样报红。
    /// </para>
    /// </remarks>
    [Fact]
    public void TurnPipeline_ShouldNotHardcodeProductPolicy()
    {
        // 中立面（ConversationalTurnPipeline / AgentAssemblyFactory / ObservedDelegatingAgent
        // 零产品政策）随实现迁出，由新仓 TurnPipelineContractGuards 持有——此处只保留产品薄壳正向锚。

        // 正向锚：政策必须仍在产品薄壳（经委托注入管线），而不是「两边都删」。
        var shellSource = ReadSource(Path.Combine("Mud.Feishu.AI", "Events", "ConversationalFeishuEventHandler.cs"));
        shellSource.Should().Contain("AllowMissingAppKey",
            "多租户分级政策必须保留在产品薄壳（经 ValidateRequestPolicies 委托注入管线）");
        shellSource.Should().Contain("AllowToolsWithoutAppKey",
            "工具链 × 租户键组合约束必须保留在产品薄壳");
    }

    /// <summary>
    /// <b>迁移说明</b>：源守卫的另一半（<c>NeutralPackages_ShouldNotReferenceMafExperimentalApis</c>——
    /// <c>Mud.AI.*</c> 不得引用 MAF 实验面）随 <c>Mud.AI.*</c> 迁出，由新仓 <c>TurnPipelineContractGuards</c> 持有。
    /// </summary>
    private static string ReadSource(string relativePath)
    {
        var path = Path.Combine(FindRepositoryRoot(), relativePath);
        File.Exists(path).Should().BeTrue($"断言锚点文件必须存在（丢失即本守卫已失配）：{path}");
        return File.ReadAllText(path);
    }

    /// <summary>跨平台定位仓库根目录（以 <c>Mud.Feishu.slnx</c> 为锚）。</summary>
    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
