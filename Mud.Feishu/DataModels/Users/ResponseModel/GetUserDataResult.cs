// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
