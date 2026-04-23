// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 表情回复信息
/// </summary>
public class Reaction
{
    /// <summary>
    /// 表情回复的键值，如 ANGRY、LIKE、LOVE 等
    /// </summary>
    [JsonPropertyName("reaction_key")]
    public string ReactionKey { get; set; }

    /// <summary>
    /// 该表情回复的总数量
    /// </summary>
    [JsonPropertyName("count")]
    public int Count { get; set; }

    /// <summary>
    /// 前几位使用该表情的用户ID列表
    /// </summary>
    [JsonPropertyName("ahead_users")]
    public List<string> AheadUsers { get; set; }
}
