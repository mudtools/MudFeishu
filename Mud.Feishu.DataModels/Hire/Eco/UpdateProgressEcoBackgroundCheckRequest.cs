// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 更新背调订单进度请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class UpdateProgressEcoBackgroundCheckRequest
{
    /// <summary>
    /// <para>背调 ID，可通过「创建背调」事件获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：6931286400470354183</para>
    /// </summary>
    [JsonPropertyName("background_check_id")]
    public string? BackgroundCheckId { get; set; }

    /// <summary>
    /// <para>阶段 ID；同一背调订单此 ID 不能重复，由调用方自定义</para>
    /// <para>必填：是</para>
    /// <para>示例值：6931286400470354183</para>
    /// </summary>
    [JsonPropertyName("stage_id")]
    public string? StageId { get; set; }

    /// <summary>
    /// <para>背调阶段英文名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：stage report</para>
    /// </summary>
    [JsonPropertyName("stage_en_name")]
    public string? StageEnName { get; set; }

    /// <summary>
    /// <para>背调阶段名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：阶段报告</para>
    /// </summary>
    [JsonPropertyName("stage_name")]
    public string? StageName { get; set; }

    /// <summary>
    /// <para>阶段进度更新时间，毫秒时间戳；每次调用此字段应严格递增</para>
    /// <para>必填：是</para>
    /// <para>示例值：1660123456789</para>
    /// </summary>
    [JsonPropertyName("stage_time")]
    public string? StageTime { get; set; }

    /// <summary>
    /// <para>背调结果（阶段性背调结果）；注意：若需回传该字段，report_file_list 为必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：通过</para>
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>
    /// <para>操作人角色，默认值为 1</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("operator_role")]
    public int? OperatorRole { get; set; }

    /// <summary>
    /// <para>报告列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("report_file_list")]
    public EcoBackgroundCheckReportFile[]? ReportFileList { get; set; }
}
