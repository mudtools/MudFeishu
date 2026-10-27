// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.Webhook.Tests.ContractGuards;

/// <summary>
/// Webhook 处理器分发策略守卫（多应用审查）。
/// </summary>
/// <remarks>
/// 拦截的真实回归：应用键注册了专属处理器后，全局处理器被<b>无条件</b>跳过，
/// 安全/审计类横切处理器在多应用部署中静默失效，且宿主没有任何开关可以找回。
/// 不变量：默认为既有行为（AppScopedOnly），合并语义必须由显式配置开启。
/// </remarks>
[Trait("Category", "ContractGuard")]
public class HandlerDispatchModeContractGuards
{
    [Fact]
    public void Options_ShouldDefaultToAppScopedOnly()
    {
        var source = ReadSource("Mud.Feishu.Webhook/Configuration/FeishuWebhookOptions.cs");

        source.Should().Contain("public HandlerDispatchMode HandlerDispatchMode { get; set; } = HandlerDispatchMode.AppScopedOnly;",
            "默认必须保持既有行为（AppScopedOnly），合并语义只能显式开启");
    }

    [Fact]
    public void Dispatch_ShouldHonourMergeMode_AndSuppressFalseUnhandled()
    {
        var source = ReadSource("Mud.Feishu.Webhook/Services/FeishuWebhookService.cs");

        source.Should().Contain("Options.HandlerDispatchMode == HandlerDispatchMode.MergeWithGlobal",
            "分发必须读取策略开关，否则配置项成为死配置");
        source.Should().Contain("suppressUnhandled: scopedHandled",
            "专属处理器已处理时，合并路径不得再把事件记为 unhandled");
    }

    private static string ReadSource(string relativePath)
    {
        var path = Path.Combine(GetRepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(path).Should().BeTrue($"源码必须存在：{relativePath}");
        return File.ReadAllText(path);
    }

    private static string GetRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）——守卫无法工作，宁可失败也不要假绿");
    }
}
