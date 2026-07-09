// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书邮箱文件夹API接口实现了修改、查询、删除等邮箱文件夹管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1MailFolder : IFeishuAppContextSwitcher
{


    /// <summary>
    /// 获取邮箱文件信息。
    /// <para>通过指定文件夹ID，获取文件夹信息，包括名称、类型等。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="folder_id">
    /// <para>邮件文件夹唯一标识。可通过「获取邮箱文件夹列表」接口获取目标文件夹的 ID；若未传入该参数，默认返回根文件夹（收件箱）详情。</para>
    /// <para>示例值：7620095646711680541</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/folders/{folder_id}")]
    Task<FeishuApiResult<UserMailboxFoldeOopsResult>?> GetUserMailboxFoldeAsync(
      [Path] string user_mailbox_id,
      [Path] string folder_id,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建邮箱文件夹。
    /// <para>创建邮箱文件夹。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-folder/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="createUserMailboxFoldeRequest">创建用户邮箱文件夹请求对象，包含待创建的文件夹信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/folders")]
    Task<FeishuApiResult<UserMailboxFoldeOopsResult>?> CreateUserMailboxFoldeAsync(
        [Path] string user_mailbox_id,
        [Body] CreateUserMailboxFoldeRequest createUserMailboxFoldeRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除邮箱文件夹。
    /// <para>通过指定文件夹ID，删除对应的邮箱文件夹。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-folder/delete">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="folder_id">
    /// <para>邮件文件夹唯一标识。可通过「获取邮箱文件夹列表」接口获取目标文件夹的 ID；若未传入该参数，默认返回根文件夹（收件箱）详情。</para>
    /// <para>示例值：7620095646711680541</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/folders/{folder_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxFoldeAsync(
        [Path] string user_mailbox_id,
        [Path] string folder_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新邮箱文件夹。
    /// <para>通过指定文件夹ID，更新对应的邮箱文件夹。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-folder/patch">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="folder_id">
    /// <para>邮件文件夹唯一标识。可通过「获取邮箱文件夹列表」接口获取目标文件夹的 ID；若未传入该参数，默认返回根文件夹（收件箱）详情。</para>
    /// <para>示例值：7620095646711680541</para>
    /// </param>
    /// <param name="updateUserMailboxFoldeRequest">更新用户邮箱文件夹请求对象，包含待更新的文件夹信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/folders/{folder_id}")]
    Task<FeishuNullDataApiResult?> UpdateUserMailboxFoldeAsync(
       [Path] string user_mailbox_id,
       [Path] string folder_id,
       [Body] UpdateUserMailboxFoldeRequest updateUserMailboxFoldeRequest,
       CancellationToken cancellationToken = default);



    /// <summary>
    /// 列出邮箱文件夹。
    /// <para>列出用户文件夹，可获取文件夹名称、文件夹ID、文件夹下的未读邮件和未读会话数量。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-folder/patch">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="folder_type">
    /// <para>必填：否</para>
    /// <para>文件夹类型</para>
    /// <para>示例值：1</para>
    /// <list type="bullet">
    /// <item>1：系统文件夹</item>
    /// <item>2：用户文件夹</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/folders")]
    Task<FeishuApiResult<GetUserMailboxFoldeListResult>?> GetUserMailboxFoldeListAsync(
         [Path] string user_mailbox_id,
         [Query] int? folder_type = null,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 列出可访问的邮箱。
    /// <para>列出可访问的邮箱，包括拥有读信和发信权限的主账号、公共邮箱。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox/accessible_mailboxes">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/accessible_mailboxes")]
    Task<FeishuApiResult<GetAccessibleMailboxesUserMailboxResult>?> GetAccessibleMailboxesUserMailboxAsync(
        [Path] string user_mailbox_id,
        CancellationToken cancellationToken = default);
}
