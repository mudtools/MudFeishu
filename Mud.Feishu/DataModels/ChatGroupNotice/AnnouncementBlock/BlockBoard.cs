// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;

/// <summary>
/// <para>画板 Block</para>
/// </summary>
public class BlockBoard
{
    /// <summary>
    /// <para>画板 token</para>
    /// <para>必填：否</para>
    /// <para>示例值：EfSPwsv03hVDKJbh1FWczJbWn90</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>对齐方式</para>
    /// <para>必填：否</para>
    /// <para>示例值：2</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：居左排版</item>
    /// <item>2：居中排版</item>
    /// <item>3：居右排版</item>
    /// </list></para>
    /// <para>默认值：1</para>
    /// </summary>
    [JsonPropertyName("align")]
    public int? Align { get; set; }

    /// <summary>
    /// <para>宽度，单位 px；不填时自动适应文档宽度；值超出文档最大宽度时，页面渲染为文档最大宽度</para>
    /// <para>必填：否</para>
    /// <para>示例值：300</para>
    /// <para>最小值：90</para>
    /// </summary>
    [JsonPropertyName("width")]
    public int? Width { get; set; }

    /// <summary>
    /// <para>高度，单位 px；不填时自动根据画板内容计算；值超出屏幕两倍高度时，页面渲染为屏幕两倍高度</para>
    /// <para>必填：否</para>
    /// <para>示例值：300</para>
    /// <para>最小值：90</para>
    /// </summary>
    [JsonPropertyName("height")]
    public int? Height { get; set; }
}