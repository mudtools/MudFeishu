// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatTabs;

/// <summary>
/// 更新会话标签页请求体
/// <para>在指定会话内添加自定义会话标签页，仅支持添加文档类型（doc）或 URL （url）类型的标签页。</para>
/// </summary>
public class UpdateChatTabsRequest
{
    /// <summary>
    /// <para>会话标签页</para>
    /// <para>**注意**：一个会话内最多只允许添加 20 个自定义会话标签页</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("chat_tabs")]
    public ChatTabUpdateData[] ChatTabs { get; set; } = Array.Empty<ChatTabUpdateData>();
}

/// <summary>
/// <para>会话标签页</para>
/// <para>**注意**：一个会话内最多只允许添加 20 个自定义会话标签页</para>
/// </summary>
public class ChatTabUpdateData : ChatTabCreateData
{
    /// <summary>
    /// 会话标签页 ID
    /// </summary>
    [JsonPropertyName("tab_id")]
    public string? TabId { get; set; }
}
