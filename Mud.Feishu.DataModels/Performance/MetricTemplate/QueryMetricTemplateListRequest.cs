// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 获取指标模板列表请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class QueryMetricTemplateListRequest
{
    /// <summary>
    /// <para>指标模板 ID 列表，填写时获取指定的指标模版（注意：飞书原始字段拼写为 metrics_template_ids）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7360956875099078676"]</para>
    /// </summary>
    [JsonPropertyName("metrics_template_ids")]
    public string[]? MetricsTemplateIds { get; set; }

    /// <summary>
    /// <para>指标模版状态：to_be_configured（待完成配置）/ to_be_activated（待启用）/ enabled（已启用）/ disabled（已停用）</para>
    /// <para>必填：否</para>
    /// <para>示例值：to_be_configured</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }
}
