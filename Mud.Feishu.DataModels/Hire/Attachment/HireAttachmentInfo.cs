// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 招聘系统附件信息（获取附件信息接口返回的 attachment 对象）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireAttachmentInfo
{
    /// <summary>
    /// <para>附件 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：6949805467799537964</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>附件下载地址，有效期为 30 分钟</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://hire.feishu.cn/blob/xx/</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>附件文件名</para>
    /// <para>必填：否</para>
    /// <para>示例值：xx的简历.prd</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>附件媒体类型/MIME</para>
    /// <para>必填：否</para>
    /// <para>示例值：application/pdf</para>
    /// </summary>
    [JsonPropertyName("mime")]
    public string? Mime { get; set; }

    /// <summary>
    /// <para>附件创建时间，毫秒时间戳（int64 类型）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618899376480</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public long? CreateTime { get; set; }
}
