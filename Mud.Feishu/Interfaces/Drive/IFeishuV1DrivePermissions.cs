// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Drive;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 权限是指在云文档相关资源中，应用或用户对各类云文档资源，如文件夹、文档、电子表格、多维表格、知识库等的可阅读、可编辑、可管理等权限。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/permission/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1DrivePermissions : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 增加协作者权限
    /// <para>为指定云文档添加协作者，协作者可以是用户、群组、部门、用户组等。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/permission/permission-member/create">接口文档</see></para>
    /// </summary>
    /// <param name="token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>云文档的 token，需要与 type 参数指定的云文档类型相匹配。</para>
    /// <para>示例值：doccnBKgoMyY5OMbUG6FioTXuBe</para>
    /// </param>
    /// <param name="type">
    /// <para>必填：是</para>
    /// <para>云文档类型，需要与云文档的 token 相匹配。</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。</item>
    /// <item>sheet：电子表格</item>
    /// <item>file：云空间文件</item>
    /// <item>wiki：知识库节点</item>
    /// <item>bitable：多维表格</item>
    /// <item>docx：新版文档</item>
    /// <item>folder：文件夹。使用 &lt;md-tag mode="inline" type="token-tenant"&gt;tenant_access_token&lt;/md-tag&gt; 调用时，需确保文件夹所有者为应用或应用拥有文件夹的可管理权限，你需要将应用作为群机器人添加至群内，然后授予该群组可管理权限。</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>minutes：妙记。目前妙记还不支持 full_access 权限角色</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="createPermissionMemberRequest">增加协作者权限请求体</param>
    /// <param name="need_notification">
    /// <para>添加权限后是否通知对方。</para>
    /// <para>可选值：</para>
    /// <para>- true：通知对方</para>
    /// <para>- false：不通知</para>
    /// <para>注意：</para>
    /// <para>仅当使用 &lt;md-tag mode="inline" type="token-user"&gt;user_access_token&lt;/md-tag&gt; 调用时，该参数有效。</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/permissions/{token}/members")]
    Task<FeishuApiResult<PermissionMemberOopsResult>?> CreatePermissionMemberAsync(
        [Path] string token,
        [Query("type")] string type,
        [Body] CreatePermissionMemberRequest createPermissionMemberRequest,
        [Query("need_notification")] bool? need_notification = false,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量增加协作者权限
    /// <para>为指定云文档批量添加多个协作者，协作者可以是用户、群组、部门、用户组等。</para>
    /// <para><see href="https://open.feishu.cn/document/docs/permission/permission-member/batch_create">接口文档</see></para>
    /// </summary>
    /// <param name="token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>云文档的 token，需要与 type 参数指定的云文档类型相匹配。</para>
    /// <para>示例值：doccnBKgoMyY5OMbUG6FioTXuBe</para>
    /// </param>
    /// <param name="type">
    /// <para>必填：是</para>
    /// <para>云文档类型，需要与云文档的 token 相匹配。</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。</item>
    /// <item>sheet：电子表格</item>
    /// <item>file：云空间文件</item>
    /// <item>wiki：知识库节点</item>
    /// <item>bitable：多维表格</item>
    /// <item>docx：新版文档</item>
    /// <item>folder：文件夹。使用 &lt;md-tag mode="inline" type="token-tenant"&gt;tenant_access_token&lt;/md-tag&gt; 调用时，需确保文件夹所有者为应用或应用拥有文件夹的可管理权限，你需要将应用作为群机器人添加至群内，然后授予该群组可管理权限。</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>minutes：妙记。目前妙记还不支持 full_access 权限角色</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="batchCreatePermissionMemberRequest">批量增加协作者权限请求体</param>
    /// <param name="need_notification">
    /// <para>添加权限后是否通知对方。</para>
    /// <para>可选值：</para>
    /// <para>- true：通知对方</para>
    /// <para>- false：不通知</para>
    /// <para>注意：</para>
    /// <para>仅当使用 &lt;md-tag mode="inline" type="token-user"&gt;user_access_token&lt;/md-tag&gt; 调用时，该参数有效。</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/permissions/{token}/members/batch_create")]
    Task<FeishuApiResult<BatchCreatePermissionMemberResult>?> BatchCreatePermissionMemberAsync(
       [Path] string token,
       [Query("type")] string type,
       [Body] BatchCreatePermissionMemberRequest batchCreatePermissionMemberRequest,
       [Query("need_notification")] bool? need_notification = false,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加协作者权限
    /// <para>为指定云文档添加协作者，协作者可以是用户、群组、部门、用户组等。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/permission/permission-member/update">接口文档</see></para>
    /// </summary>
    /// <param name="token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>云文档的 token，需要与 type 参数指定的云文档类型相匹配。</para>
    /// <para>示例值：doccnBKgoMyY5OMbUG6FioTXuBe</para>
    /// </param>
    /// <param name="type">
    /// <para>必填：是</para>
    /// <para>云文档类型，需要与云文档的 token 相匹配。</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。</item>
    /// <item>sheet：电子表格</item>
    /// <item>file：云空间文件</item>
    /// <item>wiki：知识库节点</item>
    /// <item>bitable：多维表格</item>
    /// <item>docx：新版文档</item>
    /// <item>folder：文件夹。使用 &lt;md-tag mode="inline" type="token-tenant"&gt;tenant_access_token&lt;/md-tag&gt; 调用时，需确保文件夹所有者为应用或应用拥有文件夹的可管理权限，你需要将应用作为群机器人添加至群内，然后授予该群组可管理权限。</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>minutes：妙记。目前妙记还不支持 full_access 权限角色</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="member_id">
    /// <para>路径参数</para>
    /// <para>协作者 ID，该 ID 的类型与 member_type 指定的值需要保持一致。</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</para>
    /// </param>
    /// <param name="updatePermissionMemberRequest">更新协作者权限成员请求模型</param>
    /// <param name="need_notification">
    /// <para>添加权限后是否通知对方。</para>
    /// <para>可选值：</para>
    /// <para>- true：通知对方</para>
    /// <para>- false：不通知</para>
    /// <para>注意：</para>
    /// <para>仅当使用 &lt;md-tag mode="inline" type="token-user"&gt;user_access_token&lt;/md-tag&gt; 调用时，该参数有效。</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("/open-apis/drive/v1/permissions/{token}/members/{member_id}")]
    Task<FeishuApiResult<PermissionMemberOopsResult>?> UpdatePermissionMemberAsync(
      [Path] string token,
      [Path] string member_id,
      [Query("type")] string type,
      [Body] UpdatePermissionMemberRequest updatePermissionMemberRequest,
      [Query("need_notification")] bool? need_notification = false,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取云文档协作者
    /// <para>获取指定云文档的协作者，支持查询人、群、组织架构、用户组、知识库成员五种类型的协作者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/permission/permission-member/list">接口文档</see></para>
    /// </summary>
    /// <param name="token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>云文档的 token，需要与 type 参数指定的云文档类型相匹配。</para>
    /// <para>示例值：doccnBKgoMyY5OMbUG6FioTXuBe</para>
    /// </param>
    /// <param name="type">
    /// <para>必填：是</para>
    /// <para>云文档类型，需要与云文档的 token 相匹配。</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。</item>
    /// <item>sheet：电子表格</item>
    /// <item>file：云空间文件</item>
    /// <item>wiki：知识库节点</item>
    /// <item>bitable：多维表格</item>
    /// <item>docx：新版文档</item>
    /// <item>folder：文件夹。使用 &lt;md-tag mode="inline" type="token-tenant"&gt;tenant_access_token&lt;/md-tag&gt; 调用时，需确保文件夹所有者为应用或应用拥有文件夹的可管理权限，你需要将应用作为群机器人添加至群内，然后授予该群组可管理权限。</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>minutes：妙记。目前妙记还不支持 full_access 权限角色</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>   
    /// <param name="fields">
    /// <para>必填：否</para>
    /// <para>指定返回的协作者字段信息，如无指定则默认不返回。</para>
    /// <para>**可选值有：**</para>
    /// <para>- `name`：协作者名</para>
    /// <para>- `type`：协作者类型</para>
    /// <para>- `avatar`：头像</para>
    /// <para>- `external_label`：外部标签</para>
    /// <para>**注意**：</para>
    /// <para>- 你可以使用特殊值`*`指定返回目前支持的所有字段</para>
    /// <para>- 你可以使用`,`分隔若干个你想指定返回的字段，如：`name,avatar`</para>
    /// <para>- 按需指定返回字段接口性能更好</para>
    /// <para>示例值：*</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="perm_type">
    /// <para>必填：否</para>
    /// <para>协作者的权限角色类型。当云文档类型为 wiki 即知识库节点时，该参数有效。</para>
    /// <para>**默认值**：container</para>
    /// <para>示例值：container</para>
    /// <list type="bullet">
    /// <item>container：当前页面及子页面</item>
    /// <item>single_page：仅当前页面，当且仅当在知识库文档中该参数有效</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/drive/v1/permissions/{token}/members")]
    Task<FeishuApiResult<GetPermissionMemberResult>?> GetPermissionMemberAsync(
       [Path] string token,
       [Query("type")] string type,
       [Query("fields")] string? fields = null,
       [Query("perm_type")] string? perm_type = null,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 移除云文档协作者权限
    /// <para>通过云文档 token 和协作者 ID 移除指定云文档协作者的权限。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/permission/permission-member/delete">接口文档</see></para>
    /// </summary>
    /// <param name="token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>云文档的 token，需要与 type 参数指定的云文档类型相匹配。</para>
    /// <para>示例值：doccnBKgoMyY5OMbUG6FioTXuBe</para>
    /// </param>
    /// <param name="type">
    /// <para>必填：是</para>
    /// <para>云文档类型，需要与云文档的 token 相匹配。</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。</item>
    /// <item>sheet：电子表格</item>
    /// <item>file：云空间文件</item>
    /// <item>wiki：知识库节点</item>
    /// <item>bitable：多维表格</item>
    /// <item>docx：新版文档</item>
    /// <item>folder：文件夹。使用 &lt;md-tag mode="inline" type="token-tenant"&gt;tenant_access_token&lt;/md-tag&gt; 调用时，需确保文件夹所有者为应用或应用拥有文件夹的可管理权限，你需要将应用作为群机器人添加至群内，然后授予该群组可管理权限。</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>minutes：妙记。目前妙记还不支持 full_access 权限角色</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="member_id">
    /// <para>路径参数</para>
    /// <para>协作者 ID，该 ID 的类型与 member_type 指定的值需要保持一致。</para>
    /// <para>示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</para>
    /// </param>
    /// <param name="deletePermissionMemberRequest">删除云文档协作者请求体</param>
    /// <param name="member_type">
    /// <para>必填：是</para>
    /// <para>协作者 ID 类型，与协作者 ID （member_id）需要对应。</para>
    /// <para>示例值：openid</para>
    /// <list type="bullet">
    /// <item>email：邮箱地址</item>
    /// <item>openid：开放平台 Open ID - 获取应用 OpenID，参考[如何获取应用 open_id](https://open.feishu.cn/document/ukTMukTMukTM/uczNzUjL3czM14yN3MTN#6dbaa8df) - 获取用户 OpenID，参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>openchat：开放平台群组 ID。获取方式参考[群 ID 说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/im-v1/chat-id-description)</item>
    /// <item>opendepartmentid：开放平台部门 ID。仅当使用 &lt;md-tag mode="inline" type="token-user"&gt;user_access_token&lt;/md-tag&gt; 调用时有效。获取方式参考[部门资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/department/field-overview)</item>
    /// <item>userid：用户 ID。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>unionid：开放平台 Union ID。获取方式参考[如何获取不同的用户 ID](https://open.feishu.cn/document/home/user-identity-introduction/open-id)</item>
    /// <item>groupid：自定义用户组 ID。获取方式参考[用户组资源介绍](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/contact-v3/group/overview)</item>
    /// <item>wikispaceid：知识空间 ID。仅知识库文档支持该参数，当需要操作知识库文档里的「知识库成员」类型协作者时传该参数。获取方式参考[知识库概述](https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/wiki-overview)</item>
    /// </list>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/drive/v1/permissions/{token}/members/{member_id}")]
    Task<FeishuNullDataApiResult?> DeletePermissionMemberAsync(
        [Path] string token,
        [Path] string member_id,
        [Query("type")] string type,
        [Body] DeletePermissionMemberRequest deletePermissionMemberRequest,
        [Query("member_type")] string member_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 转移云文档所有者
    /// <para>转移指定云文档的所有者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/permission/permission-member/transfer_owner">接口文档</see></para>
    /// </summary>
    /// <param name="token">
    /// <para>路径参数</para>
    /// <para>必填：是</para>
    /// <para>云文档的 token，需要与 type 参数指定的云文档类型相匹配。</para>
    /// <para>示例值：doccnBKgoMyY5OMbUG6FioTXuBe</para>
    /// </param>
    /// <param name="type">
    /// <para>必填：是</para>
    /// <para>云文档类型，需要与云文档的 token 相匹配。</para>
    /// <para>示例值：docx</para>
    /// <list type="bullet">
    /// <item>doc：旧版文档。</item>
    /// <item>sheet：电子表格</item>
    /// <item>file：云空间文件</item>
    /// <item>wiki：知识库节点</item>
    /// <item>bitable：多维表格</item>
    /// <item>docx：新版文档</item>
    /// <item>folder：文件夹。使用 &lt;md-tag mode="inline" type="token-tenant"&gt;tenant_access_token&lt;/md-tag&gt; 调用时，需确保文件夹所有者为应用或应用拥有文件夹的可管理权限，你需要将应用作为群机器人添加至群内，然后授予该群组可管理权限。</item>
    /// <item>mindnote：思维笔记</item>
    /// <item>minutes：妙记。目前妙记还不支持 full_access 权限角色</item>
    /// <item>slides：幻灯片</item>
    /// </list>
    /// </param>
    /// <param name="need_notification">
    /// <para>添加权限后是否通知对方。</para>
    /// <para>可选值：</para>
    /// <para>- true：通知对方</para>
    /// <para>- false：不通知</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="remove_old_owner">
    /// <para>必填：否</para>
    /// <para>转移后是否需要移除原云文档所有者的权限。可选值：</para>
    /// <para>- `true`：移除原所有者权限</para>
    /// <para>- `false`：不移除原所有者权限</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="stay_put">
    /// <para>必填：否</para>
    /// <para>在个人文件夹下的云文档是否仍留在原所有者个人文件夹下。可选值：</para>
    /// <para>- `true`：云文档留在原位置不变</para>
    /// <para>- `false`：系统会将该内容移至新所有者的空间下</para>
    /// <para>**注意**：仅当云文档在个人文件夹下时参数生效。</para>
    /// <para>示例值：false</para>
    /// <para>默认值：false</para>
    /// </param>
    /// <param name="old_owner_perm">
    /// <para>必填：否</para>
    /// <para>为原云文档所有者保留的具体权限。可选值：</para>
    /// <para>- `view`：可阅读角色</para>
    /// <para>- `edit`：可编辑角色</para>
    /// <para>- `full_access`：可管理角色</para>
    /// <para>**注意**：仅当 `remove_old_owner` 为 `false` 时，此参数才会生效。</para>
    /// <para>示例值：view</para>
    /// <para>默认值：full_access</para>
    /// </param>
    /// <param name="transferOwnerPermissionMemberRequest">转移云文档所有者请求体</param>    
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/drive/v1/permissions/{token}/members/transfer_owner")]
    Task<FeishuNullDataApiResult?> TransferOwnerPermissionMemberAsync(
       [Path] string token,
       [Query("type")] string type,
       [Body] TransferOwnerPermissionMemberRequest transferOwnerPermissionMemberRequest,
       [Query("need_notification")] bool? need_notification = false,
       [Query("remove_old_owner")] bool? remove_old_owner = false,
       [Query("stay_put")] bool? stay_put = false,
       [Query("old_owner_perm")] string? old_owner_perm = "full_access",
       CancellationToken cancellationToken = default);
}