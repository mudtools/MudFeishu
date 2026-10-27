// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;

namespace Mud.Feishu.WebSocket.Tests.ContractGuards;

/// <summary>
/// 后置拦截器韧性守卫（多应用审查）。
/// </summary>
/// <remarks>
/// 拦截的真实回归：两条通道的后置拦截器都在 <c>finally</c> 中用调用方令牌串行调用——
/// ① 外部取消后令牌已取消，收尾拦截器被立即短路（审计/清理未执行）；
/// ② 任一拦截器抛异常会跳过其余拦截器，并在业务失败路径上<b>替换掉原始异常</b>，
/// 改变 ACK/重试语义。
/// 不变量：收尾调用使用 <see cref="CancellationToken.None"/>，且逐条隔离异常。
/// </remarks>
[Trait("Category", "ContractGuard")]
public class PostInterceptorResilienceContractGuards
{
    [Fact]
    public void WebhookPostInterceptors_ShouldUseNoneToken_AndIsolateExceptions()
    {
        var source = ReadSource("Mud.Feishu.Webhook/Services/FeishuWebhookService.cs");

        source.Should().Contain("AfterHandleAsync(eventData.EventType, eventData, processingException, CancellationToken.None)",
            "Webhook 收尾拦截器不得消费调用方令牌");
        source.Should().Contain("catch (Exception afterEx)",
            "Webhook 必须逐条隔离收尾拦截器异常，避免跳过其余拦截器/掩盖原始异常");
        source.Should().NotContain("processingException, cancellationToken)",
            "检测到旧的收尾调用形态（使用调用方令牌）");
    }

    [Fact]
    public void WebSocketPostInterceptors_ShouldUseNoneToken_AndIsolateExceptions()
    {
        var source = ReadSource("Mud.Feishu.WebSocket/Handlers/FeishuEventMessageHandler.cs");

        source.Should().Contain("processingException, CancellationToken.None",
            "WebSocket 收尾拦截器不得消费调用方令牌");
        source.Should().Contain("catch (Exception afterEx)",
            "WebSocket 必须逐条隔离收尾拦截器异常");
        source.Should().NotContain("processingException, cancellationToken)",
            "检测到旧的收尾调用形态（使用调用方令牌）");
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
