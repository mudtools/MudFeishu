// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Organization;

/// <summary>
/// OrganizationUserGroup
/// <para>用户组对象。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Organization")]
public class OrganizationUserGroup
{
    /// <summary>
    /// <para>用户组的自定义 ID。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("user_group_id")]
    public string? UserGroupId { get; set; }

    /// <summary>
    /// <para>用户组的名称。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `100` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>用户组的类型。</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：普通用户组。</para>
    /// <para>- 2：动态用户组。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>成员数量。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("member_count")]
    public int? MemberCount { get; set; }

    /// <summary>
    /// <para>用户组状态。</para>
    /// <para>**可选值有**：</para>
    /// <para>- 0：未知。</para>
    /// <para>- 1：计算完毕。</para>
    /// <para>- 2：计算中。</para>
    /// <para>- 3：计算失败。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("status")]
    public int? Status { get; set; }
}
