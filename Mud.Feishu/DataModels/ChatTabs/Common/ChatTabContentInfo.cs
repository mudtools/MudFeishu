// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatTabs;



/// <summary>
/// <para>会话标签页内容</para>
/// </summary>
public class ChatTabContentInfo
{
    /// <summary>
    /// <para>url 类型标签页对应的 URL 地址</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>文档类型标签页对应的云文档链接</para>
    /// </summary>
    [JsonPropertyName("doc")]
    public string? Doc { get; set; }

    /// <summary>
    /// <para>会议纪要类型标签页对应的会议纪要地址</para>
    /// </summary>
    [JsonPropertyName("meeting_minute")]
    public string? MeetingMinute { get; set; }

    /// <summary>
    /// <para>任务</para>
    /// </summary>
    [JsonPropertyName("task")]
    public string? Task { get; set; }
}
