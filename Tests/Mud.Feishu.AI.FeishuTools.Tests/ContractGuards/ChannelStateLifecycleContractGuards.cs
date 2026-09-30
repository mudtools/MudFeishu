// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Reflection;
using Mud.Feishu.AI.Channels;
using Mud.Feishu.AI.Conversations;
using Mud.Feishu.AI.FeishuTools.Channels;
using Mud.Feishu.DataModels.CardMessageStream;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// G2（R2-09 / 根因 R-B）：<b>流式通道的 per-messageId 状态在终结后必须归零</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么用反射枚举而不是逐个通道写用例</b>：通道以 <c>Singleton</c> 注册（与进程同寿命），
/// 任何"键单调新增且永不重复"的字典都是确定性泄漏。R1-WP4 只修了基类<b>自己</b>的两个字典，
/// 两个子类的字典（<c>_bizIdToTarget</c> / <c>_messageOwners</c>）仍无移除点——
/// <b>同一个文件、相邻的字典，一个已修一个未修</b>，说明缺陷不是疏忽而是缺少强制机制。
/// 本守卫对<i>全部</i> <see cref="IMessageChannel"/> 实现生效：新增通道实现若不在此登记构造工厂
/// 会直接报红（见 <see cref="EveryMessageChannelImplementation_ShouldBeRegisteredForLifecycleCheck"/>），
/// 因此覆盖面等于代码里的实现数，而不是"评审者想到的数量"。
/// </para>
/// <para>
/// <b>自证报红</b>：<see cref="Checker_ShouldReportResidualState_ForLeakingChannel"/> 用一个人为泄漏的
/// 合成通道驱动同一检查器并断言报红——一次性自证不会留存，这一条是永久的。
/// </para>
/// </remarks>
public class ChannelStateLifecycleContractGuards
{
    /// <summary>
    /// 待验证的通道实现及其构造工厂。
    /// </summary>
    /// <remarks>
    /// <b>登记即义务</b>：新增通道实现而未登记会由
    /// <see cref="EveryMessageChannelImplementation_ShouldBeRegisteredForLifecycleCheck"/> 报红
    /// ——"守卫看不到新通道"这件事本身必须可见，否则元守卫退化成"只保护老代码"。
    /// </remarks>
    private static readonly Dictionary<Type, Func<IMessageChannel>> Factories = new()
    {
        [typeof(EditMessageChannel)] = () => new EditMessageChannel(
            BeginSucceedingMessageClient(),
            Mock.Of<IFeishuAppContextScopeFactory>(),
            Options.Create(new FeishuAgentOptions()),
            minUpdateInterval: TimeSpan.Zero),

        [typeof(CardStreamMessageChannel)] = () => new CardStreamMessageChannel(
            BeginSucceedingCardClient(),
            Mock.Of<IFeishuAppContextScopeFactory>(),
            Options.Create(new FeishuAgentOptions()),
            minUpdateInterval: TimeSpan.Zero),

        [typeof(StreamingChannelChain)] = () => new StreamingChannelChain(
            null,
            StubChannel("om_card"),
            StubChannel("om_edit")),
    };

