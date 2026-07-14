// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;

/// <summary>
/// <para>会议权限配置列表，如果存在相同的权限配置项则它们之间为"逻辑或"的关系（即 有一个为true则拥有该权限）</para>
/// </summary>
public class ReserveActionPermission
{
    /// <summary>
    /// <para>权限项</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：是否能成为主持人</item>
    /// <item>2：是否能邀请参会人</item>
    /// <item>3：是否能加入会议</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("permission")]
    public int Permission { get; set; }

    /// <summary>
    /// <para>权限检查器列表，权限检查器之间为"逻辑或"的关系（即 有一个为true则拥有该权限）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("permission_checkers")]
    public ReservePermissionChecker[] PermissionCheckers { get; set; } = [];


}