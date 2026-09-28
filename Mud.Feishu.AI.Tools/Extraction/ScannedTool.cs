// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Mud.Feishu.AI.Tools.Extraction;

/// <summary>
/// 一次 <c>[FeishuTool]</c> 扫描的产物：可发射模型（可能为 <see langword="null"/>）+ 待上报诊断。
/// </summary>
/// <remarks>
/// 诊断与模型<b>同源一次扫描</b>产出，避免为「模型」与「诊断」各跑一遍符号解析；
/// 两者经增量管线投影到不同出口（发射源码 / 上报诊断）。
/// </remarks>
internal sealed class ScannedTool : IEquatable<ScannedTool?>
{
    private ScannedTool(string interfaceName, ToolSchemaModel? model, PendingDiagnostic[] diagnostics)
    {
        InterfaceName = interfaceName;
        Model = model;
        Diagnostics = diagnostics;
    }

    /// <summary>承载接口名（诊断定位与日志上下文）。</summary>
    public string InterfaceName { get; }

    /// <summary>可发射模型（<see langword="null"/> 表示该接口不可产工具，仅产诊断）。</summary>
    public ToolSchemaModel? Model { get; }

    /// <summary>待上报诊断。</summary>
    public IReadOnlyList<PendingDiagnostic> Diagnostics { get; }

    /// <summary>构造成功扫描结果。</summary>
    public static ScannedTool Ok(string interfaceName, ToolSchemaModel model)
        => new(interfaceName, model, []);

    /// <summary>构造仅含诊断的扫描结果。</summary>
    public static ScannedTool Faulted(string interfaceName, params PendingDiagnostic[] diagnostics)
        => new(interfaceName, null, diagnostics);

    /// <summary>构造模型 + 附加诊断（如源挂钩告警）的扫描结果。</summary>
    public static ScannedTool OkWithDiagnostics(string interfaceName, ToolSchemaModel model, params PendingDiagnostic[] diagnostics)
        => new(interfaceName, model, diagnostics);

    public bool Equals(ScannedTool? other)
        => other is not null
            && string.Equals(InterfaceName, other.InterfaceName, StringComparison.Ordinal)
            && Equals(Model, other.Model)
            && DiagnosticsEqual(Diagnostics, other.Diagnostics);

    public override bool Equals(object? obj) => Equals(obj as ScannedTool);

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(InterfaceName);
            hash = (hash * 31) + (Model?.GetHashCode() ?? 0);
            return hash;
        }
    }

    private static bool DiagnosticsEqual(IReadOnlyList<PendingDiagnostic> a, IReadOnlyList<PendingDiagnostic> b)
        => a.Count == b.Count && !a.Where((t, i) => !t.Equals(b[i])).Any();
}

/// <summary>
/// 待上报诊断（值相等）：在增量管线内以纯数据形态流转，到 <c>SourceProductionContext</c> 才落地为
/// <see cref="Diagnostic"/>。
/// </summary>
internal sealed class PendingDiagnostic : IEquatable<PendingDiagnostic?>
{
    private PendingDiagnostic(DiagnosticDescriptor descriptor, object[] arguments)
    {
        Descriptor = descriptor;
        Arguments = arguments;
    }

    /// <summary>诊断描述符（与 <c>Diagnostics</c> 定义同源）。</summary>
    public DiagnosticDescriptor Descriptor { get; }

    /// <summary>消息实参。</summary>
    public object[] Arguments { get; }

    /// <summary>由描述符与实参构造。</summary>
    public static PendingDiagnostic Create(DiagnosticDescriptor descriptor, params object[] arguments)
        => new(descriptor, arguments);

    private string Signature => Descriptor.Id + "\u0001" + string.Join("\u0002", Arguments.Select(static a => a?.ToString() ?? string.Empty));

    public bool Equals(PendingDiagnostic? other) => other is not null && Signature == other.Signature;

    public override bool Equals(object? obj) => Equals(obj as PendingDiagnostic);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Signature);
}
