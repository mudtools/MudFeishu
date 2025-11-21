// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 合并接收消息结果类，用于表示批量获取消息的结果
/// </summary>
public class MergeReceiveMessageResult
{
    /// <summary>
    /// 获取到的消息详情
    /// </summary>
    [JsonPropertyName("message")]
    public MergeReceiveMessageDetail? Message { get; set; }

    /// <summary>
    /// 无效的消息ID列表
    /// </summary>
    [JsonPropertyName("invalid_message_id_list")]
    public List<string> InvalidMessageIdList { get; set; } = [];
}