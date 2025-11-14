namespace Mud.Feishu.DataModels.UserGroup;

/// <summary>
/// 用户组信息请求体。
/// </summary>
public class UserGroupInfoRequest
{
    /// <summary>
    /// 用户组名字，长度不能超过 100 字符。
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    /// <summary>
    /// 用户组描述，长度不能超过 500 字符。
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>
    /// 用户组的类型。默认取值 1，表示普通用户组。2：暂不支持使用该值
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// 自定义用户组 ID。
    /// </summary>
    [JsonPropertyName("group_id")]
    public string? GroupId { get; set; }
}
