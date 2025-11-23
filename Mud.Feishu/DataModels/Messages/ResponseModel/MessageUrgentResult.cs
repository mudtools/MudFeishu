// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Messages;

/// <summary>
/// 消息加急操作的结果信息。
/// 包含无效的用户ID列表，用于标识在执行加急操作时哪些用户ID是无效的。
/// </summary>
public class MessageUrgentResult
{
    /// <summary>
    /// 无效的用户 ID 列表。当执行消息加急操作时，系统无法识别或处理的用户 ID 将出现在此列表中。
    /// </summary>
    [JsonPropertyName("invalid_user_id_list")]
    public List<string> InvalidUserIdList { get; set; } = [];
}