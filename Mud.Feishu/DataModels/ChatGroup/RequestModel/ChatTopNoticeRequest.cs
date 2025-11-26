// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 群置顶操作请求体。
/// </summary>
public class ChatTopNoticeRequest
{
    /// <summary>
    /// 群置顶配置
    /// </summary>
    [JsonPropertyName("chat_top_notice")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        List<ChatTopNotice> ChatTopNotice
    { get; set; } = [];
}

/// <summary>
/// 群置顶配置
/// </summary>
public class ChatTopNotice
{
    /// <summary>
    /// 置顶类型 示例值："2"
    /// <para>可选值有：</para>
    /// <para>1：消息类型，必需填写 message_id</para>
    /// <para>2：群公告类型，无需填写 message_id</para>
    /// </summary>
    [JsonPropertyName("action_type")]
    public string? ActionType { get; set; } = "2";

    /// <summary>
    /// 消息 ID。示例值："om_dc13264520392913993dd051dba21dcf"
    /// </summary>
    [JsonPropertyName("message_id")]
    public string? MessageId { get; set; }
}
