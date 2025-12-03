// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels;

/// <summary>
/// 国际化名称类，用于存储多种语言的名称表示
/// </summary>
public class I18nName
{
    /// <summary>
    /// 中文名称 (简体中文)
    /// </summary>
    [JsonPropertyName("zh_cn")]
    public string? ZhCn { get; set; }

    /// <summary>
    /// 日文名称
    /// </summary>
    [JsonPropertyName("ja_jp")]
    public string? JaJp { get; set; }

    /// <summary>
    /// 英文名称 (美式英语)
    /// </summary>
    [JsonPropertyName("en_us")]
    public string? EnUs { get; set; }
}



/// <summary>
/// <para>任务导入来源的名称，用于在任务中心详情页展示。需提供多语言版本。</para>
/// </summary>
public class I18nText : I18nName
{
    /// <summary>
    /// <para>中文（香港地区）</para>
    /// </summary>
    [JsonPropertyName("zh_hk")]
    public string? ZhHk { get; set; }

    /// <summary>
    /// <para>中文（台湾地区）</para>
    /// </summary>
    [JsonPropertyName("zh_tw")]
    public string? ZhTw { get; set; }

    /// <summary>
    /// <para>法语</para>
    /// </summary>
    [JsonPropertyName("fr_fr")]
    public string? FrFr { get; set; }

    /// <summary>
    /// <para>意大利语</para>
    /// </summary>
    [JsonPropertyName("it_it")]
    public string? ItIt { get; set; }

    /// <summary>
    /// <para>德语</para>
    /// </summary>
    [JsonPropertyName("de_de")]
    public string? DeDe { get; set; }

    /// <summary>
    /// <para>俄语</para>
    /// </summary>
    [JsonPropertyName("ru_ru")]
    public string? RuRu { get; set; }

    /// <summary>
    /// <para>泰语</para>
    /// </summary>
    [JsonPropertyName("th_th")]
    public string? ThTh { get; set; }

    /// <summary>
    /// <para>西班牙语</para>
    /// </summary>
    [JsonPropertyName("es_es")]
    public string? EsEs { get; set; }

    /// <summary>
    /// <para>韩语</para>
    /// </summary>
    [JsonPropertyName("ko_kr")]
    public string? KoKr { get; set; }
}