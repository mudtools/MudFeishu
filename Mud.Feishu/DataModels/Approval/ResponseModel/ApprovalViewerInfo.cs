// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Approval;

/// <summary>
/// <para>审批定义的可见人列表</para>
/// </summary>
public class ApprovalViewerInfo
{
    /// <summary>
    /// <para>可见人类型</para>
    /// <para>示例值：TENANT</para>
    /// <para>可选值：<list type="bullet">
    /// <item>TENANT：企业内可见</item>
    /// <item>DEPARTMENT：指定部门</item>
    /// <item>USER：指定用户</item>
    /// <item>ROLE：指定角色</item>
    /// <item>USER_GROUP：指定用户组</item>
    /// <item>NONE：任何人都不可见</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>资源 ID。</para>
    /// <para>- 在可见人类型为 DEPARTMENT 时，ID 为部门 ID。</para>
    /// <para>- 在可见人类型为 USER 时，ID 为用户 open_id。</para>
    /// <para>- 在可见人类型为 ROLE 时，ID 为角色 ID。</para>
    /// <para>- 在可见人类型为 USER_GROUP 时，ID 为用户组 ID。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_e03053f0541cecc3269d7a9dc34a0b21</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>在可见人类型为 USER 时，表示可见人用户 open_id。</para>
    /// <para>示例值：f7cb567e</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}