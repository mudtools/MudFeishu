// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.JobFamilies;

/// <summary>
/// 职位序列创建请求体。
/// </summary>
public class JobFamilyCreateUpdateRequest
{
    /// <summary>
    /// 序列名称，租户内唯一。取值支持中、英文及符号。
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// 序列描述，描述序列详情信息。字符长度上限为 5,000。
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 上级序列 ID。如果需要为某一序列添加子序列，则需要传入该参数值。
    /// </summary>
    [JsonPropertyName("parent_job_family_id")]
    public string? ParentJobFamilyId { get; set; }

    /// <summary>
    /// 多语言序列描述。
    /// </summary>
    [JsonPropertyName("i18n_description")]
    public List<I18nContent> I18nDescription { get; set; } = [];

    /// <summary>
    /// 多语言序列名称。
    /// </summary>
    [JsonPropertyName("i18n_name")]
    public List<I18nContent> I18nName { get; set; } = [];

    /// <summary>
    /// 是否启用序列。
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; } = true;
}
