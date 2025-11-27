// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroupMember;

/// <summary>
/// 获取群组成员分页列表结果
/// <para>表示获取群组成员列表的分页结果信息</para>
/// </summary>
public class GetMemberPageListResult : ApiPageListResult
{
    /// <summary>
    /// 成员项目列表
    /// <para>包含当前页码下的所有群组成员信息</para>
    /// </summary>
    [JsonPropertyName("items")]
    public List<ChatGroupMemberItem>? Items { get; set; }

    /// <summary>
    /// 成员总数
    /// <para>群组中所有成员的总数量，不受分页影响</para>
    /// </summary>
    [JsonPropertyName("member_total")]
    public int MemberTotal { get; set; }
}

/// <summary>
/// 聊天群组成员项目
/// <para>表示群组中单个成员的详细信息</para>
/// </summary>
public class ChatGroupMemberItem
{
    /// <summary>
    /// 成员ID类型
    /// <para>标识成员ID的类型，如：user_id、open_id、union_id等</para>
    /// </summary>
    [JsonPropertyName("member_id_type")]
    public string? MemberIdType { get; set; }

    /// <summary>
    /// 成员ID
    /// <para>群组成员的唯一标识符</para>
    /// </summary>
    [JsonPropertyName("member_id")]
    public string? MemberId { get; set; }

    /// <summary>
    /// 成员名称
    /// <para>群组成员的显示名称</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 租户标识
    /// <para>标识成员所属的租户，用于多租户环境下的区分</para>
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }
}
