// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupNotice;


/// <summary>
/// <para>内嵌 Block</para>
/// </summary>
public class BlockIframe
{
    /// <summary>
    /// <para>iframe 的组成元素</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("component")]
    public IframeComponent Component { get; set; } = new();

    /// <summary>
    /// <para>iframe 的组成元素</para>
    /// </summary>
    public class IframeComponent
    {
        /// <summary>
        /// <para>iframe 类型</para>
        /// <para>必填：否</para>
        /// <para>示例值：1</para>
        /// <para>可选值：<list type="bullet">
        /// <item>1：哔哩哔哩</item>
        /// <item>2：西瓜视频</item>
        /// <item>3：优酷</item>
        /// <item>4：Airtable</item>
        /// <item>5：百度地图</item>
        /// <item>6：高德地图</item>
        /// <item>7：Undefined</item>
        /// <item>8：Figma</item>
        /// <item>9：墨刀</item>
        /// <item>10：Canva</item>
        /// <item>11：CodePen</item>
        /// <item>12：飞书问卷</item>
        /// <item>13：金数据</item>
        /// <item>14：Undefined</item>
        /// <item>15：Undefined</item>
        /// <item>99：Other</item>
        /// </list></para>
        /// </summary>
        [JsonPropertyName("iframe_type")]
        public int? IframeType { get; set; }

        /// <summary>
        /// <para>iframe 目标 url（需要进行 url_encode）</para>
        /// <para>必填：是</para>
        /// <para>示例值：https%3A%2F%2Fwww.bilibili.com%2Fvideo%2FBV1Hi4y1w7V7</para>
        /// </summary>
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;
    }
}
