// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// <para>OKR Block</para>
/// </summary>
public class BlockOkr
{
    /// <summary>
    /// <para>OKR ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："7076349900476448796"</para>
    /// </summary>
    [JsonPropertyName("okr_id")]
    public string? OkrId { get; set; }

    /// <summary>
    /// <para>OKR Block 中的 objective ID 和 key result ID，此值为空时插入 okr 下所有的 objective 和 key result</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("objectives")]
    public ObjectiveIdWithKrId[]? Objectives { get; set; }

    /// <summary>
    /// <para>OKR Block 中的 objective ID 和 key result ID，此值为空时插入 okr 下所有的 objective 和 key result</para>
    /// </summary>
    public class ObjectiveIdWithKrId
    {
        /// <summary>
        /// <para>okr 中 objective 的 ID</para>
        /// <para>必填：否</para>
        /// <para>示例值：7109022409227026460</para>
        /// </summary>
        [JsonPropertyName("objective_id")]
        public string? ObjectiveId { get; set; }

        /// <summary>
        /// <para>key result 的 ID 列表，此值为空时插入当前 objective 下的所有 key result</para>
        /// <para>必填：否</para>
        /// <para>示例值：["7109022573011894300","7109022546444517404"]</para>
        /// </summary>
        [JsonPropertyName("kr_ids")]
        public string[]? KrIds { get; set; }
    }

    /// <summary>
    /// <para>周期的状态</para>
    /// <para>必填：否</para>
    /// <para>示例值："default"</para>
    /// <para>可选值：<list type="bullet">
    /// <item>default：默认</item>
    /// <item>normal：正常</item>
    /// <item>invalid：失效</item>
    /// <item>hidden：隐藏</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("period_display_status")]
    public string? PeriodDisplayStatus { get; set; }

    /// <summary>
    /// <para>周期名 - 中文</para>
    /// <para>必填：否</para>
    /// <para>示例值："2022 年 4 月 - 6 月"</para>
    /// </summary>
    [JsonPropertyName("period_name_zh")]
    public string? PeriodNameZh { get; set; }

    /// <summary>
    /// <para>周期名 - 英文</para>
    /// <para>必填：否</para>
    /// <para>示例值："Apr - Jun 2022"</para>
    /// </summary>
    [JsonPropertyName("period_name_en")]
    public string? PeriodNameEn { get; set; }

    /// <summary>
    /// <para>OKR 所属的用户 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值："ou_3bbe8a09c20e89cce9bff989ed840674"</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>可见性设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("visible_setting")]
    public OkrVisibleSetting? VisibleSetting { get; set; }

    /// <summary>
    /// <para>可见性设置</para>
    /// </summary>
    public class OkrVisibleSetting
    {
        /// <summary>
        /// <para>进展编辑区域是否可见</para>
        /// <para>必填：否</para>
        /// <para>示例值：true</para>
        /// <para>默认值：true</para>
        /// </summary>
        [JsonPropertyName("progress_fill_area_visible")]
        public bool? ProgressFillAreaVisible { get; set; }

        /// <summary>
        /// <para>状态是否可见</para>
        /// <para>必填：否</para>
        /// <para>示例值：true</para>
        /// <para>默认值：true</para>
        /// </summary>
        [JsonPropertyName("progress_status_visible")]
        public bool? ProgressStatusVisible { get; set; }

        /// <summary>
        /// <para>分数是否可见</para>
        /// <para>必填：否</para>
        /// <para>示例值：true</para>
        /// <para>默认值：true</para>
        /// </summary>
        [JsonPropertyName("score_visible")]
        public bool? ScoreVisible { get; set; }
    }
}
