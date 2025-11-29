// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatTabs;

/// <summary>
/// 添加会话标签页 响应体
/// <para>在指定会话内添加自定义会话标签页，仅支持添加文档类型（doc）或 URL （url）类型的标签页。</para>
/// </summary>
public class ChatTabsResult
{
    /// <summary>
    /// <para>会话标签页</para>
    /// </summary>
    [JsonPropertyName("chat_tabs")]
    public ChatTabInfo[]? ChatTabs { get; set; }

}


/// <summary>
/// <para>会话标签页</para>
/// </summary>
public class ChatTabInfo
{
    /// <summary>
    /// <para>会话标签页 ID，建议你保存该 ID，后续更新、删除、排序标签页时需要基于该 ID 进行操作。</para>
    /// </summary>
    [JsonPropertyName("tab_id")]
    public string? TabId { get; set; }

    /// <summary>
    /// <para>会话标签页名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：文档</para>
    /// </summary>
    [JsonPropertyName("tab_name")]
    public string? TabName { get; set; }

    /// <summary>
    /// <para>会话标签页类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：doc</para>
    /// <para>可选值：<list type="bullet">
    /// <item>message：消息类型</item>
    /// <item>doc_list：云文档列表</item>
    /// <item>doc：文档</item>
    /// <item>pin：Pin</item>
    /// <item>meeting_minute：会议纪要</item>
    /// <item>chat_announcement：群公告</item>
    /// <item>url：URL</item>
    /// <item>file：文件</item>
    /// <item>files_resources：合并类型，包含文件、Doc文档、URL链接</item>
    /// <item>images_videos：合并类型，包含图片、视频</item>
    /// <item>task：任务</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("tab_type")]
    public string TabType { get; set; } = string.Empty;

    /// <summary>
    /// <para>会话标签页内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tab_content")]
    public ChatTabContentInfo? TabContent { get; set; }


    /// <summary>
    /// <para>会话标签页配置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tab_config")]
    public ChatTabConfigInfo? TabConfig { get; set; }

}


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

/// <summary>
/// <para>会话标签页配置</para>
/// </summary>
public class ChatTabConfigInfo
{
    /// <summary>
    /// <para>会话标签页图标。可调用[下载图片](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/image/get)（只能下载由当前机器人上传的图片），将图标下载至本地查看</para>
    /// </summary>
    [JsonPropertyName("icon_key")]
    public string? IconKey { get; set; }

    /// <summary>
    /// <para>会话标签页是否在 App 内嵌打开</para>
    /// </summary>
    [JsonPropertyName("is_built_in")]
    public bool? IsBuiltIn { get; set; }
}