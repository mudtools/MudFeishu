// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Mud.Demo.Sdk;
using Mud.Demo.Tools;
using Mud.Demo.Tools.Generated;
using Mud.Demo.Tools.Registration;
using ToolSurfaceDemo.Infrastructure;

namespace ToolSurfaceDemo;

/// <summary>
/// 工具面（ToolSurface）代码生成功能演示：剖面声明 → 编译期产物 → 运行时白名单调用。
/// </summary>
/// <remarks>
/// 编译本工程即触发 <c>ToolSurfaceSourceGenerator</c>（随 Mud.HttpUtils.Generator 以分析器形态接入），
/// 产出 8 条输出路径的产物（均可在 IDE 的「分析器 → ToolSurfaceSourceGenerator」节点查看源码）：
/// <list type="number">
/// <item><c>DemoToolSchemas.g.cs</c> —— 描述符 JSON 常量（模型可见的「工具是什么」）；</item>
/// <item><c>DemoToolNames.g.cs</c> —— 工具名契约表（读写分组，授权门禁的数据源）；</item>
/// <item><c>DemoToolContracts.g.cs</c> —— 类型化契约表（风险/身份/HTTP 事实）；</item>
/// <item><c>DemoToolArgs/*.g.cs</c> —— 参数解包类型（执行器把「字典取值」收敛为一次 Unpack）；</item>
/// <item><c>DemoToolDomainRegistrars/*.g.cs</c> —— 每执行器一个域注册器；</item>
/// <item><c>DemoToolsServiceCollectionCoreExtensions.g.cs</c> —— 逐执行器 DI 装配（软缺席语义）；</item>
/// <item><c>DemoToolGuidance.g.cs</c> —— Guidance 资产（AdditionalFiles 中的 /Guidance/ markdown）；</item>
/// <item><c>DemoToolCapabilityCatalog.g.cs</c> + <c>DemoToolMethodCatalog.g.cs</c> —— 能力/方法目录（开关 = build_property.DemoToolCatalog）。</item>
/// </list>
/// </remarks>
public static class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Mud.HttpUtils 工具面（ToolSurface）代码生成演示 ===");
        Console.WriteLine("剖面 = DemoToolProfile；产物全部由编译期生成，本程序不含任何手写的工具登记代码。\n");

        // 多容器场景防串台：重置上一轮演示登记的域注册器工厂。
        DemoToolDomainRegistrars.Reset();

        await using var provider = BuildServices();

        // 1. 白名单注册表：生成的域注册器 + 手写收集助手共同物化。
        var registry = DemoToolDomainRegistrars.BuildRegistry(provider);
        Console.WriteLine("1) 工具白名单（DemoToolNames 读写分组是授权门禁的单一真相源）：");
        Console.WriteLine($"   ReadonlyAll = [{string.Join(", ", DemoToolNames.ReadonlyAll)}]");
        Console.WriteLine($"   WriteAll    = [{string.Join(", ", DemoToolNames.WriteAll)}]");
        Console.WriteLine($"   注册表实际登记 = [{string.Join(", ", registry.ToolNames)}]");

        // 2. 描述符 JSON：厂商扩展键 x-demo、令牌身份、参数/输出 Schema 全部编译期固化。
        Console.WriteLine("\n2) 工具描述符（DemoToolSchemas，进入 golden 快照的字节面）：");
        Console.WriteLine($"   {DemoToolNames.DocList} => {DemoToolSchemas.doc_listSchemaJson}");
        Console.WriteLine($"   {DemoToolNames.DocDelete} => {DemoToolSchemas.doc_deleteSchemaJson}");

        // 3. 类型化契约：同一 pass 的结构化视图（风险/身份/HTTP 事实）。
        Console.WriteLine("\n3) 类型化契约（DemoToolContracts）：");
        foreach (var name in DemoToolContracts.AllNames)
        {
            var contract = DemoToolContracts.ByToolName[name];
            Console.WriteLine(
                $"   {contract.Name,-11} risk={contract.Risk,-13} identity={contract.Identity,-7} " +
                $"isWrite={contract.IsWrite} http={contract.HttpMethod} {contract.Route}");
        }

        // 4. Guidance 资产：/Guidance/ 下的 markdown 编译期编入常量表（guidance_read 元工具的数据源）。
        Console.WriteLine("\n4) Guidance 资产（DemoToolGuidance）：");
        Console.WriteLine($"   ByDomain[doc]     = {DemoToolGuidance.ByDomain["doc"].Split('\n')[0]}");
        Console.WriteLine($"   ReferenceKeys     = [{string.Join(", ", DemoToolGuidance.ReferenceKeys)}]");

        // 5. 能力/方法目录：SDK 覆盖率与策展进度（开关 = build_property.DemoToolCatalog = true）。
        Console.WriteLine("\n5) 能力目录（DemoToolCapabilityCatalog / DemoToolMethodCatalog）：");
        Console.WriteLine(
            $"   SdkMethodCount={DemoToolCapabilityCatalog.SdkMethodCount}, " +
            $"CuratedToolCount={DemoToolCapabilityCatalog.CuratedToolCount}, " +
            $"DomainCount={DemoToolCapabilityCatalog.DomainCount}");
        Console.WriteLine(
            $"   MethodsByDomain = [{string.Join(", ", DemoToolCapabilityCatalog.MethodsByDomain.Select(static pair => $"{pair.Key}:{pair.Value}"))}]");
        Console.WriteLine($"   方法目录条目数 = {DemoToolMethodCatalog.ByQualifiedName.Count}");

        // 6. 模拟一次模型 tool_call：注册表白名单 → 执行器 → 生成的 Args 解包 → SDK。
        Console.WriteLine("\n6) 模拟模型调用：");
        var listed = await registry.InvokeAsync(
            DemoToolNames.DocList,
            new Dictionary<string, object?> { ["folderId"] = "docs/2026" });
        Console.WriteLine($"   doc.list(folderId=docs/2026) => {listed.Payload}");

        var deleted = await registry.InvokeAsync(
            DemoToolNames.DocDelete,
            new Dictionary<string, object?> { ["docId"] = "doc-003" });
        Console.WriteLine($"   doc.delete(docId=doc-003)    => {deleted.Payload}");

        // 7. 白名单外调用被注册表拒绝（工具面唯一调用入口的 fail-fast 形态）。
        Console.WriteLine("\n7) 白名单外调用：");
        try
        {
            await registry.InvokeAsync("doc.purge", new Dictionary<string, object?>());
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"   已拒绝：{ex.Message}");
        }

        Console.WriteLine("\n=== 演示完成 ===");
    }

    /// <summary>DI 装配：SDK 实现 + 绑定单例 + 生成的逐执行器核心装配（软缺席：SDK 接口缺席则该域不注册）。</summary>
    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        var sdk = new InMemoryDocSdk();
        services.AddSingleton<IMudDemoTenantV1Doc>(sdk);
        services.AddSingleton<IMudDemoTenantV1DocTrash>(sdk);
        services.AddSingleton(new DemoToolBinding());

        // 生成产物：Mud.Demo.Tools.DemoToolsServiceCollectionCoreExtensions.AddDemoDocToolsCore
        // （方法名 = Add{SDK名}{执行器类名}Core，由剖面 Name 槽与执行器类型名派生）。
        services.AddDemoDocToolsCore();
        return services.BuildServiceProvider();
    }
}
