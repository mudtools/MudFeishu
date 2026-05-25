// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书标签API接口实现了修改、查询、删除等邮件标签功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-thread/batch_trash"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1MailLabel : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 更新标签。
    /// <para>更新用户指定标签的名字、颜色等信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/patch">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="label_id">
    /// <para>标签ID，创建标签成功后返回的标签ID，或可通过列出标签、获取邮件详情等接口获得</para>
    /// <para>示例值：7620003644728938013</para>
    /// </param>
    /// <param name="updateUserMailboxLabelRequest">更新用户邮箱标签请求对象，包含待更新的标签信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/labels/{label_id}")]
    Task<FeishuApiResult<UserMailboxLabelOopsResult>?> UpdateUserMailboxLabelAsync(
      [Path] string user_mailbox_id,
      [Path] string label_id,
      [Body] UpdateUserMailboxLabelRequest updateUserMailboxLabelRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 列出标签。
    /// <para>列出邮件标签，包括ID、名称、颜色、未读信息等内容。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/labels")]
    Task<FeishuApiResult<GetUserMailboxLabelListResult>?> GetUserMailboxLabelListAsync(
       [Path] string user_mailbox_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取标签信息。
    /// <para>根据指定ID，获取邮件标签信息，包括名称、未读数据、颜色等信息。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="label_id">
    /// <para>标签ID，创建标签成功后返回的标签ID，或可通过列出标签、获取邮件详情等接口获得</para>
    /// <para>示例值：7620003644728938013</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/labels/{label_id}")]
    Task<FeishuApiResult<GetUserMailboxLabelResult>?> GetUserMailboxLabelAsync(
      [Path] string user_mailbox_id,
      [Path] string label_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除标签。
    /// <para>删除用户指定的标签，注意，删除的标签无法恢复。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/delete">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="label_id">
    /// <para>标签ID，创建标签成功后返回的标签ID，或可通过列出标签、获取邮件详情等接口获得</para>
    /// <para>示例值：7620003644728938013</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/labels/{label_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxLabelAsync(
       [Path] string user_mailbox_id,
       [Path] string label_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建标签。
    /// <para>根据用户指定的名称、颜色等信息，创建邮件标签。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-label/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="createUserMailboxLabelRequest">创建用户邮箱标签请求对象，包含待创建的标签信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/labels")]
    Task<FeishuApiResult<UserMailboxLabelOopsResult>?> CreateUserMailboxLabelAsync(
      [Path] string user_mailbox_id,
      [Body] CreateUserMailboxLabelRequest createUserMailboxLabelRequest,
      CancellationToken cancellationToken = default);
}
