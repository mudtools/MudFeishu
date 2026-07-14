// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>云文档的点赞者列表</para>
/// </summary>
public class FileLikeInfo
{
    /// <summary>
    /// <para>用户 ID。与查询参数 user_id_type 一致</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_3bbe8a09c20e89cce9bff989ed840674</para>
    /// </summary>
    [JsonPropertyName("user_id")]
    public string? UserId { get; set; }

    /// <summary>
    /// <para>用户最后点赞时间，秒级时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1690857821</para>
    /// </summary>
    [JsonPropertyName("last_liked_time")]
    public string? LastLikedTime { get; set; }

    /// <summary>
    /// <para>用户名字，用户信息被脱敏时此值不会返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：张三</para>
    /// </summary>
    [JsonPropertyName("user_name")]
    public string? UserName { get; set; }

    /// <summary>
    /// <para>用户英文名字，用户信息被脱敏时此值不会返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：San Zhang</para>
    /// </summary>
    [JsonPropertyName("user_en_name")]
    public string? UserEnName { get; set; }

    /// <summary>
    /// <para>用户头像，用户信息被脱敏时此值不会返回</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://image.mudtools.com/xxxx</para>
    /// </summary>
    [JsonPropertyName("user_avatar_url")]
    public string? UserAvatarUrl { get; set; }

    /// <summary>
    /// <para>用户信息是否脱敏</para>
    /// <para>必填：否</para>
    /// <para>示例值：false</para>
    /// </summary>
    [JsonPropertyName("user_is_desensitized")]
    public bool? UserIsDesensitized { get; set; }
}
