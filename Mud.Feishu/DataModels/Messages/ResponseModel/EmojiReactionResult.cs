// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 表示飞书消息反应的结果信息
/// 包含关于谁在何时添加或移除了哪种类型的表情反应的信息
/// </summary>
public class EmojiReactionResult
{
    /// <summary>
    /// 获取或设置反应的唯一标识符
    /// </summary>
    [JsonPropertyName("reaction_id")]
    public string? ReactionId { get; set; }

    /// <summary>
    /// 获取或设置执行反应操作的用户信息
    /// </summary>
    [JsonPropertyName("operator")]
    public ReactionOperator? Operator { get; set; }

    /// <summary>
    /// 获取或设置操作时间
    /// </summary>
    [JsonPropertyName("action_time")]
    public string? ActionTime { get; set; }

    /// <summary>
    /// 获取或设置反应类型
    /// </summary>
    [JsonPropertyName("reaction_type")]
    public ReactionType? ReactionType { get; set; }
}

/// <summary>
/// 表示执行反应操作的用户信息
/// 包含操作者的ID和类型（如用户或应用）
/// </summary>
public class ReactionOperator
{
    /// <summary>
    /// 获取或设置操作者的唯一标识符
    /// </summary>
    [JsonPropertyName("operator_id")]
    public string? OperatorId { get; set; }

    /// <summary>
    /// 获取或设置操作者类型
    /// </summary>
    [JsonPropertyName("operator_type")]
    public string? OperatorType { get; set; }
}