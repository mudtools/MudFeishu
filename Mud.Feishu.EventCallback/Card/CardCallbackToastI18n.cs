// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Card;

/// <summary>
/// 卡片事件回调 Toast 的多语言提示文本。
/// </summary>
/// <remarks>
/// 键为语言选项；未命中的语言回退到 <see cref="CardCallbackToast.Content"/>。
/// </remarks>
[HttpJsonSerializable(SerializerClassName = "Card")]
public class CardCallbackToastI18n
{
    /// <summary><para>简体中文</para></summary>
    [JsonPropertyName("zh_cn")]
    public string? ZhCn { get; set; }

    /// <summary><para>英语</para></summary>
    [JsonPropertyName("en_us")]
    public string? EnUs { get; set; }

    /// <summary><para>繁体中文（香港）</para></summary>
    [JsonPropertyName("zh_hk")]
    public string? ZhHk { get; set; }

    /// <summary><para>繁体中文（台湾）</para></summary>
    [JsonPropertyName("zh_tw")]
    public string? ZhTw { get; set; }

    /// <summary><para>日语</para></summary>
    [JsonPropertyName("ja_jp")]
    public string? JaJp { get; set; }

    /// <summary><para>印尼语</para></summary>
    [JsonPropertyName("id_id")]
    public string? IdId { get; set; }

    /// <summary><para>越南语</para></summary>
    [JsonPropertyName("vi_vn")]
    public string? ViVn { get; set; }

    /// <summary><para>泰语</para></summary>
    [JsonPropertyName("th_th")]
    public string? ThTh { get; set; }

    /// <summary><para>葡萄牙语（巴西）</para></summary>
    [JsonPropertyName("pt_br")]
    public string? PtBr { get; set; }

    /// <summary><para>西班牙语</para></summary>
    [JsonPropertyName("es_es")]
    public string? EsEs { get; set; }

    /// <summary><para>韩语</para></summary>
    [JsonPropertyName("ko_kr")]
    public string? KoKr { get; set; }

    /// <summary><para>德语</para></summary>
    [JsonPropertyName("de_de")]
    public string? DeDe { get; set; }

    /// <summary><para>法语</para></summary>
    [JsonPropertyName("fr_fr")]
    public string? FrFr { get; set; }

    /// <summary><para>意大利语</para></summary>
    [JsonPropertyName("it_it")]
    public string? ItIt { get; set; }

    /// <summary><para>俄语</para></summary>
    [JsonPropertyName("ru_ru")]
    public string? RuRu { get; set; }

    /// <summary><para>马来语</para></summary>
    [JsonPropertyName("ms_my")]
    public string? MsMy { get; set; }
}
