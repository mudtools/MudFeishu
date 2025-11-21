// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 合并转发消息请求体
/// </summary>
public class MergeReceiveMessageRequest
{
    /// <summary>
    /// 待转发的消息 ID 列表，列表内的消息必须来自同一个会话。
    /// <para>示例值：["om_dc13264520392913993dd051dba21dcf"]</para>
    /// </summary>
    [JsonPropertyName("message_id_list")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        List<string> MessageIdList
    { get; set; } = [];

    /// <summary>
    /// 消息接收者 ID，ID 类型与 receive_id_type 的值一致。
    /// <para>示例值："oc_a0553eda9014c201e6969b478895c230"</para>
    /// </summary>
    [JsonPropertyName("receive_id")]
    public
#if NET7_0_OR_GREATER
        required
#endif
        string? ReceiveId
    { get; set; }
}
