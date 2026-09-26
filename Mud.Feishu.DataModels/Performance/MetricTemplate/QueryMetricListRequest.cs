// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Performance;

/// <summary>
/// 获取指标列表请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Performance")]
public class QueryMetricListRequest
{
    /// <summary>
    /// <para>指标启用状态，填写时根据指定启用状态进行筛选</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_active")]
    public bool? IsActive { get; set; }

    /// <summary>
    /// <para>指标标签 ID 列表，可通过获取指标标签信息接口获取，填写时筛选拥有指定标签的指标（0~99 个）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7302271694582841364"]</para>
    /// </summary>
    [JsonPropertyName("tag_ids")]
    public string[]? TagIds { get; set; }

    /// <summary>
    /// <para>指标类型 ID 列表，可通过获取指标模板列表接口返回结果中的 data.items.metrics.type_id 获取，填写时根据指定的指标类型进行筛选（0~99 个）</para>
    /// <para>必填：否</para>
    /// <para>示例值：["7272578300650717203"]</para>
    /// </summary>
    [JsonPropertyName("type_ids")]
    public string[]? TypeIds { get; set; }

    /// <summary>
    /// <para>指标可用范围，填写时根据指定可用范围进行筛选：admins_and_reviewees（允许管理员下发和被评估人选用）/ only_admins（仅允许管理员下发）</para>
    /// <para>必填：否</para>
    /// <para>示例值：admins_and_reviewees</para>
    /// </summary>
    [JsonPropertyName("range_of_availability")]
    public string? RangeOfAvailability { get; set; }

    /// <summary>
    /// <para>指标评分类型，填写时根据指定评分类型进行筛选：score_manually（手动评分）/ score_by_formula（公式评分）</para>
    /// <para>必填：否</para>
    /// <para>示例值：score_manually</para>
    /// </summary>
    [JsonPropertyName("scoring_setting_type")]
    public string? ScoringSettingType { get; set; }
}
