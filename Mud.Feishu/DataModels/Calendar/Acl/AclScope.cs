// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Calendar;


/// <summary>
/// <para>权限的生效范围。</para>
/// </summary>
public class AclScope
{
    /// <summary>
    /// <para>权限生效范围的类型。</para>
    /// <para>**注意**：目前只支持 `user`，且当 `type=user` 时，user_id 需要传入和 user_id_type 一致的用户 ID 类型。例如，`user_id_type=open_id` 时，user_id 需要传入用户的 open_id。</para>
    /// <para>必填：是</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>用户 ID。当 `type=user` 时，必须设置该参数值。关于用户 ID 的更多介绍可参见[用户相关的 ID 概念](https://open.feishu.cn/document/home/user-identity-introduction/introduction)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_xxxxxx</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }
}