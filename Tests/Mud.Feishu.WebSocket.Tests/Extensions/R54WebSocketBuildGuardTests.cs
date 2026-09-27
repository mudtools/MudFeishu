// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions;
using Mud.Feishu.WebSocket;

namespace Mud.Feishu.WebSocket.Tests.Extensions;

/// <summary>
/// R5.4-R1/F8：Build() 重复构建防呆（V9：防呆点在 Build 而非入口）。
/// </summary>
public class R54WebSocketBuildGuardTests
{
    [Fact]
    public void Build_ShouldThrow_WhenCoreServicesAlreadyRegistered()
    {
        var services = new ServiceCollection();
        services.AddLogging();

        // 第一次构建成功
        services.CreateFeishuWebSocketServiceBuilder(_ => { })
            .AddHandler<TestHandler>()
            .Build();

        // 第二次构建（新 builder、同一 services）须显式失败
        var act = () => services.CreateFeishuWebSocketServiceBuilder(_ => { })
            .AddHandler<TestHandler>()
            .Build();

        act.Should().Throw<InvalidOperationException>(
            "重复 Build 会覆盖同一未命名 FeishuWebSocketOptions 并重复注册核心服务/后台服务，须显式失败");
    }

    private sealed class TestHandler : IFeishuEventHandler
    {
        public string SupportedEventType => "test.event";
        public Task HandleAsync(EventData eventData, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}