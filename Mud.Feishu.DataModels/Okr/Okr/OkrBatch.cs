// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Okr;

/// <summary>
/// OKR（一个周期下的一组目标）
/// </summary>
public class OkrBatch
{
    /// <summary>
    /// <para>OKR id</para>
    /// <para>示例值：7072252816005349396</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>OKR 访问权限：0 无权限、1 有权限</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("permission")]
    public int? Permission { get; set; }

    /// <summary>
    /// <para>OKR 周期 id</para>
    /// <para>示例值：7067724095781142548</para>
    /// </summary>
    [JsonPropertyName("period_id")]
    public string? PeriodId { get; set; }

    /// <summary>
    /// <para>OKR 名称</para>
    /// <para>示例值：2022 年 3 月</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>目标（Objective）列表</para>
    /// </summary>
    [JsonPropertyName("objective_list")]
    public OkrObjective[]? ObjectiveList { get; set; }

    /// <summary>
    /// <para>OKR 确认状态：0 初始状态、1 待提交、2 待确认、3 已驳回、4 已通过</para>
    /// <para>示例值：4</para>
    /// </summary>
    [JsonPropertyName("confirm_status")]
    public int? ConfirmStatus { get; set; }
}
