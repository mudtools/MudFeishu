// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Spreadsheets;

/// <summary>
/// 批量创建条件格式响应体中的每个响应项
/// </summary>
public class CreateConditionFormatResponse
{
    /// <summary>
    /// <para>工作表的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sheet_id")]
    public string? SheetId { get; set; }

    /// <summary>
    /// <para>要创建的条件格式的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("cf_id")]
    public string? CfId { get; set; }

    /// <summary>
    /// <para>当前条件格式创建的状态码。0 表示成功创建，非 0 表示失败。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("res_code")]
    public int? ResCode { get; set; }

    /// <summary>
    /// <para>条件格式设置返回的状态信息，success 表示成功，非 success 将返回失败原因。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("res_msg")]
    public string? ResMsg { get; set; }
}