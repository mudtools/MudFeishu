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
}