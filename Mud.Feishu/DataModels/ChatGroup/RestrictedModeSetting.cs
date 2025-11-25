// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.ChatGroup;

/// <summary>
/// 保密模式设置
/// </summary>
public class RestrictedModeSetting
{
    /// <summary>
    /// 保密模式是否开启
    /// </summary>
    [JsonPropertyName("status")]
    public bool Status { get; set; }

    /// <summary>
    /// 允许截屏录屏
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>all_members：所有成员允许截屏录屏</para>
    /// <para>not_anyone：所有成员禁止截屏录屏</para>
    /// </summary>
    [JsonPropertyName("screenshot_has_permission_setting")]
    public string ScreenshotHasPermissionSetting { get; set; } = "all_members";

    /// <summary>
    /// 允许下载消息中图片、视频和文件
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>all_members：所有成员允许下载资源</para>
    /// <para>not_anyone：所有成员禁止下载资源</para>
    /// </summary>
    [JsonPropertyName("download_has_permission_setting")]
    public string DownloadHasPermissionSetting { get; set; } = "all_members";

    /// <summary>
    /// 允许复制和转发消息
    /// <para>示例值："all_members"</para>
    /// <para>可选值有：</para>
    /// <para>all_members：所有成员允许复制和转发消息</para>
    /// <para>not_anyone：所有成员禁止复制和转发消息</para>
    /// </summary>
    [JsonPropertyName("message_has_permission_setting")]
    public string MessageHasPermissionSetting { get; set; } = "all_members";
}
