// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Wiki;

/// <summary>
/// 更新知识空间设置请求体
/// </summary>
public class UpdateSpaceSettingRequest
{
    /// <summary>
    /// <para>谁可以创建空间的一级页面： "admin_and_member" = 管理员和成员 "admin" - 仅管理员</para>
    /// <para>必填：否</para>
    /// <para>示例值：admin/admin_and_member</para>
    /// </summary>
    [JsonPropertyName("create_setting")]
    public string? CreateSetting { get; set; }

    /// <summary>
    /// <para>可阅读用户可否创建副本/打印/导出/复制： "allow" - 允许 "not_allow" - 不允许</para>
    /// <para>必填：否</para>
    /// <para>示例值：allow/not_allow</para>
    /// </summary>
    [JsonPropertyName("security_setting")]
    public string? SecuritySetting { get; set; }

    /// <summary>
    /// <para>可阅读用户可否评论： "allow" - 允许 "not_allow" - 不允许</para>
    /// <para>必填：否</para>
    /// <para>示例值：allow/not_allow</para>
    /// </summary>
    [JsonPropertyName("comment_setting")]
    public string? CommentSetting { get; set; }
}
