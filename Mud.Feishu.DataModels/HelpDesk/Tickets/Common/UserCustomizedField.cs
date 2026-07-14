// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;

/// <summary>
/// <para>用户自定义字段</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "HelpDesk")]
public class UserCustomizedField
{
    /// <summary>
    /// <para>字段ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6746384425543548981</para>
    /// </summary>
    [JsonPropertyName("user_customized_field_id")]
    public string? UserCustomizedFieldId { get; set; }

    /// <summary>
    /// <para>旧字段ID，向后兼容用</para>
    /// <para>必填：否</para>
    /// <para>示例值：6746384425543548981</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>服务台ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：1542164574896126</para>
    /// </summary>
    [JsonPropertyName("helpdesk_id")]
    public string? HelpdeskId { get; set; }

    /// <summary>
    /// <para>字段键</para>
    /// <para>必填：否</para>
    /// <para>示例值：company_id3</para>
    /// </summary>
    [JsonPropertyName("key_name")]
    public string? KeyName { get; set; }

    /// <summary>
    /// <para>字段展示名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：CompanyID</para>
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// <para>字段在列表中的展示位置</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("position")]
    public string? Position { get; set; }

    /// <summary>
    /// <para>字段类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：string</para>
    /// </summary>
    [JsonPropertyName("field_type")]
    public string? FieldType { get; set; }

    /// <summary>
    /// <para>字段描述信息</para>
    /// <para>必填：否</para>
    /// <para>示例值：租户ID</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>字段是否可见</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("visible")]
    public bool? Visible { get; set; }

    /// <summary>
    /// <para>字段是否可编辑</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("editable")]
    public bool? Editable { get; set; }

    /// <summary>
    /// <para>字段是否必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    /// <summary>
    /// <para>字段创建时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1574040677000</para>
    /// </summary>
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    /// <summary>
    /// <para>字段修改时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1574040677000</para>
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string? UpdatedAt { get; set; }
}
