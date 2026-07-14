// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Files;

/// <summary>
/// <para>访问记录列表</para>
/// </summary>
public class FileViewRecord
{
    /// <summary>
    /// <para>访问者 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：ou_cc19b2bfb93f8a44db4b4d6eababcef</para>
    /// </summary>
    [JsonPropertyName("viewer_id")]
    public string? ViewerId { get; set; }

    /// <summary>
    /// <para>访问者姓名</para>
    /// <para>必填：否</para>
    /// <para>示例值：zhangsan</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>访问者头像的 URL</para>
    /// <para>必填：否</para>
    /// <para>示例值：https://foo.icon.com/xxxx</para>
    /// </summary>
    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// <para>最近访问时间。Unix 时间戳，单位为秒。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1679284285</para>
    /// </summary>
    [JsonPropertyName("last_view_time")]
    public string? LastViewTime { get; set; }
}

