// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Search;

/// <summary>
/// 搜索消息请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Search")]
public class SearchMessageRequest
{
    /// <summary>
    /// <para>搜索关键词</para>
    /// <para>必填：是</para>
    /// <para>示例值：测试消息</para>
    /// </summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// <para>消息来自user_id列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_1970b39a6730a4a8e97b530d8cb14ccb</para>
    /// </summary>
    [JsonPropertyName("from_ids")]
    public string[]? FromIds { get; set; }

    /// <summary>
    /// <para>消息所在chat_id列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：oc_c063434856a818a615fd36697a9ffe09</para>
    /// </summary>
    [JsonPropertyName("chat_ids")]
    public string[]? ChatIds { get; set; }

    /// <summary>
    /// <para>消息类型(file/image/media)</para>
    /// <para>必填：否</para>
    /// <para>示例值：image</para>
    /// <para>可选值：<list type="bullet">
    /// <item>file：文件</item>
    /// <item>image：图片</item>
    /// <item>media：视频</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("message_type")]
    public string? MessageType { get; set; }

    /// <summary>
    /// <para>at用户user_id列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_1970b39a6730a4a8e97b530d8cb14ccb</para>
    /// </summary>
    [JsonPropertyName("at_chatter_ids")]
    public string[]? AtChatterIds { get; set; }

    /// <summary>
    /// <para>消息来自类型(bot/user)</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>bot：机器人</item>
    /// <item>user：用户</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("from_type")]
    public string? FromType { get; set; }

    /// <summary>
    /// <para>会话类型(group_chat/p2p_chat)</para>
    /// <para>必填：否</para>
    /// <para>示例值：group_chat</para>
    /// <para>可选值：<list type="bullet">
    /// <item>group_chat：群聊</item>
    /// <item>p2p_chat：单聊</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("chat_type")]
    public string? ChatType { get; set; }

    /// <summary>
    /// <para>消息发送起始时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609296809</para>
    /// </summary>
    [JsonPropertyName("start_time")]
    public string? StartTime { get; set; }

    /// <summary>
    /// <para>消息发送结束时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1609296809</para>
    /// </summary>
    [JsonPropertyName("end_time")]
    public string? EndTime { get; set; }
}
