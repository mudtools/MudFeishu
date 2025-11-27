// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupMember;

/// <summary>
/// 群管理员相关操作的响应结果
/// <para>表示群管理员相关操作的结果信息</para>
/// </summary>
public class GroupManagerResult
{
    /// <summary>
    /// 群管理员列表
    /// <para>包含具有群管理权限的用户ID列表</para>
    /// <para>这些用户可以管理群组设置、成员等</para>
    /// </summary>
    [JsonPropertyName("chat_managers")]
    public List<string>? ChatManagers { get; set; }

    /// <summary>
    /// 群机器人管理员列表
    /// <para>包含具有群管理权限的机器人ID列表</para>
    /// <para>这些机器人可以执行群组管理相关的自动化任务</para>
    /// </summary>
    [JsonPropertyName("chat_bot_managers")]
    public List<string>? ChatBotManagers { get; set; }
}
