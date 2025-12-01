// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Cards;

/// <summary>
/// 创建卡片实体请求体
/// <para>基于卡片 JSON 代码或卡片搭建工具搭建的卡片，创建卡片实体。用于后续通过卡片实体 ID（card_id）发送卡片、更新卡片等。</para>
/// </summary>
public class CreateCardRequest
{
    /// <summary>
    /// <para>卡片类型。可选值：</para>
    /// <para>- `card_json`：由卡片 JSON 代码构建的卡片</para>
    /// <para>- `template`：由[卡片搭建工具]搭建的卡片模板</para>
    /// <para>必填：是</para>
    /// <para>示例值：card_json</para>
    /// </summary>
    [JsonPropertyName("type")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string Type
    { get; set; } = string.Empty;

    /// <summary>
    /// <para>卡片数据。需要与 `type` 指定的类型一致：</para>
    /// <para>- 若 `type` 为 `card_json`，则此处应传卡片 JSON 代码，并确保将其转义为字符串。</para>
    /// <para>- 若 `type` 为 `template`，则此处应传卡片模板的数据，并确保将其转义为字符串。</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("data")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string Data
    { get; set; } = string.Empty;
}
