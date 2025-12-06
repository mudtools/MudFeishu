// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels;

/// <summary>
/// 国际化内容节点
/// </summary>
public class I18nNames
{
    /// <summary>
    /// 简体中文
    /// </summary>
    [JsonPropertyName("zh_cn")]
    public string? ZhCn { get; set; }

    /// <summary>
    /// 英文
    /// </summary>
    [JsonPropertyName("en_us")]
    public string? EnUs { get; set; }

    /// <summary>
    /// 日文
    /// </summary>
    [JsonPropertyName("ja_jp")]
    public string? JaJp { get; set; }

    /// <summary>
    /// 繁体中文（中国香港）
    /// </summary>
    [JsonPropertyName("zh_hk")]
    public string? ZhHk { get; set; }

    /// <summary>
    /// 繁体中文（中国台湾）
    /// </summary>
    [JsonPropertyName("zh_tw")]
    public string? ZhTw { get; set; }

    /// <summary>
    /// 印尼语
    /// </summary>
    [JsonPropertyName("id_id")]
    public string? IdId { get; set; }

    /// <summary>
    /// 越南语
    /// </summary>
    [JsonPropertyName("vi_vn")]
    public string? ViVn { get; set; }

    /// <summary>
    /// 泰语
    /// </summary>
    [JsonPropertyName("th_th")]
    public string? ThTh { get; set; }

    /// <summary>
    /// 葡萄牙语
    /// </summary>
    [JsonPropertyName("pt_br")]
    public string? PtBr { get; set; }

    /// <summary>
    /// 西班牙语
    /// </summary>
    [JsonPropertyName("es_es")]
    public string? EsEs { get; set; }

    /// <summary>
    /// 韩语
    /// </summary>
    [JsonPropertyName("ko_kr")]
    public string? KoKr { get; set; }

    /// <summary>
    /// 德语
    /// </summary>
    [JsonPropertyName("de_de")]
    public string? DeDe { get; set; }

    /// <summary>
    /// 法语
    /// </summary>
    [JsonPropertyName("fr_fr")]
    public string? FrFr { get; set; }

    /// <summary>
    /// 意大利语
    /// </summary>
    [JsonPropertyName("it_it")]
    public string? ItIt { get; set; }

    /// <summary>
    /// 俄语
    /// </summary>
    [JsonPropertyName("ru_ru")]
    public string? RuRu { get; set; }

    /// <summary>
    /// 马来语
    /// </summary>
    [JsonPropertyName("ms_my")]
    public string? MsMy { get; set; }
}
