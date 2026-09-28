// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Mud.Feishu.AI.Tools.Extraction;

/// <summary>
/// 执行器构造参数在 DI 中的解析类别（<b>由符号事实推导，不由人声明</b>）。
/// </summary>
/// <remarks>
/// <para>
/// 判定只用两条符号事实：<b>参数是否为接口</b> + <b>接口所在命名空间是否为 <c>Mud.Feishu*</c></b>
/// + <b>参数是否可空</b>。三者的组合恰好覆盖工具面全部 16 个执行器的真实差异，
/// <b>无需 per-domain 逃生舱</b>：
/// </para>
/// <list type="bullet">
/// <item><c>IFeishuTenantV1BitableAppTable</c>（Mud.Feishu，不可空）→ <see cref="SoftService"/>；</item>
/// <item><c>IFeishuUserV2Task?</c>（Mud.Feishu，<b>可空</b>）→ <see cref="OptionalService"/>——
/// 这是 <c>TaskTools</c> 的关键：user 客户端缺席时执行器<b>仍要构造</b>（工具级报错），
/// 若误判为软缺席会让 <c>task.list_my_tasks</c> 从注册表消失（语义变更）；</item>
/// <item><c>IRetriever</c>（Mud.Feishu.AI.Knowledge）/ <c>IFeishuAttachmentStager</c>（Mud.Feishu.AI.Tools）
/// → <see cref="SoftService"/>（前者的缺席 = 知识域不注册，后者的缺席 = 上传工具不注册，与既有契约一致）；</item>
/// <item><c>IOptions&lt;FeishuAgentOptions&gt;</c>（Microsoft.Extensions.*）→ <see cref="RequiredService"/>。</item>
/// </list>
/// </remarks>
internal enum ToolDependencyKind
{
    /// <summary>软缺席候选：缺席 → 执行器为 <see langword="null"/> → 注册器不注册 → 该域工具不进注册表。</summary>
    SoftService = 0,

    /// <summary>可选外部服务：缺席时按 <see langword="null"/> 传入，<b>不</b>触发软缺席。</summary>
    OptionalService = 1,

    /// <summary>必需服务：缺席时由容器直接抛（配置类依赖如 <c>IOptions&lt;T&gt;</c> 属此列）。</summary>
    RequiredService = 2,
}

/// <summary>执行器主构造器的一个参数（值模型，供增量管线缓存）。</summary>
internal sealed class ToolExecutorDependency : IEquatable<ToolExecutorDependency?>
{
    public ToolExecutorDependency(string typeName, ToolDependencyKind kind)
    {
        TypeName = typeName;
        Kind = kind;
    }

    /// <summary>全限定类型名（含 <c>global::</c> 前缀，可直接写入产物）。</summary>
    public string TypeName { get; }

    /// <summary>解析类别。</summary>
    public ToolDependencyKind Kind { get; }

    public bool Equals(ToolExecutorDependency? other)
        => other is not null
            && Kind == other.Kind
            && string.Equals(TypeName, other.TypeName, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ToolExecutorDependency);

    public override int GetHashCode()
    {
        unchecked
        {
            return (StringComparer.Ordinal.GetHashCode(TypeName) * 31) + (int)Kind;
        }
    }
}

/// <summary>
/// 一枚「工具 ↔ 执行器方法」绑定（<c>[FeishuToolHandler]</c> 的编译期投影，值模型）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要方法级声明</b>：tool → 执行器方法<b>不可派生</b>——<c>CapabilityEntry.MethodName</c> 是
/// <c>Source</c> 挂钩解析出的 <b>SDK 方法名</b>（<c>GetAppTablePageListAsync</c>），而注册器需要的是
/// 执行器方法名（<c>ListTablesAsync</c>），二者既不等同也无命名律可依（31 枚工具中 12 枚不满足任何
/// 简单约定）。故映射必须显式声明；把它放在<b>执行器方法上</b>（而非单独的注册器文件里）使
/// 「新增工具」从 3 处编辑降为 2 处，且工具名经 <c>FeishuToolNames</c> 常量传递 = 编译期校验。
/// </para>
/// <para>
/// <b>注册器分组 = 执行器类</b>（不再依赖「模块」这一维度）：跨 <c>im</c>/<c>bitable</c>/<c>approval</c>
/// 三模块的写域因此自然拆成三个注册器，「一模块一执行器」的伪约束与随之而来的特例分支一并消失。
/// </para>
/// </remarks>
internal sealed class ToolHandlerBinding : IEquatable<ToolHandlerBinding?>
{
    public ToolHandlerBinding(
        string toolName,
        string executorType,
        string executorTypeName,
        string registrarTypeName,
        string coreMethodName,
        string methodName,
        IReadOnlyList<ToolExecutorDependency> dependencies)
    {
        ToolName = toolName;
        ExecutorType = executorType;
        ExecutorTypeName = executorTypeName;
        RegistrarTypeName = registrarTypeName;
        CoreMethodName = coreMethodName;
        MethodName = methodName;
        Dependencies = dependencies;
    }

    /// <summary>模型可见工具名（与 <c>FeishuToolNames</c> 契约同源）。</summary>
    public string ToolName { get; }

    /// <summary>执行器类型的全限定名（含 <c>global::</c>）。</summary>
    public string ExecutorType { get; }

    /// <summary>执行器类型的简单名（注册器/核心方法命名与诊断用）。</summary>
    public string ExecutorTypeName { get; }

    /// <summary>生成的域注册器类型名。</summary>
    public string RegistrarTypeName { get; }

