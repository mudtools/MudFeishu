// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 字段信息（职位模板子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CommonSchemaChild
{
    /// <summary>
    /// <para>字段 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>字段名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18n? Name { get; set; }

    /// <summary>
    /// <para>字段描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18n? Description { get; set; }

    /// <summary>
    /// <para>模块信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("setting")]
    public CommonSchemaSetting? Setting { get; set; }

    /// <summary>
    /// <para>所属模块 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("parent_id")]
    public string? ParentId { get; set; }

    /// <summary>
    /// <para>是否是自定义字段：true 自定义字段 / false 系统预置字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_customized")]
    public bool? IsCustomized { get; set; }

    /// <summary>
    /// <para>是否必填：true 必填 / false 非必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_required")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// <para>是否可见：true 可见 / false 不可见</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_visible")]
    public bool? IsVisible { get; set; }

    /// <summary>
    /// <para>是否启用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("active_status")]
    public int? ActiveStatus { get; set; }
}
