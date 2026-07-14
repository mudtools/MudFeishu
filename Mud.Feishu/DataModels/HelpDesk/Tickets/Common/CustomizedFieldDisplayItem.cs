// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.HelpDesk;


/// <summary>
/// <para>自定义字段列表，没有值时不设置</para>
/// <para>下拉菜单的value对应工单字段里面的children.display_name</para>
/// <para>[获取全部工单自定义字段](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/helpdesk-v1/ticket_customized_field/list-ticket-customized-fields)</para>
/// </summary>
public class CustomizedFieldDisplayItem
{
    /// <summary>
    /// <para>自定义字段ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：123</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>自定义字段值</para>
    /// <para>必填：否</para>
    /// <para>示例值：value</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>键名</para>
    /// <para>必填：否</para>
    /// <para>示例值：key</para>
    /// </summary>
    [JsonPropertyName("key_name")]
    public string? KeyName { get; set; }

    /// <summary>
    /// <para>展示名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：display name</para>
    /// </summary>
    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    /// <summary>
    /// <para>展示位置</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("position")]
    public int? Position { get; set; }

    /// <summary>
    /// <para>是否必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("required")]
    public bool? Required { get; set; }

    /// <summary>
    /// <para>是否可修改</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("editable")]
    public bool? Editable { get; set; }
}