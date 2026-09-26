// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 背调报告信息
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class BackgroundCheckOrderFeedbackInfo
{
    /// <summary>
    /// <para>背调报告 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>报告附件链接（有效期 1 小时）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("attachment_url")]
    public string? AttachmentUrl { get; set; }

    /// <summary>
    /// <para>报告预览链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("report_preview_url")]
    public string? ReportPreviewUrl { get; set; }

    /// <summary>
    /// <para>背调结果：红灯 / 黄灯 / 蓝灯 / 绿灯</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>
    /// <para>报告生成时间（毫秒时间戳字符串）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>报告名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("report_name")]
    public string? ReportName { get; set; }

    /// <summary>
    /// <para>报告类型：1 阶段性报告 / 2 终版报告</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("report_type")]
    public int? ReportType { get; set; }
}
