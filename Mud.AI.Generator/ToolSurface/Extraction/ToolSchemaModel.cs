// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.ToolSurface.Extraction;

/// <summary>
/// Tier C（手写工具特性接口）的编译期模型：<see cref="CapabilityEntry"/> + 发射所需的命名信息。
/// </summary>
/// <remarks>
/// <para>
/// 全部字段均为<b>值类型/字符串</b>（不含 <c>ISymbol</c>）——这是增量管线的关键：
/// 符号持有编译引用，一旦进入模型会让增量缓存恒失效。
/// </para>
/// <para>
/// 值相等供增量管线判定"这次编辑是否需要重发源码"。
/// </para>
/// </remarks>
internal sealed class ToolSchemaModel : IEquatable<ToolSchemaModel?>
{
    public ToolSchemaModel(
        CapabilityEntry entry,
        string constName,
        string description,
        string? source)
    {
        Entry = entry;
        ConstName = constName;
        Description = description;
        Source = source;
    }

    /// <summary>能力条目（方法级结构化事实，含已推导的参数 Schema 片段）。</summary>
    /// <remarks>
    /// 读写分类的<b>唯一</b>事实源是 <see cref="CapabilityEntry.Risk"/>（已含特性声明与 SDK 源推导的
    /// 升级结果）——本类型不再另设 <c>IsWrite</c> 字段，避免"声明标志 vs 风险派生"两处口径
    /// 在 <c>{P}Names</c> 与 <c>{P}Contracts</c> 之间漂移（设计文档 §2 的"单一真相源"纪律）。
    /// </remarks>
    public CapabilityEntry Entry { get; }

    /// <summary>生成的 C# 常量名（如 <c>bitable_list_tablesSchemaJson</c>）。</summary>
    public string ConstName { get; }

    /// <summary>工具描述（模型侧，来自工具特性的 <c>Description=...</c>）。</summary>
    public string Description { get; }

    /// <summary>声明的 SDK 能力来源（可空；非空时已通过交叉校验）。</summary>
    public string? Source { get; }

    public bool Equals(ToolSchemaModel? other)
        => other is not null
            && Entry.Equals(other.Entry)
            && string.Equals(ConstName, other.ConstName, StringComparison.Ordinal)
            && string.Equals(Description, other.Description, StringComparison.Ordinal)
            && string.Equals(Source ?? string.Empty, other.Source ?? string.Empty, StringComparison.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as ToolSchemaModel);

    /// <summary>哈希覆盖 <see cref="Equals(ToolSchemaModel?)"/> 的全部字段（AT-B16：原实现漏了 <see cref="Source"/>）。</summary>
    public override int GetHashCode()
    {
        var comparer = StringComparer.Ordinal;
        unchecked
        {
            var hash = 17;
            hash = (hash * 31) + Entry.GetHashCode();
            hash = (hash * 31) + comparer.GetHashCode(ConstName);
            hash = (hash * 31) + comparer.GetHashCode(Description);
            hash = (hash * 31) + comparer.GetHashCode(Source ?? string.Empty);
            return hash;
        }
    }
}