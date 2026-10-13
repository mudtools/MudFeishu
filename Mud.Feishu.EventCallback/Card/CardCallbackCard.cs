// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

namespace Mud.Feishu.EventCallback.Card;

/// <summary>
/// 卡片事件回调响应中的卡片数据，用于本次交互后更新卡片。
/// </summary>
/// <remarks>
/// <para><see cref="Type"/> 为 <c>raw</c> 时，<see cref="Data"/> 为卡片 JSON 数据
/// （发送时为 Card JSON 1.0 结构则回传 1.0，为 2.0 结构则回传 2.0；跨结构更新会报错 200830）。</para>
/// <para><see cref="Type"/> 为 <c>template</c> 时，<see cref="Data"/> 为 <see cref="CardCallbackTemplateData"/>。</para>
/// </remarks>
[HttpJsonSerializable(SerializerClassName = "Card")]
public class CardCallbackCard
{
    /// <summary>
    /// <para>卡片类型。枚举值有：</para>
    /// <para>- <c>template</c>：构建工具搭建的卡片模板</para>
    /// <para>- <c>raw</c>：JSON 数据搭建的卡片</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>卡片数据</para>
    /// <para><c>raw</c>：卡片 JSON（<see cref="JsonElement"/>）；<c>template</c>：<see cref="CardCallbackTemplateData"/></para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }

    /// <summary>
    /// 构造 <c>raw</c> 类型的卡片数据（JSON 搭建的卡片）。
    /// </summary>
    /// <param name="cardJson">卡片 JSON 数据。</param>
    public static CardCallbackCard Raw(JsonElement cardJson) => new() { Type = "raw", Data = cardJson };

    /// <summary>
    /// 构造 <c>template</c> 类型的卡片数据（构建工具搭建的卡片模板）。
    /// </summary>
    /// <param name="templateId">卡片模板 ID（即卡片 ID）。</param>
    /// <param name="templateVersionName">卡片模板版本，为空时使用模板当前发布版本。</param>
    /// <param name="templateVariable">卡片模板变量，格式 <c>{key:value}</c>。</param>
    public static CardCallbackCard Template(
        string templateId,
        string? templateVersionName = null,
        Dictionary<string, object?>? templateVariable = null)
        => new()
        {
            Type = "template",
            Data = new CardCallbackTemplateData
            {
                TemplateId = templateId,
                TemplateVersionName = templateVersionName,
                TemplateVariable = templateVariable
            }
        };
}