    /// <summary>每个具体的通道实现都必须登记构造工厂（否则其状态生命周期无法被验证）。</summary>
    [Fact]
    public void EveryMessageChannelImplementation_ShouldBeRegisteredForLifecycleCheck()
    {
        // 扫描面 = 通道实现所在程序集（实现都在 Mud.Feishu.AI.FeishuTools；接口在 Mud.Feishu.AI）。
        // 刻意不扫测试程序集：本文件里的 LeakingChannel 是"检查器自证"用的合成类型，不该进覆盖集。
        var implementations = typeof(EditMessageChannel).Assembly
            .GetTypes()
            .Where(static t => typeof(IMessageChannel).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .OrderBy(static t => t.Name, StringComparer.Ordinal)
            .ToArray();

        implementations.Should().NotBeEmpty("扫描面为空是假绿（守卫必须命中真实实现）");

        var unregistered = implementations
            .Where(t => !Factories.ContainsKey(t))
            .Select(static t => t.Name)
            .ToArray();

        unregistered.Should().BeEmpty(
            "新增的 IMessageChannel 实现必须在 ChannelStateLifecycleContractGuards.Factories 登记构造工厂——"
            + "否则它的 per-messageId 状态（Singleton 生命周期）不会被任何守卫验证："
            + string.Join(", ", unregistered));
    }

    /// <summary>
    /// Begin → Write → Flush 之后，通道内每个 <c>ConcurrentDictionary&lt;string, *&gt;</c> 字段都必须为空。
    /// </summary>
    [Fact]
    public void AfterFlush_EveryPerMessageIdDictionary_ShouldBeEmpty()
    {
        var violations = new List<string>();

        foreach (var (type, factory) in Factories)
        {
            violations.AddRange(ChannelStateLifecycleChecker.FindResidualState(type, factory()));
        }

        Factories.Keys.Should().NotBeEmpty();
        violations.Should().BeEmpty(
            "以下通道字段在 Flush 之后仍有残留条目——通道是 Singleton，字典与进程同寿命，"
            + "键单调新增即确定性常驻内存增长。请在 Begin/Flush 的配对钩子（BufferedMessageChannel.OnFlushed "
            + "或自身 FlushAsync 的 finally）中移除：" + string.Join(" | ", violations));
    }

    /// <summary>检查器自证：对人为泄漏的合成通道必须报红。</summary>
    [Fact]
    public void Checker_ShouldReportResidualState_ForLeakingChannel()
        => ChannelStateLifecycleChecker
            .FindResidualState(typeof(LeakingChannel), new LeakingChannel())
            .Should().NotBeEmpty("合成泄漏通道是缺陷原始形态——检查器对它必须报红，否则本守卫是假绿");

    /// <summary>行为用例（R2-02）：卡片流通道 Flush 后 <c>_bizIdToTarget</c> 归零。</summary>
    [Fact]
    public async Task CardStreamMessageChannel_ShouldClearBizIdTarget_AfterFlush()
    {
        var channel = new CardStreamMessageChannel(
            BeginSucceedingCardClient(),
            Mock.Of<IFeishuAppContextScopeFactory>(),
            Options.Create(new FeishuAgentOptions()),
            minUpdateInterval: TimeSpan.Zero);

        var bizId = await channel.BeginAsync("app-a", "ou_1");
        await channel.WriteStreamAsync("app-a", "ou_1", bizId, new string('x', 500));
        await channel.FlushAsync("app-a", "ou_1", bizId);

        ReadDictionaryCounts(channel).Should().AllSatisfy(
            pair => pair.Value.Should().Be(0, $"{pair.Key} 在 Flush 之后必须归零（R2-02）"));
    }

    /// <summary>行为用例（R2-02）：降级链 Flush 后 <c>_messageOwners</c> 归零。</summary>
    [Fact]
    public async Task StreamingChannelChain_ShouldClearMessageOwners_AfterFlush()
    {
        var chain = new StreamingChannelChain(null, StubChannel("om_1"));
        var messageId = await chain.BeginAsync("app-a", "ou_1");
        await chain.FlushAsync("app-a", "ou_1", messageId);

        ReadDictionaryCounts(chain).Should().AllSatisfy(
            pair => pair.Value.Should().Be(0, $"{pair.Key} 在 Flush 之后必须归零（R2-02）"));
    }

    /// <summary>行为用例（R2-02/回归）：基类两个字典在 Flush 之后同样归零。</summary>
    [Fact]
    public async Task BufferedMessageChannelBaseState_ShouldBeEmpty_AfterFlush()
    {
        var channel = new EditMessageChannel(
            BeginSucceedingMessageClient(),
            Mock.Of<IFeishuAppContextScopeFactory>(),
            Options.Create(new FeishuAgentOptions()),
            minUpdateInterval: TimeSpan.Zero);

        await channel.WriteStreamAsync("app-a", "oc_1", "om_x", new string('y', 500));
        await channel.FlushAsync("app-a", "oc_1", "om_x");

        ReadDictionaryCounts(channel).Should().AllSatisfy(
            pair => pair.Value.Should().Be(0, $"{pair.Key} 在 Flush 之后必须归零（R2-11/既有语义）"));
    }

    // ────────── 辅助 ──────────

    /// <summary>反射读取实例内全部 <c>ConcurrentDictionary&lt;string, T&gt;</c> 字段的计数。</summary>
    private static IReadOnlyDictionary<string, int> ReadDictionaryCounts(object instance)
        => ChannelStateLifecycleChecker.ReadDictionaryCounts(instance);

    /// <summary>
    /// 只让 <c>Begin</c> 成功的消息客户端替身（占位消息创建成功；后续编辑失败被基类异常隔离吞掉，
    /// 不影响"生命周期结束后状态是否归零"这一断言对象）。
    /// </summary>
    private static Mud.Feishu.IFeishuTenantV1Message BeginSucceedingMessageClient()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        client
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_edit_1" },
            });
        return client.Object;
    }

    /// <summary>只让卡片流 <c>Begin</c> 成功的客户端替身（同上）。</summary>
    private static Mud.Feishu.IFeishuTenantV2AppCardMessageStream BeginSucceedingCardClient()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2AppCardMessageStream>();
        client
            .Setup(c => c.CreateCardMessageStreamAsync(
                It.IsAny<CreateAppCardMessageStreamRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<CreateAppCardMessageStreamResult>
            {
                Code = 0,
                Data = new CreateAppCardMessageStreamResult { BizId = "biz_1" },
            });
        return client.Object;
    }

    /// <summary>脚本化子通道替身（Begin 返回固定 id；写/收尾不产生下游调用）。</summary>
    private static IMessageChannel StubChannel(string messageId)
    {
        var channel = new Mock<IMessageChannel>();
        channel.Setup(c => c.BeginAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(messageId);
        channel.Setup(c => c.WriteStreamAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        channel.Setup(c => c.FlushAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return channel.Object;
    }
}

/// <summary>
/// 通道状态生命周期检查器（可被合成类型直接驱动 ⇒ 支持检查器自证）。
/// </summary>
internal static class ChannelStateLifecycleChecker
{
    /// <summary>
    /// 驱动一次完整生命周期（Begin → Write → Flush）并报告残留状态。
    /// </summary>
    /// <param name="channelType">通道类型（用于定位输出）。</param>
    /// <param name="channel">已构造的通道实例。</param>
    /// <returns>违规描述（空 = 通过）。</returns>
    public static IReadOnlyList<string> FindResidualState(Type channelType, IMessageChannel channel)
    {
        try
        {
            // 同步等待是刻意的：本检查器只被测试驱动，且需要在一处拿到"生命周期结束后"的确定状态。
#pragma warning disable xUnit1031
            var messageId = channel.BeginAsync("app-a", "target-1").GetAwaiter().GetResult();
            channel.WriteStreamAsync("app-a", "target-1", messageId, new string('x', 500)).GetAwaiter().GetResult();
            channel.FlushAsync("app-a", "target-1", messageId).GetAwaiter().GetResult();
#pragma warning restore xUnit1031
        }
        catch (Exception ex)
        {
            // 构造/驱动失败必须显式报告（否则"检查器没跑起来"会被读成"没有残留"）。
            return [$"{channelType.Name}: 生命周期驱动失败（{ex.GetType().Name}: {ex.Message}）"];
        }

        return ReadDictionaryCounts(channel)
            .Where(static pair => pair.Value != 0)
            .Select(pair => $"{channelType.Name}.{pair.Key} = {pair.Value}")
            .ToArray();
    }

    /// <summary>反射读取实例内全部 <c>ConcurrentDictionary&lt;string, T&gt;</c> 字段的计数（含继承链）。</summary>
    /// <param name="instance">通道实例。</param>
    /// <returns>字段名 → 计数。</returns>
    public static IReadOnlyDictionary<string, int> ReadDictionaryCounts(object instance)
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);

        for (var type = instance.GetType(); type is not null && type != typeof(object); type = type.BaseType)
        {
            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                if (!IsStringKeyedConcurrentDictionary(field.FieldType))
                {
                    continue;
                }

                if (field.GetValue(instance) is System.Collections.ICollection collection)
                {
                    counts[$"{type.Name}.{field.Name}"] = collection.Count;
                }
            }
        }

        return counts;
    }

    /// <summary>字段类型是否为 <c>ConcurrentDictionary&lt;string, *&gt;</c>。</summary>
    private static bool IsStringKeyedConcurrentDictionary(Type type)
        => type.IsGenericType
           && type.GetGenericTypeDefinition() == typeof(ConcurrentDictionary<,>)
           && type.GetGenericArguments()[0] == typeof(string);
}

/// <summary>人为泄漏的合成通道（检查器自证用；不进生产代码）。</summary>
internal sealed class LeakingChannel : IMessageChannel
{
    private readonly ConcurrentDictionary<string, string> _neverCleared = new(StringComparer.Ordinal);

    public Task<string> BeginAsync(string appKey, string chatId, CancellationToken cancellationToken = default)
    {
        _neverCleared["biz_1"] = chatId;
        return Task.FromResult("biz_1");
    }

    public Task WriteStreamAsync(string appKey, string chatId, string messageId, string delta, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task FlushAsync(string appKey, string chatId, string messageId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