    /// <summary>生成的 DI 核心方法名。</summary>
    public string CoreMethodName { get; }

    /// <summary>执行器方法名。</summary>
    public string MethodName { get; }

    /// <summary>执行器主构造器参数（DI 装配的事实来源）。</summary>
    public IReadOnlyList<ToolExecutorDependency> Dependencies { get; }

    /// <summary>由执行器类型名派生注册器类型名（<c>BitableTools</c> → <c>BitableToolDomainRegistrar</c>）。</summary>
    public static string BuildRegistrarTypeName(string executorTypeName)
    {
        const string ToolsSuffix = "Tools";
        var stem = executorTypeName.EndsWith(ToolsSuffix, StringComparison.Ordinal)
            ? executorTypeName.Substring(0, executorTypeName.Length - ToolsSuffix.Length)
            : executorTypeName;

        return stem + "ToolDomainRegistrar";
    }

    /// <summary>由执行器类型名派生 DI 核心方法名（<c>BitableTools</c> → <c>AddFeishuBitableToolsCore</c>）。</summary>
    public static string BuildCoreMethodName(string executorTypeName) => "AddFeishu" + executorTypeName + "Core";

    public bool Equals(ToolHandlerBinding? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return string.Equals(ToolName, other.ToolName, StringComparison.Ordinal)
            && string.Equals(ExecutorType, other.ExecutorType, StringComparison.Ordinal)
            && string.Equals(ExecutorTypeName, other.ExecutorTypeName, StringComparison.Ordinal)
            && string.Equals(RegistrarTypeName, other.RegistrarTypeName, StringComparison.Ordinal)
            && string.Equals(CoreMethodName, other.CoreMethodName, StringComparison.Ordinal)
            && string.Equals(MethodName, other.MethodName, StringComparison.Ordinal)
            && DependenciesEqual(Dependencies, other.Dependencies);
    }

    public override bool Equals(object? obj) => Equals(obj as ToolHandlerBinding);

    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + comparer.GetHashCode(ToolName);
            hash = (hash * 31) + comparer.GetHashCode(ExecutorType);
            hash = (hash * 31) + comparer.GetHashCode(ExecutorTypeName);
            hash = (hash * 31) + comparer.GetHashCode(RegistrarTypeName);
            hash = (hash * 31) + comparer.GetHashCode(CoreMethodName);
            hash = (hash * 31) + comparer.GetHashCode(MethodName);
            hash = (hash * 31) + Dependencies.Count;
            foreach (var dependency in Dependencies)
            {
                hash = (hash * 31) + dependency.GetHashCode();
            }

            return hash;
        }
    }

    private static bool DependenciesEqual(
        IReadOnlyList<ToolExecutorDependency> a,
        IReadOnlyList<ToolExecutorDependency> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].Equals(b[i]))
            {
                return false;
            }
        }

        return true;
    }
}

/// <summary>
/// 一次 <c>[FeishuToolHandler]</c> 扫描的产物：可发射绑定（可能为 <see langword="null"/>）+ 待上报诊断。
/// </summary>
/// <remarks>
/// <b>工具名独立于绑定保留</b>：签名不合法（<c>MUDFT024</c>）时绑定为 <see langword="null"/>，但工具名
/// 仍要传给发射器——否则「已声明但形态错误」会被同时报成「未绑定」（同一根因两条诊断）。
/// </remarks>
internal sealed class ScannedHandler : IEquatable<ScannedHandler?>
{
    private ScannedHandler(string? toolName, ToolHandlerBinding? binding, PendingDiagnostic[] diagnostics)
    {
        ToolName = toolName;
        Binding = binding;
        Diagnostics = diagnostics;
    }

    /// <summary>特性声明的工具名（特性参数非法时为 <see langword="null"/>）。</summary>
    public string? ToolName { get; }

    /// <summary>可发射绑定（形态非法时为 <see langword="null"/>）。</summary>
    public ToolHandlerBinding? Binding { get; }

    /// <summary>待上报诊断。</summary>
    public IReadOnlyList<PendingDiagnostic> Diagnostics { get; }

    /// <summary>构造合法绑定。</summary>
    public static ScannedHandler Ok(string toolName, ToolHandlerBinding binding)
        => new(toolName, binding, []);

    /// <summary>构造不合法绑定（仅诊断；工具名可用于抑制"未绑定"误报）。</summary>
    public static ScannedHandler Faulted(string? toolName, params PendingDiagnostic[] diagnostics)
        => new(toolName, null, diagnostics);

    public bool Equals(ScannedHandler? other)
        => other is not null
            && string.Equals(ToolName ?? string.Empty, other.ToolName ?? string.Empty, StringComparison.Ordinal)
            && Equals(Binding, other.Binding)
            && DiagnosticsEqual(Diagnostics, other.Diagnostics);

    public override bool Equals(object? obj) => Equals(obj as ScannedHandler);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ToolName ?? string.Empty);
            hash = (hash * 31) + (Binding?.GetHashCode() ?? 0);
            hash = (hash * 31) + Diagnostics.Count;
            foreach (var diagnostic in Diagnostics)
            {
                hash = (hash * 31) + diagnostic.GetHashCode();
            }

            return hash;
        }
    }

    private static bool DiagnosticsEqual(IReadOnlyList<PendingDiagnostic> a, IReadOnlyList<PendingDiagnostic> b)
        => a.Count == b.Count && !a.Where((item, index) => !item.Equals(b[index])).Any();
}
