// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;


/// <summary>
/// <para>高亮块 Block</para>
/// </summary>
public class BlockCallout
{
    /// <summary>
    /// <para>高亮块背景色</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：浅红色</item>
    /// <item>2：浅橙色</item>
    /// <item>3：浅黄色</item>
    /// <item>4：浅绿色</item>
    /// <item>5：浅蓝色</item>
    /// <item>6：浅紫色</item>
    /// <item>7：浅灰色</item>
    /// <item>8：暗红色</item>
    /// <item>9：暗橙色</item>
    /// <item>10：暗黄色</item>
    /// <item>11：暗绿色</item>
    /// <item>12：暗蓝色</item>
    /// <item>13：暗紫色</item>
    /// <item>14：暗灰色</item>
    /// <item>15：暗银灰色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("background_color")]
    public int? BackgroundColor { get; set; }

    /// <summary>
    /// <para>边框色</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：红色</item>
    /// <item>2：橙色</item>
    /// <item>3：黄色</item>
    /// <item>4：绿色</item>
    /// <item>5：蓝色</item>
    /// <item>6：紫色</item>
    /// <item>7：灰色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("border_color")]
    public int? BorderColor { get; set; }

    /// <summary>
    /// <para>文字颜色</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：粉红色</item>
    /// <item>2：橙色</item>
    /// <item>3：黄色</item>
    /// <item>4：绿色</item>
    /// <item>5：蓝色</item>
    /// <item>6：紫色</item>
    /// <item>7：灰色</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("text_color")]
    public int? TextColor { get; set; }

    /// <summary>
    /// <para>高亮块图标</para>
    /// <para>必填：否</para>
    /// <para>示例值：pushpin</para>
    /// </summary>
    [JsonPropertyName("emoji_id")]
    public string? EmojiId { get; set; }
}