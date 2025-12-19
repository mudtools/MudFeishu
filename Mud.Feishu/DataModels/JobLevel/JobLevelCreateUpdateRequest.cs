// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.JobLevel;

/// <summary>
/// 创建职级请求体。
/// </summary>
public class JobLevelCreateUpdateRequest
{
    /// <summary>
    /// 职级名称。通用名称，如果未设置多语言名称，则默认展示该名称。
    /// <para>示例值："高级专家"</para>
    /// </summary>
    [JsonPropertyName("name")]
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? Name
    { get; set; }

    /// <summary>
    /// 职级描述。字符长度上限 5,000。通用描述，如果未设置多语言描述，则默认展示该描述。
    /// <para>示例值："公司内部中高级职称，有一定专业技术能力的人员"</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 职级排序。数值越小，排序越靠前。
    /// <para>示例值：200</para>
    /// </summary>
    [JsonPropertyName("order")]
    public int? Order { get; set; }

    /// <summary>
    /// 是否启用该职级。
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; } = true;

    /// <summary>
    /// 多语言职级名称。
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; } = [];

    /// <summary>
    /// 多语言职级描述。
    /// </summary>
    [JsonPropertyName("i18n_description")]
    public List<I18nContent> I18nDescription { get; set; } = [];
}
