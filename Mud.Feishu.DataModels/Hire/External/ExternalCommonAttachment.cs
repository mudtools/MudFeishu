// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 外部通用附件（外部 Offer 详情附件）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class ExternalCommonAttachment
{
    /// <summary>
    /// <para>附件 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6987954043925432620</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>附件名字</para>
    /// <para>必填：否</para>
    /// <para>示例值：test_resume.pdf</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>附件大小</para>
    /// <para>必填：否</para>
    /// <para>示例值：126371</para>
    /// </summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }
}
