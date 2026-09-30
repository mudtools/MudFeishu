// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Mud.Feishu.AI.Tools.Extraction;

/// <summary>
/// L1 能力目录条目：从 Roslyn symbol 抽取的方法级结构化事实（纯数据，无 Schema 字符串）。
/// </summary>
/// <remarks>
/// 手写值相等（而非 record）：生成器宿主为 netstandard2.0，record 的 init 访问器需要
/// <c>IsExternalInit</c> 垫片。值相等供增量管线缓存。
/// </remarks>
internal sealed class CapabilityEntry : IEquatable<CapabilityEntry?>
{
    public CapabilityEntry(
        string interfaceName,
        string toolName,
        ToolIdentity identity,
        string httpMethod,
        string routeTemplate,
        string methodName,
        IReadOnlyList<CapabilityParameter> parameters,
        string? docSummary,
        ToolRisk risk,
        IReadOnlyList<string> scopes,
        string? outputSchemaJson = null,
        IReadOnlyList<string>? outputSchemaTruncations = null)
    {
        InterfaceName = interfaceName;
        ToolName = toolName;
        Identity = identity;
        HttpMethod = httpMethod;
        RouteTemplate = routeTemplate;
        MethodName = methodName;
        Parameters = parameters;
        DocSummary = docSummary;
        Risk = risk;
        Scopes = scopes;
        OutputSchemaJson = outputSchemaJson;
        OutputSchemaTruncations = outputSchemaTruncations ?? [];
    }

    public string InterfaceName { get; }
    public string ToolName { get; }
    public ToolIdentity Identity { get; }
    public string HttpMethod { get; }
    public string RouteTemplate { get; }
    public string MethodName { get; }
    public IReadOnlyList<CapabilityParameter> Parameters { get; }
    public string? DocSummary { get; }
    public ToolRisk Risk { get; }
    public IReadOnlyList<string> Scopes { get; }

    /// <summary>
    /// 工具返回值的 JSON Schema（可空；由 <see cref="Schema.TypeSchemaResolver"/> 从 SDK 返回类型推导）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 消费面（R2-05 决策后的真实消费点，两条皆为<b>构建期</b>信号）：
    /// ① 写入描述符信封 <c>x-feishu.output_schema</c>（供宿主与生成器产物做契约比对）；
    /// ② 驱动 <c>CoverageReport.OutputSchemaRate</c> 与截断告警 <c>MUDFT009</c>。
    /// </para>
    /// <para>
    /// <b>不接线到运行时结果投影</b>：曾计划的 <c>SchemaProjection.Project</c> 运行期字段裁剪
    /// 与各执行器的<b>有意策展投影</b>冲突——策展后的键名（如 <c>task_guid</c>）并不都在
    /// output_schema 的顶层字段集（如 <c>task.guid</c>）内，接线会把结果裁成空对象（R2-05 已实证并驳回）。
    /// </para>
    /// </remarks>
    public string? OutputSchemaJson { get; }

    /// <summary>
    /// 输出 Schema 推导过程中的截断样本（深度超限 / 循环引用；AT-B14）。
    /// </summary>
    /// <remarks>
    /// 由 <see cref="Schema.TypeSchemaResolver"/> 在推导时记录，生成器在拿到全部模型后<b>聚合为单条</b>
    /// <c>MUDFT009</c> 上报——逐处上报会因 SDK 中深层 DTO 众多而淹没构建输出。
    /// </remarks>
    public IReadOnlyList<string> OutputSchemaTruncations { get; }

    public bool Equals(CapabilityEntry? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(InterfaceName, other.InterfaceName, StringComparison.Ordinal)
            && string.Equals(ToolName, other.ToolName, StringComparison.Ordinal)
            && Identity == other.Identity
            && string.Equals(HttpMethod, other.HttpMethod, StringComparison.Ordinal)
            && string.Equals(RouteTemplate, other.RouteTemplate, StringComparison.Ordinal)
            && string.Equals(MethodName, other.MethodName, StringComparison.Ordinal)
            && ParametersEqual(Parameters, other.Parameters)
            && string.Equals(DocSummary ?? string.Empty, other.DocSummary ?? string.Empty, StringComparison.Ordinal)
            && Risk == other.Risk
            && ScopesEqual(Scopes, other.Scopes)
            && string.Equals(OutputSchemaJson ?? string.Empty, other.OutputSchemaJson ?? string.Empty, StringComparison.Ordinal)
            && StringsEqual(OutputSchemaTruncations, other.OutputSchemaTruncations);
    }

    public override bool Equals(object? obj) => Equals(obj as CapabilityEntry);

    /// <summary>
    /// 哈希必须覆盖 <see cref="Equals(CapabilityEntry?)"/> 认可的<b>全部</b>字段（AT-B16）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么集合字段也必须逐个入哈希</b>：此前实现只取了标量字段，集合与长字符串字段被整个跳过——
    /// 两个仅在某集合元素上不同的条目会得到<b>相同</b>哈希。本类型是 Roslyn 增量管线的值键，
    /// 哈希碰撞会让"每个条目都要跑一遍逐字段 <c>Equals</c>"退化，在大型 SDK 上放大为可见的编译开销。
    /// </para>
    /// <para>
    /// <b>刻意不做"计数 + 首元素"的省算优化</b>：那种写法与"完全跳过"在当前碰撞面上等价
    /// （集合内其余元素变化仍碰撞），属"补了等于没补"；而本类型的入参是编译期元数据
    /// （集合元素数是个位数到几十），全量遍历的成本可忽略。
    /// </para>
    /// <para>契约：<c>Equals</c> 为真 ⇒ 哈希必相等（本实现同时满足，且反向碰撞面已最小化）。</para>
    /// </remarks>
    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + comparer.GetHashCode(InterfaceName);
            hash = (hash * 31) + comparer.GetHashCode(ToolName);
            hash = (hash * 31) + (int)Identity;
            hash = (hash * 31) + comparer.GetHashCode(HttpMethod);
            hash = (hash * 31) + comparer.GetHashCode(RouteTemplate);
            hash = (hash * 31) + comparer.GetHashCode(MethodName);

            hash = (hash * 31) + Parameters.Count;
            foreach (var parameter in Parameters)
            {
                hash = (hash * 31) + parameter.GetHashCode();
            }

            hash = (hash * 31) + comparer.GetHashCode(DocSummary ?? string.Empty);
            hash = (hash * 31) + (int)Risk;

            hash = (hash * 31) + Scopes.Count;
            foreach (var scope in Scopes)
            {
                hash = (hash * 31) + comparer.GetHashCode(scope);
            }

            hash = (hash * 31) + comparer.GetHashCode(OutputSchemaJson ?? string.Empty);

            hash = (hash * 31) + OutputSchemaTruncations.Count;
            foreach (var truncation in OutputSchemaTruncations)
            {
                hash = (hash * 31) + comparer.GetHashCode(truncation);
            }

            return hash;
        }
    }

    private static bool ParametersEqual(IReadOnlyList<CapabilityParameter> a, IReadOnlyList<CapabilityParameter> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!a[i].Equals(b[i])) return false;
        }
        return true;
    }

    private static bool ScopesEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
        }
        return true;
    }

    private static bool StringsEqual(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count) return false;
        for (var i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal)) return false;
        }
        return true;
    }
}
