// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.UserGroupMember;

/// <summary>
/// 添加用户组成员请求体
/// </summary>
public class UserGroupMemberRequest
{
    /// <summary>
    /// 用户组成员的类型，目前仅支持选择 user。
    /// </summary>
    [JsonPropertyName("member_type")]
    public required string MemberType { get; set; } = "user";

    /// <summary>
    /// 当 member_type 取值为 user时，通过该参数设置用户 ID 类型。
    /// </summary>
    [JsonPropertyName("member_id_type")]
    public required string MemberIdType { get; set; } = "open_id";

    /// <summary>
    /// 添加的用户 ID，ID 类型与 member_id_type 的取值保持一致。
    /// </summary>
    [JsonPropertyName("member_id")]
    public required string MemberId { get; set; }
}
