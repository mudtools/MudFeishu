namespace Mud.Feishu.DataModels.Users;

/// <summary>
/// 获取用户数据的返回结果。
/// </summary>
public class GetUserDataResult : UserData
{
    /// <summary>
    /// 头像URL。
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// 头像缩略图URL。
    /// </summary>
    [JsonPropertyName("avatar_thumb")]
    public string? AvatarThumb { get; set; }

    /// <summary>
    /// 中等尺寸头像URL。
    /// </summary>
    [JsonPropertyName("avatar_middle")]
    public string? AvatarMiddle { get; set; }

    /// <summary>
    /// 大头像URL。
    /// </summary>
    [JsonPropertyName("avatar_big")]
    public string? AvatarBig { get; set; }

    /// <summary>
    /// 用户在当前应用中的唯一标识。
    /// </summary>
    [JsonPropertyName("open_id")]
    public string? OpenId { get; set; }

    /// <summary>
    /// 用户在飞书开放平台下的唯一标识。
    /// </summary>
    [JsonPropertyName("union_id")]
    public string? UnionId { get; set; }

    /// <summary>
    /// 租户Key。
    /// </summary>
    [JsonPropertyName("tenant_key")]
    public string? TenantKey { get; set; }
}
