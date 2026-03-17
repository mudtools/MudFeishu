// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// 删除知识空间成员请求体
/// </summary>
public class DeleteSpaceMemberRequest
{
    /// <summary>
    /// <para>“openchat” - 群id</para>
    /// <para>“userid” - 用户id</para>
    /// <para>“email” - 邮箱</para>
    /// <para>“opendepartmentid” - 部门id</para>
    /// <para>“openid” - 应用openid</para>
    /// <para>“unionid” - [unionid](/:ssltoken/home/user-identity-introduction/union-id</para>
    /// <para>)</para>
    /// <para>必填：是</para>
    /// <para>示例值：userid</para>
    /// </summary>
    [JsonPropertyName("member_type")]
    public string MemberType { get; set; } = string.Empty;

    /// <summary>
    /// <para>角色:</para>
    /// <para>“admin” - 管理员</para>
    /// <para>“member” - 成员</para>
    /// <para>必填：是</para>
    /// <para>示例值：admin</para>
    /// </summary>
    [JsonPropertyName("member_role")]
    public string MemberRole { get; set; } = string.Empty;

    /// <summary>
    /// <para>知识库协作者类型（暂不支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// <item>chat：群组</item>
    /// <item>department：组织架构</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}
