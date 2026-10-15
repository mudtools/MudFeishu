// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// CoreHrV2OrganizationDomainEventData
/// <para>子变更事件</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2OrganizationDomainEventData
{
    /// <summary>
    /// <para>变更实体的ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>变更实体标识，ObjAPIName</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("entity")]
    public string? Entity { get; set; }

    /// <summary>
    /// <para>聚合实体标识，父对象 ObjAPIName</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agg_entity")]
    public string? AggEntity { get; set; }

    /// <summary>
    /// <para>聚合实体ID, 父对象ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agg_entity_id")]
    public string? AggEntityId { get; set; }

    /// <summary>
    /// <para>所属聚合实体字段， FieldAPIName</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agg_entity_field")]
    public string? AggEntityField { get; set; }

    /// <summary>
    /// <para>操作类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：新增</para>
    /// <para>- 2：修改</para>
    /// <para>- 3：删除</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("opt_type")]
    public int? OptType { get; set; }

    /// <summary>
    /// <para>变更字段列表</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `1000`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_changes")]
    public string[]? FieldChanges { get; set; }
}
