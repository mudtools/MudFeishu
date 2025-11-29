// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatTabs;

/// <summary>
/// 添加会话标签页请求体
/// <para>在指定会话内添加自定义会话标签页，仅支持添加文档类型（doc）或 URL （url）类型的标签页。</para>
/// </summary>
public class CreateChatTabsRequest
{
    /// <summary>
    /// <para>会话标签页</para>
    /// <para>**注意**：一个会话内最多只允许添加 20 个自定义会话标签页</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("chat_tabs")]
    public ChatTabData[] ChatTabs { get; set; } = Array.Empty<ChatTabData>();

}

/// <summary>
/// <para>会话标签页</para>
/// <para>**注意**：一个会话内最多只允许添加 20 个自定义会话标签页</para>
/// </summary>
public class ChatTabData
{
    /// <summary>
    /// <para>会话标签页名称</para>
    /// <para>**注意**：会话标签页的名称不能超过 60 个字符</para>
    /// <para>必填：否</para>
    /// <para>示例值：文档</para>
    /// </summary>
    [JsonPropertyName("tab_name")]
    public string? TabName { get; set; }

    /// <summary>
    /// <para>会话标签页类型</para>
    /// <para>**注意**：只支持添加 doc、url 类型的标签页，其他字段为只读字段</para>
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
    /// <item>files_resources：合并类型，包含文件、Doc 文档、URL 链接</item>
    /// <item>images_videos：合并类型，包含图片、视频</item>
    /// <item>task：任务</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("tab_type")]
    public string TabType { get; set; } = string.Empty;

    /// <summary>
    /// <para>会话标签页的内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tab_content")]
    public ChatTabContentData? TabContent { get; set; }

    /// <summary>
    /// <para>会话标签页的配置</para>
    /// <para>**注意**：仅当 tab_type 取值为 url 时，该参数生效</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tab_config")]
    public ChatTabConfigData? TabConfig { get; set; }


}

/// <summary>
/// <para>会话标签页的内容</para>
/// </summary>
public class ChatTabContentData
{
    /// <summary>
    /// <para>URL 地址，在 tab_type 取值为 url 时生效</para>
    /// <para>**注意**：</para>
    /// <para>- tab_type 取值为 url 时url不能为空</para>
    /// <para>- 必须以 http 或 https 开头</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://www.feishu.cn</para>
    /// </summary>
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    /// <summary>
    /// <para>云文档链接，在 tab_type 取值为 doc 时生效</para>
    /// <para>**注意**：</para>
    /// <para>- tab_type 取值为 doc 时doc不能为空</para>
    /// <para>- 必须以 http 或 https 开头</para>
    /// <para>- 当前操作者必须有云文档的协作者权限</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.feishu.cn/wiki/wikcnPIcqWjJQwkwDzrB9t40123xz</para>
    /// </summary>
    [JsonPropertyName("doc")]
    public string? Doc { get; set; }

    /// <summary>
    /// <para>会议纪要，因不支持添加 meeting_minute 类型的会话标签页，该字段为只读字段，无需传值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://example.feishu.cn/docs/doccnvIXbV22i6hSD3utar4123dx</para>
    /// </summary>
    [JsonPropertyName("meeting_minute")]
    public string? MeetingMinute { get; set; }

    /// <summary>
    /// <para>任务，因不支持添加 task 类型的会话标签页，该字段为只读字段，无需传值。</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://bytedance.feishu.cn/client/todo/task_list?guid=fa03fb6d-344b-47d9-83e3-049e3b3da931</para>
    /// </summary>
    [JsonPropertyName("task")]
    public string? Task { get; set; }
}

/// <summary>
/// <para>会话标签页的配置</para>
/// <para>**注意**：仅当 tab_type 取值为 url 时，该参数生效</para>
/// </summary>
public class ChatTabConfigData
{
    /// <summary>
    /// <para>会话标签页的图标。需要先调用[上传图片](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/image/create) 接口，图片类型设置为 message 上传图片，然后获取 `image_key` 后传入当前参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：img_v2_b99741-7628-4abd-aad0-b881e4db83ig</para>
    /// </summary>
    [JsonPropertyName("icon_key")]
    public string? IconKey { get; set; }

    /// <summary>
    /// <para>会话标签页是否在 App 内嵌打开</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("is_built_in")]
    public bool? IsBuiltIn { get; set; }
}