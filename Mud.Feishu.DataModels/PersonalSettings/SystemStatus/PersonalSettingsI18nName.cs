// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.PersonalSettings;

/// <summary>
/// 系统状态的国际化名称（中文名/英文名/日文名；同步设置的国际化名称与解释文案共用此结构）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "PersonalSettings")]
public class PersonalSettingsI18nName
{
    /// <summary>
    /// <para>中文名</para>
    /// <para>示例值：出差</para>
    /// </summary>
    [JsonPropertyName("zh_cn")]
    public string? ZhCn { get; set; }

    /// <summary>
    /// <para>英文名</para>
    /// <para>示例值：On business trip</para>
    /// </summary>
    [JsonPropertyName("en_us")]
    public string? EnUs { get; set; }

    /// <summary>
    /// <para>日文名</para>
    /// <para>示例值：出張中</para>
    /// </summary>
    [JsonPropertyName("ja_jp")]
    public string? JaJp { get; set; }
}
