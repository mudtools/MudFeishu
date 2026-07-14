// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 云文档权限模型
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class DrivePermissions
{
    /// <summary>
    /// <para>是否允许内容被分享到组织外</para>
    /// <para>必填：否</para>
    /// <para>示例值：open</para>
    /// <para>可选值：<list type="bullet">
    /// <item>open：打开，即允许内容被分享到组织外 **注意**：内容是否支持分享到组织外，还与企业的安全设置相关。如果文档位于知识库中，还与知识空间的安全设置相关。</item>
    /// <item>closed：关闭，即不允许内容被分享到组织外</item>
    /// <item>allow_share_partner_tenant：仅允许内容分享给关联组织。了解关联组织，参考飞书帮助中心文档[关联组织介绍](https://www.feishu.cn/hc/zh-CN/articles/657083794612-%E5%85%B3%E8%81%94%E7%BB%84%E7%BB%87%E4%BB%8B%E7%BB%8D)。 **注意**：只有企业管理后台设置仅允许关联组织分享，才能设置为该值。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("external_access_entity")]
    public string? ExternalAccessEntity { get; set; }

    /// <summary>
    /// <para>谁可以创建副本、打印、下载</para>
    /// <para>必填：否</para>
    /// <para>示例值：anyone_can_view</para>
    /// <para>可选值：<list type="bullet">
    /// <item>anyone_can_view：拥有可阅读权限的用户</item>
    /// <item>anyone_can_edit：拥有可编辑权限的用户</item>
    /// <item>only_full_access：拥有可管理权限（包括我）的用户</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("security_entity")]
    public string? SecurityEntity { get; set; }

    /// <summary>
    /// <para>谁可以评论</para>
    /// <para>必填：否</para>
    /// <para>示例值：anyone_can_view</para>
    /// <para>可选值：<list type="bullet">
    /// <item>anyone_can_view：拥有可阅读权限的用户</item>
    /// <item>anyone_can_edit：拥有可编辑权限的用户</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("comment_entity")]
    public string? CommentEntity { get; set; }

    /// <summary>
    /// <para>从组织维度，设置谁可以查看、添加、移除协作者</para>
    /// <para>必填：否</para>
    /// <para>示例值：anyone</para>
    /// <para>可选值：<list type="bullet">
    /// <item>anyone：所有可阅读或编辑此文档的用户</item>
    /// <item>same_tenant：组织内所有可阅读或编辑此文档的用户</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("share_entity")]
    public string? ShareEntity { get; set; }

    /// <summary>
    /// <para>从协作者维度，设置谁可以查看、添加、移除协作者</para>
    /// <para>必填：否</para>
    /// <para>示例值：collaborator_can_view</para>
    /// <para>可选值：<list type="bullet">
    /// <item>collaborator_can_view：拥有可阅读权限的协作者</item>
    /// <item>collaborator_can_edit：拥有可编辑权限的协作者</item>
    /// <item>collaborator_full_access：拥有可管理权限（包括我）的协作者</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("manage_collaborator_entity")]
    public string? ManageCollaboratorEntity { get; set; }

    /// <summary>
    /// <para>链接分享设置</para>
    /// <para>必填：否</para>
    /// <para>示例值：tenant_readable</para>
    /// <para>可选值：<list type="bullet">
    /// <item>tenant_readable：组织内获得链接的人可阅读</item>
    /// <item>tenant_editable：组织内获得链接的人可编辑</item>
    /// <item>partner_tenant_readable：[关联组织](https://www.feishu.cn/hc/zh-CN/articles/657083794612-%E5%85%B3%E8%81%94%E7%BB%84%E7%BB%87%E4%BB%8B%E7%BB%8D)的人可阅读 **注意**：只有企业管理后台设置仅允许关联组织分享，才能设置为该值。</item>
    /// <item>partner_tenant_editable：[关联组织](https://www.feishu.cn/hc/zh-CN/articles/657083794612-%E5%85%B3%E8%81%94%E7%BB%84%E7%BB%87%E4%BB%8B%E7%BB%8D)的人可编辑 **注意**：只有企业管理后台设置仅允许关联组织分享，才能设置为该值。</item>
    /// <item>anyone_readable：互联网上获得链接的任何人可阅读（仅external_access=“open” 时有效）</item>
    /// <item>anyone_editable：互联网上获得链接的任何人可编辑（仅 external_access=“open” 时有效）</item>
    /// <item>closed：关闭链接分享</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("link_share_entity")]
    public string? LinkShareEntity { get; set; }

    /// <summary>
    /// <para>谁可以复制内容</para>
    /// <para>必填：否</para>
    /// <para>示例值：anyone_can_view</para>
    /// <para>可选值：<list type="bullet">
    /// <item>anyone_can_view：拥有可阅读权限的用户</item>
    /// <item>anyone_can_edit：拥有可编辑权限的用户</item>
    /// <item>only_full_access：拥有可管理权限（包括我）的协作者</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("copy_entity")]
    public string? CopyEntity { get; set; }
}
