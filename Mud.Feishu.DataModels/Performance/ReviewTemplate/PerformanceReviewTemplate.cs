// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 绩效模板信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class PerformanceReviewTemplate
{
    /// <summary>
    /// <para>绩效模板 ID</para>
    /// <para>示例值：7343513161666723843</para>
    /// </summary>
    [JsonPropertyName("review_template_id")]
    public string? ReviewTemplateId { get; set; }

    /// <summary>
    /// <para>绩效模板名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>绩效模板描述</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18nName? Description { get; set; }

    /// <summary>
    /// <para>状态：to_be_configured（待完成配置）/ to_be_enabled（待启用）/ enabled（已启用）/ disabled（已停用）/ deleted（已删除但曾经被项目引用过）</para>
    /// <para>示例值：enabled</para>
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// <para>环节模板列表</para>
    /// </summary>
    [JsonPropertyName("templates")]
    public PerformanceStageTemplate[]? Templates { get; set; }

    /// <summary>
    /// <para>评估内容列表</para>
    /// </summary>
    [JsonPropertyName("units")]
    public PerformanceReviewUnit[]? Units { get; set; }
}
