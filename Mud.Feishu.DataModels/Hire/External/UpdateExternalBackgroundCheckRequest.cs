// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 更新外部背调请求体（覆盖更新）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class UpdateExternalBackgroundCheckRequest
{
    /// <summary>
    /// <para>外部投递 ID</para>
    /// <para>必填：是</para>
    /// <para>示例值：7003247299220982060</para>
    /// </summary>
    [JsonPropertyName("external_application_id")]
    public string? ExternalApplicationId { get; set; }

    /// <summary>
    /// <para>背调日期，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1626602069393</para>
    /// </summary>
    [JsonPropertyName("date")]
    public long? Date { get; set; }

    /// <summary>
    /// <para>背调名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：Test.pdf</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>背调结果</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("result")]
    public string? Result { get; set; }

    /// <summary>
    /// <para>背调附件 ID 列表，可通过创建附件接口返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7003247299220982060"]</para>
    /// </summary>
    [JsonPropertyName("attachment_id_list")]
    public string[]? AttachmentIdList { get; set; }
}
