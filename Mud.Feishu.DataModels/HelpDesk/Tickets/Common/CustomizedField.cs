// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// 工单自定义字段
/// </summary>
public class CustomizedField
{
    /// <summary>
    /// <para>工单自定义字段ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("ticket_customized_field_id")]
    public string? TicketCustomizedFieldId { get; set; }

    /// <summary>
    /// <para>服务台ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("helpdesk_id")]
    public string? HelpdeskId { get; set; }

    /// <summary>
    /// <para>键名</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("key_name")]
    public string? KeyName { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// <para>字段在列表后台管理列表中的位置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position")]
    public string? Position { get; set; }

    /// <summary>
    /// <para>类型</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_type")]
    public string? FieldType { get; set; }

    /// <summary>
    /// <para>描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>是否可见</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visible")]
    public bool? Visible { get; set; }

    /// <summary>
    /// <para>是否可以修改</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("editable")]
    public bool? Editable { get; set; }

    /// <summary>
    /// <para>是否必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    /// <summary>
    /// <para>创建时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>更新时间</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }

    /// <summary>
    /// <para>创建用户</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("created_by")]
    public TicketUser? CreatedBy { get; set; }

    /// <summary>
    /// <para>更新用户</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("updated_by")]
    public TicketUser? UpdatedBy { get; set; }

    /// <summary>
    /// <para>下拉列表选项</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dropdown_options")]
    public DropdownOptions? DropdownOptions { get; set; }


    /// <summary>
    /// <para>是否支持多选，仅在字段类型是dropdown的时候有效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("dropdown_allow_multiple")]
    public bool? DropdownAllowMultiple { get; set; }
}
