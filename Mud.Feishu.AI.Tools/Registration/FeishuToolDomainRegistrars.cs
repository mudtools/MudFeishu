// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Mud.Feishu.AI.FeishuTools.Registration;

/// <summary>
/// 域级工具注册器契约（AI-FD-D12 P1D-1c，internal）：每域一个注册器——把该域工具注册进
/// <see cref="FeishuToolRegistry"/>。<b>客户端缺席 → 执行器缺席 → 注册器不注册</b>：域缺失表现为
/// 「该域工具不在注册表」，白名单映射期 fail-fast 报「未注册」——错误面从启动崩溃收敛为白名单期明确报错。
/// </summary>
/// <remarks>
/// 本契约与 <see cref="FeishuToolRegistration"/> 保持手写（稳定契约、单文件、生成无收益）；
/// <b>实现类</b>（<c>{Executor}ToolDomainRegistrar</c>）由 <c>ToolRegistrarEmitter</c> 编译期产出
/// （见 <c>FeishuToolDomainRegistrars/{Registrar}.g.cs</c>，每执行器一个文件）——手写实现已全部删除，未发布无需过渡期。
/// </remarks>
internal interface IFeishuToolDomainRegistrar
{
    /// <summary>把该域工具注册进注册表（执行器缺席时不注册任何工具）。</summary>
    /// <param name="registry">工具注册表（宿主构建期单线程调用）。</param>
    void Register(FeishuToolRegistry registry);
}

/// <summary>
/// 注册表登记助手：从编译期类型化契约构造定义并注册（各域注册器共用）。
/// </summary>
/// <remarks>
/// <para>
/// <b>WP2（R-B 根因）</b>：契约消费点改为 <see cref="FeishuToolContracts"/>（生成器发射的类型化
/// 契约表，与 Schema JSON 常量出自同一 pass）——此前的 <c>FromSchema</c> 运行期解析
/// （<c>JsonDocument.Parse</c> + 3 个失败分支）整体删除：契约漏字段现在是<b>编译期错误</b>
/// 而不是运行期 <c>InvalidOperationException</c>。
/// </para>
/// </remarks>
internal static class FeishuToolRegistration
{
    /// <summary>按名注册工具（缺编译期契约即 fail-fast——契约守卫防漂移）。</summary>
    public static FeishuToolDefinition RegisterTool(FeishuToolRegistry registry, string toolName, FeishuToolHandler handler)
    {
        if (!FeishuToolContracts.ByToolName.TryGetValue(toolName, out var contract))
        {
            throw new InvalidOperationException(
                $"工具 '{toolName}' 缺少编译期契约——接口须标注 [FeishuTool]（契约守卫防漂移）");
        }

        var definition = new FeishuToolDefinition(
            contract.Name, contract.Description, contract.RequiredScopes,
            contract.IsWrite, contract.Risk, contract.Identity, handler);
        registry.Register(definition);
        return definition;
    }

    /// <summary>按名取已注册定义（执行期调用；注册顺序保证存在性）。</summary>
    public static FeishuToolDefinition Def(FeishuToolRegistry registry, string toolName)
        => registry.TryGet(toolName, out var definition) && definition is not null
            ? definition
            : throw new InvalidOperationException($"工具 '{toolName}' 尚未注册（注册顺序错误）");

    /// <summary>
    /// 注册一枚"经执行链包裹的执行器方法"工具（AT-F16(a) / R3 评审 C-6：<b>纯样板抽取，零新机制</b>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 取代此前在 11 个域注册器里逐行重复的三段式写法：
    /// <c>RegisterTool(registry, X, (args, ctx, ct) =&gt; binding.ExecuteAsync(Def(registry, X), args, ctx, token =&gt; executor.YAsync(args, token), ct))</c>。
    /// 抽出的只是"取定义 → 执行链包裹 → 传参闭包"这一机械结构，<b>不引入任何新特性、抽象或包</b>
    /// （对照 `[ToolProjection]` 那类声明式方案——后者属新造机制，已明确另立批次）。
    /// </para>
    /// <para>
    /// <b>定义必须在执行期经 <see cref="Def"/> 反查</b>（而非在注册期捕获）：工具定义本身持有 handler，
    /// 而 handler 又需要定义来走执行链——捕获会造成初始化循环。注册表在构建期单线程填充、
    /// 运行期只读，故该反查是纯字典命中，无并发与性能代价。
    /// </para>
    /// </remarks>
    /// <param name="registry">工具注册表。</param>
    /// <param name="toolName">工具名（须与 <c>[FeishuTool]</c> 派生的契约名一致）。</param>
    /// <param name="binding">工具执行链。</param>
    /// <param name="executorCall">分域执行器调用（入参 + 取消令牌 → 已投影/截断的结果）。</param>
    public static void RegisterExecution(
        FeishuToolRegistry registry,
        string toolName,
        FeishuToolBinding binding,
        Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<FeishuToolResult>> executorCall)
        => RegisterTool(registry, toolName, (args, ctx, ct) => binding.ExecuteAsync(
            Def(registry, toolName), args, ctx, token => executorCall(args, token), ct));
}

/// <summary>
/// 域注册器登记的稳定 seam（手写装配与<b>生成产物</b>共用）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须是独立类型而不是 <c>FeishuToolsServiceCollectionExtensions</c> 的 private 扩展</b>：
/// 生成的 DI 核心方法是<b>另一个类型</b>（<c>FeishuToolsServiceCollectionCoreExtensions</c>），
/// 跨类不可见 private 成员（R1 §4.5 的 <c>CS0122</c> 根因）。把它提为 <c>internal static</c>
/// 是"生成产物只依赖稳定 seam"的最小代价实现。
/// </para>
/// <para>
/// <b>null 语义</b>：注册器的<b>工厂</b>返回 <see langword="null"/> 表示该域执行器缺席
/// （软缺席），此时仍登记一个返回 <see langword="null"/> 的描述符——<c>BuildRegistry</c> 显式跳过
/// null（既有行为，未变更）。
/// </para>
/// <para>
/// 注：此处原写 <c>&lt;paramref name="factory"/&gt;</c>，但本项是<b>类</b>注释而 <c>factory</c> 是
/// <see cref="Add{T}"/> 的形参名——该 cref 无法解析（<c>CS1734</c>），已改为普通形参说明（R2-08）。
/// </para>
/// </remarks>
internal static class FeishuToolDomainRegistrars
{
    /// <summary>登记一个域注册器工厂。</summary>
    /// <typeparam name="T">域注册器实现类型。</typeparam>
    /// <param name="services">服务集合。</param>
    /// <param name="factory">注册器工厂（执行器缺席时返回 <see langword="null"/>）。</param>
    /// <returns>服务集合。</returns>
    public static IServiceCollection Add<T>(IServiceCollection services, Func<IServiceProvider, T?> factory)
        where T : class, IFeishuToolDomainRegistrar
        => services.AddSingleton<IFeishuToolDomainRegistrar>(sp => factory(sp)!);
}
