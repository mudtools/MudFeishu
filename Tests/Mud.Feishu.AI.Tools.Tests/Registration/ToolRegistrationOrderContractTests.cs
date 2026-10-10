// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Registration;

namespace Mud.Feishu.AI.Tools.Tests.Registration;

/// <summary>
/// BUG-3（回归保险）：把"<b>执行期按名字反查注册表必然命中</b>"从注释承诺升级为可测事实。
/// </summary>
/// <remarks>
/// <para>
/// <b>被锁定的结构</b>：<c>FeishuToolRegistration.RegisterExecution</c> 把 handler 包成
/// <c>(args, ctx, ct) =&gt; binding.ExecuteAsync(Def(registry, toolName), …)</c>——定义<b>不在注册期捕获</b>
/// （定义自身持有 handler，捕获会形成初始化循环），而是执行期经字符串名反查。若这个反查会 miss，
/// 失败形态是<b>调用期</b> <c>InvalidOperationException("工具 'X' 尚未注册")</c>。
/// </para>
/// <para>
/// <b>为什么定级 P2（回归保险）而不是缺陷</b>：<c>RegisterExecution</c> 内部先调用
/// <c>RegisterTool</c>，后者以编译期契约表 <c>FeishuToolContracts.ByToolName</c> 做 fail-fast——
/// 工具名笔误在<b>注册期</b>就失败；而 <c>Def</c> 用的是同一个 <c>toolName</c>，注册成功后结构上必然命中。
/// 因此本守卫的作用是"防未来重构破坏该不变量"，不是"修一个现存缺陷"。
/// </para>
/// <para>
/// <b>覆盖全链路而非单文件</b>（R1 的 N-1 教训）：断言遍历<b>注册表里的每一个条目</b>，
/// 并对"已启用工具"做一次真实执行（经执行链），而不是只断言某个写死的工具名。
/// </para>
/// </remarks>
public class ToolRegistrationOrderContractTests
{
    private const string SchemaReadTool = "feishu.schema_read";

    private static ServiceProvider BuildProvider()
    {
        // 白名单只启用一个"无下游调用"的元工具：执行它即可端到端验证
        // "反查命中 → 执行链通行"，且不依赖任何业务客户端。
        var options = new FeishuAgentOptions { Instructions = "test", Tools = [SchemaReadTool] };

        return new ServiceCollection()
            .AddSingleton(Options.Create(options))
            // FeishuAppContextScopeFactory 的三个核心依赖（与生成的 HTTP 客户端同构，与工具域无关）。
            .AddSingleton(new Mock<Mud.HttpUtils.IAppContextHolder>().Object)
            .AddSingleton(new Mock<Mud.Feishu.Abstractions.IFeishuAppManager>().Object)
            .AddSingleton(Mock.Of<Mud.HttpUtils.IAppAccessAuthorizer>(a => a.CanSwitchTo(It.IsAny<string>())))
            .AddFeishuTools()
            .BuildServiceProvider();
    }

    [Fact]
    public void RegisteredEntry_ShouldAlwaysHitTheRunTimeLookup()
    {
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var names = registry.AllTools.Select(static t => t.Name).ToArray();

        // 分母自证：注册表必须非空，否则下面的遍历是空集假绿。
        names.Should().NotBeEmpty("装配入口必须至少注册一个工具——空注册表会让本守卫退化为假绿");

        // 注册名必须是编译期契约的子集（否则"注册名 ⊆ Schema 常量表"的守卫链断了）。
        names.Should().BeSubsetOf(FeishuToolNames.All,
            "注册名只能来自 [FeishuTool] 派生的契约表（RegisterTool 已 fail-fast，此处是第二道锁）");

        foreach (var name in names)
        {
            var lookup = () => FeishuToolRegistration.Def(registry, name);

            lookup.Should().NotThrow(
                $"'{name}' 已登记在注册表，执行期反查必须命中（BUG-3 不变量：注册即命中）");
        }
    }

    [Fact]
    public void RunTimeLookup_ShouldThrow_WhenNameIsNotRegistered()
    {
        // 负例自证：把"反查会 miss"的形态显式钉住——否则上面的 NotThrow 断言可能只是白跑（假绿）。
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();

        var lookup = () => FeishuToolRegistration.Def(registry, "ghost.not_registered");

        lookup.Should().Throw<InvalidOperationException>()
            .WithMessage("*尚未注册*", "反查 miss 的失败形态即此异常——本守卫的判据必须真的能被触发");
    }

    [Fact]
    public async Task EnabledTool_ShouldBeInvocableThroughTheExecutionChain()
    {
        // 端到端：经 RegisterExecution 包裹的 handler 必须能走完整条执行链——
        // 若 Def 未命中，这里会抛 InvalidOperationException 而不是返回结果。
        using var provider = BuildProvider();
        var registry = provider.GetRequiredService<FeishuToolRegistry>();

        var definition = registry.EnabledTools.Should().ContainSingle().Subject;
        definition.Name.Should().Be(SchemaReadTool);

        // 反查（Def）在 ExecuteAsync 的实参位置求值 ⇒ 若 miss 会先抛 InvalidOperationException
        // （"尚未注册"），根本走不到参数校验。因此用"合法入参且返回结果"来证明链路是通的。
        var result = await definition.Handler(
            new Dictionary<string, object?> { ["keyword"] = "Calendar" },
            new FeishuToolContext("cli_test"),
            CancellationToken.None);

        result.Error.Should().BeNull(
            "只读元工具（无下游调用、判据齐全）经执行链必须成功——失败即说明反查/执行链接线断了");
        result.Text.Should().NotBeNullOrEmpty("schema_read 返回方法目录 JSON 文本");
    }
}
