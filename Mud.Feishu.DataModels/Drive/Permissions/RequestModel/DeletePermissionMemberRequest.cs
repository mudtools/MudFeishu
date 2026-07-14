// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 删除云文档协作者请求体
/// <para>通过云文档 token 和协作者 ID 移除指定云文档协作者的权限。</para>
/// </summary>
public class DeletePermissionMemberRequest
{
    /// <summary>
    /// <para>协作者类型</para>
    /// <para>**注意**：当 `member_type` 参数为 `wikispaceid` 时必须传该参数</para>
    /// <para>**默认值**：""</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// <item>chat：群组</item>
    /// <item>department：组织架构</item>
    /// <item>group：用户组</item>
    /// <item>wiki_space_member：知识库成员。在知识库启用了成员分组功能后不支持该参数</item>
    /// <item>wiki_space_viewer：知识库可阅读成员。仅在知识库启用了成员分组功能后才支持该参数</item>
    /// <item>wiki_space_editor：知识库可编辑成员。仅在知识库启用了成员分组功能后才支持该参数</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// <para>协作者的权限角色类型。当云文档类型为 wiki 即知识库节点时，该参数有效。</para>
    /// <para>必填：否</para>
    /// <para>示例值：container</para>
    /// <para>可选值：<list type="bullet">
    /// <item>container：当前页面及子页面</item>
    /// <item>single_page：仅当前页面，当且仅当在知识库文档中该参数有效</item>
    /// </list></para>
    /// <para>默认值：container</para>
    /// </summary>
    [JsonPropertyName("perm_type")]
    public string? PermType { get; set; }
}