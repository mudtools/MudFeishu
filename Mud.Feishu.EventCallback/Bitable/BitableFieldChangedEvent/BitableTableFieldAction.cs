// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Bitable;

/// <summary>字段变更操作类型列表</summary>
public class BitableTableFieldAction
{
    /// <summary>
    /// <para>字段变更类型。枚举值有：</para>
    /// <para>- field_added：新增字段</para>
    /// <para>- field_edited：修改字段</para>
    /// <para>- field_deleted：删除字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// <para>字段 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    /// <summary>
    /// <para>操作前的字段值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("before_value")]
    public BitableTableFieldActionValue? BeforeValue { get; set; }

    /// <summary>
    /// <para>操作后的字段值</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("after_value")]
    public BitableTableFieldActionValue? AfterValue { get; set; }

}