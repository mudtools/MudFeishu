// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 官网投递工作经历（简历信息子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class WebsiteDeliveryCareer
{
    /// <summary>
    /// <para>工作经历描述</para>
    /// <para>必填：否</para>
    /// <para>示例值：这是一家很不错的创业公司</para>
    /// </summary>
    [JsonPropertyName("desc")]
    public string? Desc { get; set; }

    /// <summary>
    /// <para>结束时间，毫秒时间戳；「至今」传 -1</para>
    /// <para>必填：否</para>
    /// <para>示例值：1618500278667</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public long? EndTime { get; set; }

    /// <summary>
    /// <para>开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609430400</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public long? StartTime { get; set; }

    /// <summary>
    /// <para>职位名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：后端研发实习生</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>公司</para>
    /// <para>必填：否</para>
    /// <para>示例值：字节跳动</para>
    /// </summary>
    [JsonPropertyName("company")]
    public string? Company { get; set; }

    /// <summary>
    /// <para>自定义字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("customized_data")]
    public WebsiteDeliveryCustomizedData[]? CustomizedData { get; set; }
}
