// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;

namespace Mud.Demo.Tools;

/// <summary>工具风险等级（剖面 <c>RiskEnumFullName</c> 槽指向的消费方枚举；成员名由引擎契约锁定）。</summary>
public enum DemoToolRisk
{
    /// <summary>只读。</summary>
    Read = 0,

    /// <summary>写入。</summary>
    Write = 1,

    /// <summary>高危写入（危险词表命中，如 delete/create）。</summary>
    HighRiskWrite = 2,
}

/// <summary>工具执行结果（剖面 <c>ResultTypeName</c> 槽；执行器方法签名契约的返回类型）。</summary>
public sealed class DemoToolResult
{
    /// <summary>模型可见的结果文本（demo 用 JSON 文本示意）。</summary>
    public string Payload { get; init; } = string.Empty;
}

/// <summary>工具绑定标记（剖面 <c>BindingTypeName</c> 槽；域注册器构造实参）。</summary>
public sealed class DemoToolBinding
{
    /// <summary>绑定说明（demo 用）。</summary>
    public string Name { get; init; } = "default";
}

/// <summary>
/// 工具注册表：名字 → 执行器的运行时白名单（消费方手写；生成的域注册器经
/// <see cref="DemoToolRegistration.RegisterExecution"/> 向其登记执行器）。
/// </summary>
public sealed class DemoToolRegistry
{
    private sealed record Entry(DemoToolBinding Binding, Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<DemoToolResult>> Executor);

    private readonly ConcurrentDictionary<string, Entry> _tools = new(StringComparer.Ordinal);

    /// <summary>已注册工具名（只读快照）。</summary>
    public IReadOnlyCollection<string> ToolNames => _tools.Keys.ToArray();

    /// <summary>登记一个工具执行器（同工具重复注册以最后一次为准——demo 从简）。</summary>
    public void Register(string toolName, DemoToolBinding binding, Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<DemoToolResult>> executor)
        => _tools[toolName] = new Entry(binding, executor);

    /// <summary>按工具名调用执行器（白名单外的名字直接拒绝——工具面调用入口的唯一形态）。</summary>
    public Task<DemoToolResult> InvokeAsync(string toolName, IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken = default)
        => _tools.TryGetValue(toolName, out var entry)
            ? entry.Executor(args, cancellationToken)
            : throw new InvalidOperationException($"工具 '{toolName}' 未注册（白名单外调用）。");
}

/// <summary>域注册器接口（生成的 <c>DemoToolDomainRegistrars/*</c> 产物实现本接口）。</summary>
public interface IDemoToolDomainRegistrar
{
    /// <summary>把执行器集合登记进注册表。</summary>
    void Register(DemoToolRegistry registry);
}

/// <summary>
/// 执行器登记助手（生成产物按固定形态调用：
/// <c>DemoToolRegistration.RegisterExecution(registry, DemoToolNames.{Tool}, binding, (args, ct) => executor.{Method}(args, ct))</c>）。
/// </summary>
public static class DemoToolRegistration
{
    /// <summary>把一枚执行器绑定登记进注册表。</summary>
    public static void RegisterExecution(
        DemoToolRegistry registry,
        string toolName,
        DemoToolBinding binding,
        Func<IReadOnlyDictionary<string, object?>, CancellationToken, Task<DemoToolResult>> executor)
        => registry.Register(toolName, binding, executor);
}

/// <summary>
/// 参数解包 helper 集（生成的 <c>DemoToolArgs/*</c> 产物按类型读取器引用本类成员；
/// 类型 → 读取器映射表在引擎侧锁定：string / string[] / int / bool + 必填/可选两态）。
/// </summary>
public static class ToolArgs
{
    /// <summary>读取必填字符串（缺失或为空即抛）。</summary>
    public static string RequireString(IReadOnlyDictionary<string, object?> args, string name)
        => args.TryGetValue(name, out var value) && value is string text && text.Length > 0
            ? text
            : throw new ArgumentException($"缺少必填参数 {name}。", name);

    /// <summary>读取可选字符串。</summary>
    public static string? OptionalString(IReadOnlyDictionary<string, object?> args, string name)
        => args.TryGetValue(name, out var value) ? value as string : null;

    /// <summary>读取必填字符串数组（缺失或为空即抛）。</summary>
    public static string[] RequireStringArray(IReadOnlyDictionary<string, object?> args, string name)
        => args.TryGetValue(name, out var value) && value is string[] array && array.Length > 0
            ? array
            : throw new ArgumentException($"缺少必填参数 {name}。", name);

    /// <summary>读取可选字符串数组。</summary>
    public static string[]? OptionalStringArray(IReadOnlyDictionary<string, object?> args, string name)
        => args.TryGetValue(name, out var value) && value is string[] array ? array : null;

    /// <summary>读取必填整数（缺失即抛）。</summary>
    public static int RequireInt(IReadOnlyDictionary<string, object?> args, string name)
        => args.TryGetValue(name, out var value) && value is int number
            ? number
            : throw new ArgumentException($"缺少必填参数 {name}。", name);

    /// <summary>读取可选整数。</summary>
    public static int? OptionalInt(IReadOnlyDictionary<string, object?> args, string name)
        => args.TryGetValue(name, out var value) ? value as int? : null;

    /// <summary>读取可选布尔。</summary>
    public static bool? OptionalBool(IReadOnlyDictionary<string, object?> args, string name)
        => args.TryGetValue(name, out var value) ? value as bool? : null;
}

