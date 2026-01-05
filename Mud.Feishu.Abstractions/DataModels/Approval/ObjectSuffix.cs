// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.Approval;


/// <summary>
/// 事件详细数据
/// </summary>
public class ObjectSuffix
{
    /// <summary>
    /// <para>审批定义 Code</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_code")]
    public string? ApprovalCode { get; set; }

    /// <summary>
    /// <para>审批定义 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("approval_id")]
    public string? ApprovalId { get; set; }

    /// <summary>
    /// <para>扩展字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// <para>表单定义 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("form_definition_id")]
    public string? FormDefinitionId { get; set; }

    /// <summary>
    /// <para>审批流程表单 JSON 数据</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_obj")]
    public string? ProcessObj { get; set; }

    /// <summary>
    /// <para>审批定义更新时间，秒级时间戳</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("timestamp")]
    public int? Timestamp { get; set; }

    /// <summary>
    /// <para>审批定义的版本号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("version_id")]
    public string? VersionId { get; set; }

    /// <summary>
    /// <para>控件组类型，返回 0 表示未使用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("widget_group_type")]
    public int? WidgetGroupType { get; set; }
}
