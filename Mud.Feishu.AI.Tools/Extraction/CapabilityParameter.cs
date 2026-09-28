// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System;

namespace Mud.Feishu.AI.Tools.Extraction;

/// <summary>
/// 能力条目的参数描述。
/// </summary>
/// <remarks>
/// <para>
/// <see cref="SchemaFragmentJson"/> 是<b>类型系统已完成推导</b>的 JSON Schema 片段
/// （由 <see cref="ParameterSchemaRenderer"/> 从 Roslyn 符号产出）——下游
/// <c>SchemaWriter</c> 直接拼装，<b>不再</b>做「C# 类型字符串 → JSON 类型」的二次猜测。
/// </para>
/// <para>
/// 旧设计把 <c>CsharpType</c> 字符串交给 <c>SchemaWriter.MapJsonType</c> 解析，
/// 导致三条已记录的 Schema 质量缺陷：数组 <c>items</c> 恒为 <c>string</c>、
/// 复合 DTO 一律降级为 <c>string</c>、C# <c>enum</c> 无 <c>enum</c> 约束。
/// 让<b>符号层</b>一次性推导并固化片段，从结构上消除该缺陷类别。
/// </para>
/// </remarks>
internal sealed class CapabilityParameter : IEquatable<CapabilityParameter?>
{
    public CapabilityParameter(
        string name,
        string csharpType,
        string parameterKind,
        string? docDescription,
        bool isRequired,
        bool isNullable,
        string schemaFragmentJson)
    {
        Name = name;
        CsharpType = csharpType;
        ParameterKind = parameterKind;
        DocDescription = docDescription;
        IsRequired = isRequired;
        IsNullable = isNullable;
        SchemaFragmentJson = schemaFragmentJson;
    }

    /// <summary>模型可见参数名（snake_case 契约）。</summary>
    public string Name { get; }

    /// <summary>C# 类型显示名（仅用于诊断消息，不参与 Schema 推导）。</summary>
    public string CsharpType { get; }

    /// <summary>参数类别：Path / Query / Body / Header / FormContent。</summary>
    public string ParameterKind { get; }

    /// <summary>参数文档描述（XML <c>&lt;param&gt;</c> 或 <c>[ToolParameter]</c>）。</summary>
    public string? DocDescription { get; }

    /// <summary>是否进入 Schema 的 <c>required</c>。</summary>
    public bool IsRequired { get; }

    /// <summary>是否可空。</summary>
    public bool IsNullable { get; }

    /// <summary>已推导的 JSON Schema 片段（如 <c>{"type":"array","items":{"type":"string"}}</c>）。</summary>
    public string SchemaFragmentJson { get; }

    public bool Equals(CapabilityParameter? other)
        => other is not null
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(CsharpType, other.CsharpType, StringComparison.Ordinal)
            && string.Equals(ParameterKind, other.ParameterKind, StringComparison.Ordinal)
            && string.Equals(DocDescription ?? string.Empty, other.DocDescription ?? string.Empty, StringComparison.Ordinal)
            && IsRequired == other.IsRequired
            && IsNullable == other.IsNullable
            && string.Equals(SchemaFragmentJson, other.SchemaFragmentJson, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as CapabilityParameter);

    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + comparer.GetHashCode(Name);
            hash = (hash * 31) + comparer.GetHashCode(CsharpType);
            hash = (hash * 31) + comparer.GetHashCode(ParameterKind);
            hash = (hash * 31) + comparer.GetHashCode(SchemaFragmentJson);
            hash = (hash * 31) + (IsRequired ? 1 : 0);
            hash = (hash * 31) + (IsNullable ? 1 : 0);
            return hash;
        }
    }
}

/// <summary>工具身份维度（由接口名令牌词推导）。</summary>
internal enum ToolIdentity
{
    /// <summary>租户令牌（IFeishuTenantV*）。</summary>
    Tenant = 0,

    /// <summary>用户令牌（IFeishuUserV*）。</summary>
    User = 1,

    /// <summary>双令牌基接口（IFeishuV*，不直接产工具）。</summary>
    Both = 2,
}

/// <summary>工具风险分级。</summary>
internal enum ToolRisk
{
    /// <summary>只读（GET）。</summary>
    Read = 0,

    /// <summary>写操作（POST/PUT/PATCH/DELETE）。</summary>
    Write = 1,

    /// <summary>高风险写操作（命中危险词表）。</summary>
    HighRiskWrite = 2,
}
