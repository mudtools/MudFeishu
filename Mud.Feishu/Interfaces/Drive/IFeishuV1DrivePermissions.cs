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
    Task<FeishuApiResult<CreatePermissionMemberResult>?> CreatePermissionMemberAsync(
        [Path] string token,
        [Query("type")] string type,
        [Body] CreatePermissionMemberRequest createPermissionMemberRequest,
        [Query("need_notification")] bool? need_notification = false,
        CancellationToken cancellationToken = default);
}