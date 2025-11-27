// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupMember;

/// <summary>
/// 添加成员结果
/// <para>表示添加成员操作的结果信息</para>
/// </summary>
public class AddMemberResult
{
    /// <summary>
    /// 无效ID列表
    /// <para>包含格式不正确或无法解析的用户ID</para>
    /// <para>这些ID无法被系统识别或处理</para>
    /// </summary>
    [JsonPropertyName("invalid_id_list")]
    public List<string>? InvalidIdList { get; set; }

    /// <summary>
    /// 不存在ID列表
    /// <para>包含系统中不存在的用户ID</para>
    /// <para>这些用户可能已被删除或从未注册过</para>
    /// </summary>
    [JsonPropertyName("not_existed_id_list")]
    public List<string>? NotExistedIdList { get; set; }

    /// <summary>
    /// 待审批ID列表
    /// <para>包含需要等待审批才能加入群组的用户ID</para>
    /// <para>这些用户的加群申请需要管理员批准后才能正式加入</para>
    /// </summary>
    [JsonPropertyName("pending_approval_id_list")]
    public List<string>? PendingApprovalIdList { get; set; }
}
