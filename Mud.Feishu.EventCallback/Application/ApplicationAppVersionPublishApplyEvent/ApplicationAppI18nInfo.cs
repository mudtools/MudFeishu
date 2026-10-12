// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// ApplicationAppI18nInfo
/// <para>应用的国际化信息列表</para>
/// <para>**数据校验规则**：</para>
/// <para>- 最小长度：`1`</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationAppI18nInfo
{
    /// <summary>
    /// <para>国际化语言的 key</para>
    /// <para>**可选值有**：</para>
    /// <para>- zh_cn：简体中文</para>
    /// <para>- en_us：英文</para>
    /// <para>- ja_jp：日文</para>
    /// <para>- zh_hk：繁体中文(中国香港)</para>
    /// <para>- zh_tw：繁体中文(中国台湾)</para>
    /// <para>- id_id：印度尼西亚语</para>
    /// <para>- ms_my：马来语</para>
    /// <para>- de_de：德语</para>
    /// <para>- es_es：西班牙语</para>
    /// <para>- fr_fr：法语</para>
    /// <para>- it_it：意大利语</para>
    /// <para>- pt_br：葡萄牙语(巴西)</para>
    /// <para>- vi_vn：越南语</para>
    /// <para>- ru_ru：俄语</para>
    /// <para>- th_th：泰语</para>
    /// <para>- ko_kr：韩语</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("i18n_key")]
    public string? I18nKey { get; set; }

    /// <summary>
    /// <para>应用国际化名称</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>应用国际化描述（副标题）</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 最小长度：`1` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// <para>国际化帮助文档链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("help_use")]
    public string? HelpUse { get; set; }
}
