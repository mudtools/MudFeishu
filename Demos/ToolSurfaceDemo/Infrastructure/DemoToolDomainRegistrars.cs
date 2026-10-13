// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Demo.Tools;

namespace Mud.Demo.Tools.Registration;

/// <summary>
/// 域注册器收集助手（<b>必须</b>位于剖面 <c>RegistrationNamespace</c>）。
/// </summary>
/// <remarks>
/// <para>
/// 每个执行器类的 DI 核心方法（生成的 <c>DemoToolsServiceCollectionCoreExtensions.AddDemo{X}Core</c>）
/// 都会调用 <see cref="Add"/> 登记一个「解析该执行器 → 构造域注册器」的工厂。
/// 本 demo 采用<b>惰性物化</b>实现：工厂先暂存，<see cref="BuildRegistry"/> 在容器构建完成后
/// 统一解析执行器并物化注册表——与生成的软缺席语义天然配合（执行器缺席 → 工厂返回 null → 该域不注册）。
/// </para>
/// <para>生产实现可改为挂接 IServiceCollection 注册链；demo 从简，直接演示物化时点。</para>
/// </remarks>
public static class DemoToolDomainRegistrars
{
    private static readonly object SyncRoot = new();
    private static readonly List<Func<IServiceProvider, IDemoToolDomainRegistrar?>> RegistrarFactories = [];

    /// <summary>登记一个域注册器工厂（由生成的 DI 核心方法逐执行器调用）。</summary>
    public static void Add(IServiceCollection services, Func<IServiceProvider, IDemoToolDomainRegistrar?> factory)
    {
        _ = services;
        lock (SyncRoot)
        {
            RegistrarFactories.Add(factory);
        }
    }

    /// <summary>重置已登记工厂（多容器/重复演示场景防串台）。</summary>
    public static void Reset()
    {
        lock (SyncRoot)
        {
            RegistrarFactories.Clear();
        }
    }

    /// <summary>物化注册表：解析全部执行器 → 逐域注册器登记工具（执行器缺席的域自然缺席）。</summary>
    public static DemoToolRegistry BuildRegistry(IServiceProvider services)
    {
        var registry = new DemoToolRegistry();
        lock (SyncRoot)
        {
            foreach (var factory in RegistrarFactories)
            {
                factory(services)?.Register(registry);
            }
        }

        return registry;
    }
}
