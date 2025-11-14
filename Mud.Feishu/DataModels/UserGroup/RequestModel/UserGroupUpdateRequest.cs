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
