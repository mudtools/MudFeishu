// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 批量消息撤回进度信息类
/// 用于表示飞书平台批量消息撤回操作的执行状态和统计信息
/// </summary>
public class BatchMessageRecallProgress
{
    /// <summary>
    /// 撤回操作是否成功执行
    /// </summary>
    [JsonPropertyName("recall")]
    public bool Recall { get; set; }

    /// <summary>
    /// 已撤回的消息数量
    /// </summary>
    [JsonPropertyName("recall_count")]
    public int RecallCount { get; set; }
}