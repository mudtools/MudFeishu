// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// 添加知识空间成员请求体
/// </summary>
public class CreateSpaceMemberRequest
{
    /// <summary>
    /// <para>要添加的成员或管理员的身份类型。可选值：</para>
    /// <para>- openchat：群组 ID。</para>
    /// <para>- userid：用户 ID。</para>
    /// <para>- email：用户邮箱</para>
    /// <para>- opendepartmentid：部门 ID。</para>
    /// <para>- openid：用户的 Open ID。</para>
    /// <para>- unionid：用户的 Union ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：openid</para>
    /// </summary>
    [JsonPropertyName("member_type")]
    public string MemberType { get; set; } = string.Empty;

    /// <summary>
    /// <para>成员或管理员的 ID，值的类型由 member_type 参数决定。参考 member_type 的描述获取不同类型的 ID。</para>
    /// <para>必填：是</para>
    /// <para>示例值：ou_449b53ad6aee526f7ed311b216aabcef</para>
    /// </summary>
    [JsonPropertyName("member_id")]
    public string MemberId { get; set; } = string.Empty;

    /// <summary>
    /// <para>成员的角色类型。可选值:</para>
    /// <para>- admin：管理员</para>
    /// <para>- member：成员</para>
    /// <para>必填：是</para>
    /// <para>示例值：admin</para>
    /// </summary>
    [JsonPropertyName("member_role")]
    public string MemberRole { get; set; } = string.Empty;
}
