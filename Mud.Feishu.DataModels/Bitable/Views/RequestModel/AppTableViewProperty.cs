// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>视图属性</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppTableViewProperty
{
    /// <summary>
    /// <para>筛选条件</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("filter_info")]
    public AppTableViewPropertyFilterInfo? FilterInfo { get; set; }

    /// <summary>
    /// <para>隐藏字段 ID 列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：["fldCGzANXx", "fldCGzANXx"]</para>
    /// <para>最大长度：300</para>
    /// </summary>
    [JsonPropertyName("hidden_fields")]
    public string[]? HiddenFields { get; set; }

    /// <summary>
    /// <para>表格视图层级结构设置</para>
    /// <para>必填：否</para>
    /// </summary>
    /// <remarks>
    /// 层级结构设置复用同命名空间的 <see cref="Bitable.AppTableViewPropertyHierarchyConfig"/>：
    /// 同一 <c>Bitable</c> JsonSerializerContext 内不允许存在同名类型，否则源生成器（SYSLIB1031）
    /// 只会为其中一个生成元数据。
    /// </remarks>
    [JsonPropertyName("hierarchy_config")]
    public AppTableViewPropertyHierarchyConfig? HierarchyConfig { get; set; }
}
