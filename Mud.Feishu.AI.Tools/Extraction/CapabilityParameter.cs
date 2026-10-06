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
        string? docDescription,
        bool isRequired,
        bool isNullable,
        string schemaFragmentJson,
        string? declaredToolParameterName = null,
        string? enumTypeFullName = null,
        string? enumMembers = null)
    {
        Name = name;
        CsharpType = csharpType;
        DocDescription = docDescription;
        IsRequired = isRequired;
        IsNullable = isNullable;
        SchemaFragmentJson = schemaFragmentJson;
        DeclaredToolParameterName = declaredToolParameterName;
        EnumTypeFullName = enumTypeFullName;
        EnumMembers = enumMembers;
    }

    /// <summary>模型可见参数名（snake_case 契约）。</summary>
    /// <remarks>即渲染产物的 properties 键——<see cref="DeclaredToolParameterName"/> 与本值的
    /// 漂移由 L4 校验器检出（MUDFT015 的真实意图源比对）。</remarks>
    public string Name { get; }

    /// <summary>
    /// <c>[ToolParameter]</c> 声明的参数名（模型可见契约的<b>意图源</b>；未声明时为
    /// <see langword="null"/>）。它与 <see cref="Name"/>（渲染产物实际使用的键）不一致即
    /// 声明与实现漂移——模型看到的键是后者，声明的名字只是文档。
    /// </summary>
    public string? DeclaredToolParameterName { get; }

    /// <summary>C# 类型显示名（仅用于诊断消息，不参与 Schema 推导）。</summary>
    public string CsharpType { get; }

    // R3-09：已删除 ParameterKind 属性——零消费方。

    /// <summary>参数文档描述（XML <c>&lt;param&gt;</c> 或 <c>[ToolParameter]</c>）。</summary>
    public string? DocDescription { get; }

    /// <summary>是否进入 Schema 的 <c>required</c>。</summary>
    public bool IsRequired { get; }

    /// <summary>是否可空。</summary>
    public bool IsNullable { get; }

    /// <summary>已推导的 JSON Schema 片段（如 <c>{"type":"array","items":{"type":"string"}}</c>）。</summary>
    public string SchemaFragmentJson { get; }

    /// <summary>
    /// 显式声明的取值闭集类型全名（R5 / F-2，来自 <c>[ToolParameter(EnumType = typeof(X))]</c>；
    /// 未声明时为 <see langword="null"/>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么进值键（<c>Equals</c>/<c>GetHashCode</c>）</b>：它决定
    /// <see cref="SchemaFragmentJson"/> 里的 <c>"enum":[…]</c>，属"影响产物的事实"。
    /// 漏掉它会让"增删闭集声明"不触发增量重算⇒ 描述符产物过期。
    /// 本仓已因同类问题漏字段 2 次（AT-B16），故由
    /// <c>CapabilityEqualityFieldCoverageTests</c> 机械锁定"每个构造参数都参与相等性"。
    /// </para>
    /// <para>
    /// <b>为什么用全名（string）而不是 <c>INamedTypeSymbol</c></c>：本类型是 Roslyn 增量管线的
    /// <b>值键</b>，其成员必须是可比较的不可变值；符号对象不满足该约束（且会把编译状态带进缓存键）。
    /// </para>
    /// </remarks>
    public string? EnumTypeFullName { get; }

    /// <summary>
    /// 闭集成员表（R5 / F-2，形如 <c>"page=1;text=2;heading1=3"</c>；<b>仅常量类闭集</b>有值）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么用字符串而不是 <c>List&lt;(string, long)&gt;</c></b>：本类型是 Roslyn 增量管线的
    /// <b>值键</b>，成员必须可比较、不含编译状态，且跨 <c>netstandard2.0</c> 的
    /// <c>ValueTuple</c> 命名开销也要避免。字符串 CSV 是最省事且足够的选择——
    /// 它的唯一消费者是 <c>ToolArgsEmitter</c> 的映射表渲染。
    /// </para>
    /// <para>
    /// <b>为什么必须进值键</b>：常量类闭集<b>增删成员</b>会改变映射表（进而改变 Args 产物），
    /// 却<b>不会</b>改变 <see cref="SchemaFragmentJson"/>（那里只渲染名字，且名字集合可能不变
    /// ——例如值变了但名字没变）。漏掉本字段 ⇒ 改常量值不触发增量重算 ⇒ 产物过期。
    /// </para>
    /// </remarks>
    public string? EnumMembers { get; }

    public bool Equals(CapabilityParameter? other)
        => other is not null
            && string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(DeclaredToolParameterName ?? string.Empty, other.DeclaredToolParameterName ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(CsharpType, other.CsharpType, StringComparison.Ordinal)
            && string.Equals(DocDescription ?? string.Empty, other.DocDescription ?? string.Empty, StringComparison.Ordinal)
            && IsRequired == other.IsRequired
            && IsNullable == other.IsNullable
            && string.Equals(EnumTypeFullName ?? string.Empty, other.EnumTypeFullName ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(EnumMembers ?? string.Empty, other.EnumMembers ?? string.Empty, StringComparison.Ordinal)
            && string.Equals(SchemaFragmentJson, other.SchemaFragmentJson, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as CapabilityParameter);

    /// <summary>哈希覆盖 <see cref="Equals(CapabilityParameter?)"/> 的全部字段（AT-B16：原实现漏了文档描述）。</summary>
    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + comparer.GetHashCode(Name);
            hash = (hash * 31) + comparer.GetHashCode(DeclaredToolParameterName ?? string.Empty);
            hash = (hash * 31) + comparer.GetHashCode(CsharpType);
            hash = (hash * 31) + comparer.GetHashCode(DocDescription ?? string.Empty);
            hash = (hash * 31) + comparer.GetHashCode(EnumTypeFullName ?? string.Empty);
            hash = (hash * 31) + comparer.GetHashCode(EnumMembers ?? string.Empty);
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
