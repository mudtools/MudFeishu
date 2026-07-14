// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// 协作者权限成员基础类
/// <para>协作者权限成员基础类，包括用户、群组、部门、用户组等。</para>
/// </summary>
public class BasePermissionMember
{
    /// <summary>
    /// <para>协作者 ID 类型，与协作者 ID 需要对应</para>
    /// <para>必填：是</para>
    /// <para>示例值：openid</para>
    /// <para>可选值：<list type="bullet">
    /// <item>email：飞书邮箱</item>
    /// <item>openid：开放平台 Open ID - 获取应用 OpenID，参考[如何获取应用 open_id](https://open.feishu.cn/document/ukTMukTMukTM/uczNzUjL3czM14yN3MTN#6dbaa8df) - 获取用户 OpenID，参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>unionid：开放平台 Union ID。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>openchat：开放平台群组 ID。获取方式参考[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)</item>
    /// <item>opendepartmentid：开放平台部门 ID。仅当使用 &lt;md-tag mode="inline" type="token-user"&gt;user_access_token&lt;/md-tag&gt; 调用时，该参数有效。获取方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// <item>userid：用户 ID。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>groupid：自定义用户组 ID。获取方式参考[用户组资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/group/overview)</item>
    /// <item>wikispaceid：知识空间 ID。仅知识库文档支持该参数，当需要操作知识库文档里的「知识库成员」类型协作者时传该参数。获取方式参考[知识库概述](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/wiki-overview)</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("member_type")]
    public string MemberType { get; set; } = string.Empty;


    /// <summary>
    /// <para>协作者对应的权限角色。</para>
    /// <para>必填：是</para>
    /// <para>示例值：view</para>
    /// <para>可选值：<list type="bullet">
    /// <item>view：可阅读角色</item>
    /// <item>edit：可编辑角色</item>
    /// <item>full_access：可管理角色。暂不支持妙记。</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("perm")]
    public string Perm { get; set; } = string.Empty;

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

    /// <summary>
    /// <para>协作者类型。</para>
    /// <para>**注意**：当 `member_type` 参数为 `wikispaceid` 时，该参数必填，且必须在 `wiki_space_member`、`wiki_space_viewer`、`wiki_space_editor` 中选择。</para>
    /// <para>**默认值**：""</para>
    /// <para>必填：否</para>
    /// <para>示例值：user</para>
    /// <para>可选值：<list type="bullet">
    /// <item>user：用户</item>
    /// <item>chat：群组</item>
    /// <item>department：组织架构</item>
    /// <item>group：用户组</item>
    /// <item>wiki_space_member：知识库成员。即知识库 **成员设置** 中的成员角色。若在知识库 **成员设置** 页面中，成员分为了 **可编辑成员** 和 **可阅读成员**，则不再支持该参数。你需选择下方参数</item>
    /// <item>wiki_space_viewer：知识库可阅读成员组。仅当知识库成员分为 **可编辑成员** 和 **可阅读成员** 时，支持该参数。</item>
    /// <item>wiki_space_editor：知识库可编辑成员组。仅当知识库成员分为 **可编辑成员** 和 **可阅读成员** 时，支持该参数</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }
}