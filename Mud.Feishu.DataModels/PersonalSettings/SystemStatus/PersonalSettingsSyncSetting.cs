// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.PersonalSettings;

/// <summary>
/// 系统状态的同步设置（system_status_sync_setting）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "PersonalSettings")]
public class PersonalSettingsSyncSetting
{
    /// <summary>
    /// <para>是否默认开启，默认 true</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// </summary>
    [JsonPropertyName("is_open_by_default")]
    public bool? IsOpenByDefault { get; set; }

    /// <summary>
    /// <para>同步设置名称，名称字符数要在 1 到 30 范围内（1中文=2英文=2其他语言字符=2字符），默认「自动开启」</para>
    /// <para>必填：否</para>
    /// <para>示例值：出差期间自动开启</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>同步设置国际化名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_title")]
    public PersonalSettingsI18nName? I18nTitle { get; set; }

    /// <summary>
    /// <para>同步设置解释文案，解释字符数要在 1 到 60 范围内，默认「从相关系统进行信息同步，同步后将自动开启并优先展示该状态。」</para>
    /// <para>必填：否</para>
    /// <para>示例值：出差审批通过后，将自动开启并优先展示该状态。</para>
    /// </summary>
    [JsonPropertyName("explain")]
    public string? Explain { get; set; }

    /// <summary>
    /// <para>同步设置国际化解释文案</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_explain")]
    public PersonalSettingsI18nName? I18nExplain { get; set; }
}
