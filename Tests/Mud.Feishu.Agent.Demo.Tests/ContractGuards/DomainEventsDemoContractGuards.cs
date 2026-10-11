// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Events;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Events;

namespace Mud.Feishu.Agent.Demo.Tests.ContractGuards;

/// <summary>
/// <b>R-13 守卫</b>：Demo 必须<b>真的注册并调用</b>三个领域会话处理器、两个领域上下文装配器
/// 与流式通道降级链——"类存在"不算通过。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么要这条守卫（缺陷背景）</b>：<c>ApprovalTaskConversationalEventHandler</c> /
/// <c>BitableRecordChangedConversationalEventHandler</c> / <c>TaskUpdatedConversationalEventHandler</c>
/// 与 <c>ApprovalContextAssembler</c> / <c>BitableRecordContextAssembler</c>、以及
/// <c>CardStreamMessageChannel</c> + <c>StreamingChannelChain</c>，此前<b>只有各自单测消费</b>——
/// 发布面承诺了"宿主注册即生效"，却没有一个真实调用点。允许这种状态的实际后果是：
/// 装配契约（构造参数、DI 依赖、与装配器的事实键约定）只在测试里成立，真实宿主照文档接线可能不工作。
/// </para>
/// <para>
/// <b>本守卫的判定机制（三条，均防"假绿"）</b>：
/// <list type="number">
/// <item><b>行为面（反射）</b>：三个处理器类型必须是可解析的 public 类型、其 <c>HandleAsync</c>
/// 必须是 public 方法；降级链必须真的实现 <see cref="IMessageChannel"/>；
/// 两个装配器必须真的实现 <see cref="IContextAssembler"/>——保证下面的源码扫描指向真实 API；</item>
/// <item><b>调用面（源码）</b>：Demo 的模式文件必须出现三个处理器的<b>解析</b>与
/// <c>HandleAsync</c> 调用（计数 ≥ 3）、装配器的 <c>contextAssemblers:</c> 接线、
/// 流式链的 <c>AddFeishuStreamingChannel</c> + <c>Begin/WriteStream/Flush</c> 全链调用；</item>
/// <item><b>落点断言的<b>存在性</b></b>：Demo 必须对"增量落点次数为 0"显式抛错——
/// 否则"走了一轮流式"与"增量根本没落到通道"在输出上无法区分（Readme §6 防假绿铁律）。</item>
/// </list>
/// </para>
/// <para>
/// <b>与 <see cref="DocAgentDemoContractGuards.DemoProject_ShouldKeepTheExistingProjectReferenceSet"/> 的关系</b>：
/// 那条守卫断言 Demo 只有 3 条 <c>ProjectReference</c>（R-13 明确要求保持）。本模式用到的
/// 事件 DTO（<c>Mud.Feishu.EventCallback</c>）与 <c>IFeishu*</c> 客户端类型<b>经传递引用获得</b>，
/// 故两条守卫同批成立；若本模式新增引用，那条守卫会先红。
/// </para>
/// </remarks>
public class DomainEventsDemoContractGuards
{
    private const string DemoModeFile = "Demos/Mud.Feishu.Agent.Demo/Modes/DomainEventsDemo.cs";

    /// <summary>① 行为面：被演示的处理器/装配器/通道类型必须是可解析的真实公开契约。</summary>
    [Fact]
    public void DemoedTypes_ShouldBeRealPublicContracts()
    {
        foreach (var type in new[]
        {
            typeof(ApprovalTaskConversationalEventHandler),
            typeof(BitableRecordChangedConversationalEventHandler),
            typeof(TaskUpdatedConversationalEventHandler),
        })
        {
            type.IsPublic.Should().BeTrue($"{type.Name} 必须是对宿主可见的公开类型（否则 Demo 只是自娱自乐）");
            type.GetMethod("HandleAsync").Should().NotBeNull(
                $"{type.Name} 必须有公开入口 HandleAsync——本守卫的源码扫描以它为判据");
        }

        typeof(StreamingChannelChain).Should().BeAssignableTo<IMessageChannel>(
            "降级链必须实现 IMessageChannel（否则 AddFeishuStreamingChannel 注册进去也拿不到）");

        typeof(ApprovalContextAssembler).Should().BeAssignableTo<IContextAssembler>();
        typeof(BitableRecordContextAssembler).Should().BeAssignableTo<IContextAssembler>();
    }

