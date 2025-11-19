// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组更新请求体。
/// </summary>
public class UserGroupUpdateRequest
{
    /// <summary>
    /// 用户组名字，长度不能超过 100 字符。
    /// <para>说明：用户组名称企业内唯一，如重复设置则会创建失败。</para>
    /// <para>默认值：空，表示不更新用户组名字。</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 用户组描述，长度不能超过 500 字符。
    /// <para>默认值：空，表示不更新用户组描述。</para>
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}
