// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.MDM;

/// <summary>
/// 主数据多语言文本（i18n_string）：value 为入参 languages 中排序第一的语言对应的值，multilingual_value 为所有语言的值
/// </summary>
[HttpJsonSerializable(SerializerClassName = "MDM")]
public class MdmI18nString
{
    /// <summary>
    /// <para>入参 languages 中排序第一的语言对应的值</para>
    /// <para>示例值：安道尔</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// <para>入参 languages 中所有语言对应的值（键为语言代码，如 zh-CN / en-US / ja-JP）</para>
    /// </summary>
    [JsonPropertyName("multilingual_value")]
    public Dictionary<string, string>? MultilingualValue { get; set; }

    /// <summary>
    /// <para>value 实际返回的值对应的语言，如 "zh-CN"</para>
    /// </summary>
    [JsonPropertyName("return_language")]
    public string? ReturnLanguage { get; set; }
}
