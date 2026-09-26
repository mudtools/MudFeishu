// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 指标字段基础信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceMetricField
{
    /// <summary>
    /// <para>指标字段 ID</para>
    /// <para>示例值：7272581996315099155</para>
    /// </summary>
    [JsonPropertyName("field_id")]
    public string? FieldId { get; set; }

    /// <summary>
    /// <para>指标字段名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>指标字段类型：text（文本）/ number（数字）/ pencentage（百分比，飞书原始枚举值拼写如此）/ person（人员单选）</para>
    /// <para>示例值：text</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
