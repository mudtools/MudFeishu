// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 回传背调订单最终结果请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class UpdateResultEcoBackgroundCheckRequest
{
    /// <summary>
    /// <para>背调 ID，可通过「创建背调」事件获取</para>
    /// <para>必填：是</para>
    /// <para>示例值：6931286400470354183</para>
    /// </summary>
    [JsonPropertyName("background_check_id")]
    public string? BackgroundCheckId { get; set; }

    /// <summary>
    /// <para>背调结果</para>
    /// <para>必填：是</para>
    /// <para>示例值：No Diff</para>
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>
    /// <para>背调结果时间，毫秒时间戳</para>
    /// <para>必填：是</para>
    /// <para>示例值：1660123456789</para>
    /// </summary>
    [JsonPropertyName("result_time")]
    public string? ResultTime { get; set; }

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
