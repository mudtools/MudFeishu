// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.PersonalSettings;

/// <summary>
/// 创建系统状态请求体（每个租户最多创建 10 个系统状态）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "PersonalSettings")]
public class CreateSystemStatusRequest
{
    /// <summary>
    /// <para>系统状态名称（必填），名称字符数要在 1 到 20 范围内，不同系统状态的 title 不能重复</para>
    /// <para>必填：是</para>
    /// <para>示例值：出差</para>
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// <para>系统状态国际化名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_title")]
    public PersonalSettingsI18nName? I18nTitle { get; set; }

    /// <summary>
    /// <para>图标（必填），可选值见飞书 icon_key 文档</para>
    /// <para>必填：是</para>
    /// <para>示例值：GeneralBusinessTrip</para>
    /// </summary>
    [JsonPropertyName("icon_key")]
    public string? IconKey { get; set; }

    /// <summary>
    /// <para>颜色，默认 BLUE</para>
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
