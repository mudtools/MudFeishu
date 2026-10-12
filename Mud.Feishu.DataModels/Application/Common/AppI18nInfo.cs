// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Application;

/// <summary>
/// 应用多语言名称与描述信息。
/// <para>i18n_key 可选值：zh_cn、en_us、ja_jp、zh_hk、zh_tw、id_id、ms_my、de_de、es_es、fr_fr、it_it、pt_br、vi_vn、ru_ru、th_th、ko_kr</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class AppI18nInfo
{
    /// <summary>
    /// <para>语言标签，如 zh_cn、en_us、ja_jp</para>
    /// <para>必填：是</para>
    /// <para>示例值：zh_cn</para>
    /// </summary>
    [JsonPropertyName("i18n_key")]
    public string? I18nKey { get; set; }

    /// <summary>
    /// <para>应用名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>应用描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>应用使用场景描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("help_use")]
    public string? HelpUse { get; set; }
}
