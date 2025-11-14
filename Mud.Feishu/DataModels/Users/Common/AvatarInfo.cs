namespace Mud.Feishu.DataModels.Users;


/// <summary>
/// 用户头像信息，包含不同尺寸的头像URL。
/// </summary>
public class AvatarInfo
{
    /// <summary>
    /// 72x72像素的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_72")]
    public string? Avatar72 { get; set; }

    /// <summary>
    /// 240x240像素的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_240")]
    public string? Avatar240 { get; set; }

    /// <summary>
    /// 640x640像素的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_640")]
    public string? Avatar640 { get; set; }

    /// <summary>
    /// 原始尺寸的头像URL。
    /// </summary>
    [JsonPropertyName("avatar_origin")]
    public string? AvatarOrigin { get; set; }
}


