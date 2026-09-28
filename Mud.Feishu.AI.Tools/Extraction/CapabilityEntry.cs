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
        string moduleName,
        ToolIdentity identity,
        string httpMethod,
        string routeTemplate,
        string methodName,
        string returnTypeMetadataName,
        IReadOnlyList<CapabilityParameter> parameters,
        string? docSummary,
        string? docReturns,
        bool hasFileUpload,
        bool returnsBinary,
        ToolRisk risk,
        IReadOnlyList<string> scopes)
    {
        InterfaceName = interfaceName;
        ToolName = toolName;
        ModuleName = moduleName;
        Identity = identity;
        HttpMethod = httpMethod;
        RouteTemplate = routeTemplate;
        MethodName = methodName;
        ReturnTypeMetadataName = returnTypeMetadataName;
        Parameters = parameters;
        DocSummary = docSummary;
        DocReturns = docReturns;
        HasFileUpload = hasFileUpload;
        ReturnsBinary = returnsBinary;
        Risk = risk;
        Scopes = scopes;
    }

    public string InterfaceName { get; }
    public string ToolName { get; }
    public string ModuleName { get; }
    public ToolIdentity Identity { get; }
    public string HttpMethod { get; }
    public string RouteTemplate { get; }
    public string MethodName { get; }
    public string ReturnTypeMetadataName { get; }
    public IReadOnlyList<CapabilityParameter> Parameters { get; }
    public string? DocSummary { get; }
    public string? DocReturns { get; }
    public bool HasFileUpload { get; }
    public bool ReturnsBinary { get; }
    public ToolRisk Risk { get; }
    public IReadOnlyList<string> Scopes { get; }

    public bool Equals(CapabilityEntry? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(InterfaceName, other.InterfaceName, StringComparison.Ordinal)
            && string.Equals(ToolName, other.ToolName, StringComparison.Ordinal)
            && string.Equals(ModuleName, other.ModuleName, StringComparison.Ordinal)
            && Identity == other.Identity
            && string.Equals(HttpMethod, other.HttpMethod, StringComparison.Ordinal)
            && string.Equals(RouteTemplate, other.RouteTemplate, StringComparison.Ordinal)
            && string.Equals(MethodName, other.MethodName, StringComparison.Ordinal)
            && string.Equals(ReturnTypeMetadataName, other.ReturnTypeMetadataName, StringComparison.Ordinal)
            && ParametersEqual(Parameters, other.Parameters)
            && string.Equals(DocSummary ?? string.Empty, other.DocSummary ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(DocReturns ?? string.Empty, other.DocReturns ?? string.Empty, StringComparison.Ordinal)
            && HasFileUpload == other.HasFileUpload
            && ReturnsBinary == other.ReturnsBinary
            && Risk == other.Risk
            && ScopesEqual(Scopes, other.Scopes);
    }

    public override bool Equals(object? obj) => Equals(obj as CapabilityEntry);

    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + comparer.GetHashCode(InterfaceName);
            hash = (hash * 31) + comparer.GetHashCode(ToolName);
            hash = (hash * 31) + comparer.GetHashCode(ModuleName);
            hash = (hash * 31) + (int)Identity;
            hash = (hash * 31) + comparer.GetHashCode(HttpMethod);
            hash = (hash * 31) + comparer.GetHashCode(RouteTemplate);
            hash = (hash * 31) + comparer.GetHashCode(MethodName);
            hash = (hash * 31) + comparer.GetHashCode(ReturnTypeMetadataName);
            hash = (hash * 31) + (HasFileUpload ? 1 : 0);
            hash = (hash * 31) + (ReturnsBinary ? 1 : 0);
            hash = (hash * 31) + (int)Risk;
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
}

/// <summary>
/// 能力条目的参数描述。
/// </summary>
internal sealed class CapabilityParameter : IEquatable<CapabilityParameter?>
{
    public CapabilityParameter(
        string name,
        string csharpType,
        string parameterKind, // Path / Query / Body / Header / FormContent
        string? docDescription,
        bool isRequired,
        bool isNullable)
    {
        Name = name;
        CsharpType = csharpType;
        ParameterKind = parameterKind;
        DocDescription = docDescription;
        IsRequired = isRequired;
        IsNullable = isNullable;
    }

    public string Name { get; }
    public string CsharpType { get; }
    public string ParameterKind { get; }
    public string? DocDescription { get; }
    public bool IsRequired { get; }
    public bool IsNullable { get; }

    public bool Equals(CapabilityParameter? other)
        => other is not null
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(CsharpType, other.CsharpType, StringComparison.Ordinal)
            && string.Equals(ParameterKind, other.ParameterKind, StringComparison.Ordinal)
            && string.Equals(DocDescription ?? string.Empty, other.DocDescription ?? string.Empty, StringComparison.Ordinal)
            && IsRequired == other.IsRequired
            && IsNullable == other.IsNullable;

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

    /// <summary>高风险写操作（命中危险词表或显式 override）。</summary>
    HighRiskWrite = 2,
}
