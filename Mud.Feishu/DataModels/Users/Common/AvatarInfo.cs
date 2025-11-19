// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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