    /// <summary>② 调用面：Demo 必须注册并调用三个处理器、接线两个装配器、走通流式全链。</summary>
    /// <remarks>
    /// 反向自证（防假绿）：<c>HandleAsync</c> 的命中数必须 <b>≥ 3</b>（三个处理器各一次）——
    /// 若正则或路径失效，本条会因"扫不到"而失败，而不是静默通过。
    /// </remarks>
    [Fact]
    public void DemoMode_ShouldRegisterAndInvokeTheWholeChain()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), DemoModeFile.Replace('/', Path.DirectorySeparatorChar)));

        foreach (var handler in new[]
        {
            "ApprovalTaskConversationalEventHandler",
            "BitableRecordChangedConversationalEventHandler",
            "TaskUpdatedConversationalEventHandler",
        })
        {
            source.Should().Contain(
                $"GetRequiredService<{handler}>()",
                $"{handler} 必须被 Demo 真实解析（只需注册而没人解析 = 没有调用点）");
        }

        var handleCalls = Regex.Matches(source, @"\.HandleAsync\(").Count;
        handleCalls.Should().BeGreaterThanOrEqualTo(
            3, "三个领域处理器必须各被调用一次（事件载荷由 Demo 构造后派发）——少于 3 次说明扫描失效或漏调用");

        // 装配器必须"接线进处理器"，而不是只被注册到容器（注册了没人用正是 D-2 的原始缺陷形态）。
        source.Should().Contain("contextAssemblers:", "领域装配器必须经 contextAssemblers 接线进处理器");
        source.Should().Contain("ApprovalContextAssembler");
        source.Should().Contain("BitableRecordContextAssembler");

        // 流式链路：注册 + 全链调用（Begin → WriteStream → Flush）。
        source.Should().Contain("AddFeishuStreamingChannel(");
        source.Should().Contain(".BeginAsync(");
        source.Should().Contain(".WriteStreamAsync(");
        source.Should().Contain(".FlushAsync(");
    }

    /// <summary>③ 落点断言的存在性：Demo 必须对"零增量落点"显式抛错。</summary>
    [Fact]
    public void StreamingSection_ShouldAssertIncrementLanded()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), DemoModeFile.Replace('/', Path.DirectorySeparatorChar)));

        source.Should().Contain("increments", "必须统计增量落点次数");
        source.Should().Contain(
            "increments == 0",
            "必须对『零增量落点』显式判定——否则『走了一轮流式』与『增量没落通道』在输出上无法区分");
        source.Should().Contain(
            "InvalidOperationException",
            "零增量必须是 fail-fast（抛错），而不是打印一行提示后继续");
    }

    /// <summary>④ 事件注入形态：事件载荷由 Demo 构造（不依赖外部平台通道）。</summary>
    /// <remarks>
    /// 与 R-13 的建议一致：直接构造事件载荷 + 调用 handler ⇒ 演示可离线复现，
    /// 不把"演示能不能跑"绑在 WebSocket/Webhook 通道与真实平台投递上。
    /// </remarks>
    [Fact]
    public void EventPayloads_ShouldBeConstructedInDemo()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), DemoModeFile.Replace('/', Path.DirectorySeparatorChar)));

        source.Should().Contain("EventData", "事件载荷类型必须是 EventData（处理器的公开入口参数）");
        source.Should().Contain("evt-demo-", "事件 ID 必须显式构造（去重键的事实来源）");
        source.Should().Contain(
            "IAppKeyAccessor",
            "必须提供应用键事实——缺它时处理器会因『有工具链 + 无 appKey』fail-closed（R2-3）");
    }

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
