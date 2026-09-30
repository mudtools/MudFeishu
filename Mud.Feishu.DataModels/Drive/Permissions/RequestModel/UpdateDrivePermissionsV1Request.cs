// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 更新云文档权限设置请求体（v1 历史版本接口）
/// <para>更新指定云文档的权限设置，包括是否允许内容被分享到组织外、谁可以查看、添加、移除协作者、谁可以复制内容等设置。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/permission/permission-public/patch"/></para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class UpdateDrivePermissionsV1Request
{
    /// <summary>
    /// <para>是否允许内容被分享到组织外。</para>
    /// <para>**注意**：当云文档类型为 wiki 即知识库节点时，不支持传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>可选值：<list type="bullet">
    /// <item>true：允许</item>
    /// <item>false：不允许</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("external_access")]
    public bool? ExternalAccess { get; set; }

    /// <summary>
    /// <para>谁可以创建副本、打印、下载。</para>
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
    /// <para>谁可以评论。</para>
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
    /// <para>谁可以添加和管理协作者。</para>
    /// <para>**注意**：当云文档类型为 wiki 即知识库节点时，不支持传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：anyone</para>
    /// <para>可选值：<list type="bullet">
    /// <item>anyone：所有可阅读或编辑此文档的用户</item>
    /// <item>same_tenant：组织内所有可阅读或编辑此文档的用户</item>
    /// <item>only_full_access：仅可管理协作者（包括我）</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("share_entity")]
    public string? ShareEntity { get; set; }

    /// <summary>
    /// <para>链接分享设置。</para>
    /// <para>**注意**：当云文档类型为 wiki 即知识库节点时，不支持传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：tenant_readable</para>
    /// <para>可选值：<list type="bullet">
    /// <item>tenant_readable：组织内获得链接的人可阅读</item>
    /// <item>tenant_editable：组织内获得链接的人可编辑</item>
    /// <item>anyone_readable：互联网上获得链接的任何人可阅读（仅 external_access 为 true 时有效）</item>
    /// <item>anyone_editable：互联网上获得链接的任何人可编辑（仅 external_access 为 true 时有效）</item>
    /// <item>closed：关闭链接分享</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("link_share_entity")]
    public string? LinkShareEntity { get; set; }

    /// <summary>
    /// <para>是否允许非「可管理权限」的人分享到组织外。</para>
    /// <para>**注意**：当云文档类型为 wiki 即知识库节点时，不支持传入该参数。</para>
    /// <para>必填：否</para>
    /// <para>示例值：true</para>
    /// <para>可选值：<list type="bullet">
    /// <item>true：允许</item>
    /// <item>false：不允许</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("invite_external")]
    public bool? InviteExternal { get; set; }
}
