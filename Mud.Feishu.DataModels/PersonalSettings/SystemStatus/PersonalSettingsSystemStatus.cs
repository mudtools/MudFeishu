// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.PersonalSettings;

/// <summary>
/// 租户维度的系统状态（system_status）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "PersonalSettings")]
public class PersonalSettingsSystemStatus
{
    /// <summary>
    /// <para>系统状态 ID</para>
    /// <para>示例值：7101214603622940633</para>
    /// </summary>
    [JsonPropertyName("system_status_id")]
    public string? SystemStatusId { get; set; }

    /// <summary>
    /// <para>系统状态名称，名称字符数要在 1 到 20 范围内，不同系统状态的 title 不能重复（1中文=2英文=2其他语言字符=2字符）</para>
    /// <para>必填：是</para>
    /// <para>示例值：出差</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>系统状态国际化名称，不同系统状态之间任何一种语言的 title 都不能重复</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_title")]
    public PersonalSettingsI18nName? I18nTitle { get; set; }

    /// <summary>
    /// <para>图标（必填），可选值如 GeneralDoNotDisturb / GeneralInMeetingBusy / Coffee / GeneralBusinessTrip / GeneralWorkFromHome / StatusEnjoyLife / GeneralTravellingCar / StatusBus / StatusInFlight / Typing / EatingFood / SICK / GeneralSun / GeneralMoonRest / StatusReading / Status_PrivateMessage / StatusFlashOfInspiration / GeneralVacation</para>
    /// <para>必填：是</para>
    /// <para>示例值：GeneralBusinessTrip</para>
    /// </summary>
    [JsonPropertyName("icon_key")]
    public string? IconKey { get; set; }

    /// <summary>
    /// <para>颜色，默认 BLUE：BLUE（蓝色）/ GRAY（灰色）/ INDIGO（靛青色）/ WATHET（浅蓝色）/ GREEN（绿色）/ TURQUOISE（绿松石色）/ YELLOW（黄色）/ LIME（酸橙色）/ RED（红色）/ ORANGE（橙色）/ PURPLE（紫色）/ VIOLET（紫罗兰色）/ CARMINE（胭脂红色）</para>
    /// <para>必填：否</para>
    /// <para>示例值：BLUE</para>
    /// </summary>
    [JsonPropertyName("color")]
    public string? Color { get; set; }

    /// <summary>
    /// <para>优先级，数值越小客户端展示优先级越高，不同系统状态的优先级不能一样，默认 0，取值范围 0~9</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("priority")]
    public int? Priority { get; set; }

    /// <summary>
    /// <para>同步设置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sync_setting")]
    public PersonalSettingsSyncSetting? SyncSetting { get; set; }
}
