// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.TrustParty;

/// <summary>
/// 可搜可见规则的主/客体实体集合，实体数量之和需小于100
/// </summary>
[HttpJsonSerializable(SerializerClassName = "TrustParty")]
public class CollaborationRuleEntities
{
    /// <summary>
    /// 用户 open id 列表（subjects 来自我方通讯录；objects 可通过共享成员范围/部门成员信息接口获取）
    /// </summary>
    [JsonPropertyName("open_user_ids")]
    public string[]? OpenUserIds { get; set; }

    /// <summary>
    /// 部门 open id 列表，0 代表全部成员
    /// </summary>
    [JsonPropertyName("open_department_ids")]
    public string[]? OpenDepartmentIds { get; set; }

    /// <summary>
    /// 用户组 open id 列表
    /// </summary>
    [JsonPropertyName("open_group_ids")]
    public string[]? OpenGroupIds { get; set; }
}
