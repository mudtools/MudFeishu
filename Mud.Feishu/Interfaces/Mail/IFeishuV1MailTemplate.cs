// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书邮件模板API接口实现了更新、查询等邮件模板功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/download_url"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1MailTemplate : IFeishuAppContextSwitcher
{


    /// <summary>
    /// 获取模板附件下载链接。
    /// <para>获取指定邮件模板下的附件下载链接。用于在已知模板 ID 与附件 ID 的场景下，二次获取附件的有效访问 URL，便于在用户端预览或下载邮件模板中的附件资源。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/download_url">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="template_id">
    /// <para>邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。</para>
    /// <para>示例值：7281187859195772947</para>
    /// </param>
    /// <param name="attachment_ids">
    /// <para>待获取下载链接的附件 ID 列表。可通过获取个人邮件模板详情接口返回的 attachments 字段中的 id 获取。</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates/{template_id}/attachments/download_url")]
    Task<FeishuApiResult<GetAttachmentsDownloadUrlResult>?> GetAttachmentsDownloadUrlAsync(
        [Path] string user_mailbox_id,
        [Path] string template_id,
        [Query] string[] attachment_ids,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新邮件模板。
    /// <para>以全量替换的方式更新指定邮件模板的所有字段（包括名称、主题、正文、附件、收件信息等）。本接口为「全量更新」语义：请求时需传入完整的模板对象，未携带的字段将被清空。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/update">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="template_id">
    /// <para>邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。</para>
    /// <para>示例值：7281187859195772947</para>
    /// </param>
    /// <param name="updateMailTemplateRequest">更新邮件模板请求对象，包含待更新的邮件模板信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates/{template_id}")]
    Task<FeishuApiResult<UpdateMailTemplateResult>?> UpdateMailTemplateAsync(
        [Path] string user_mailbox_id,
        [Path] string template_id,
        [Body] UpdateMailTemplateRequest updateMailTemplateRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 列出邮件模板。
    /// <para>列出指定用户邮箱下的全部个人邮件模板基本信息（一次性返回，不分页），常用于在编辑或发送邮件场景下展示可选模板列表。如需获取模板正文与附件等完整字段，请通过获取个人邮件模板详情接口按 template_id 查询。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates")]
    Task<FeishuApiResult<GetMailTemplateListResult>?> GetMailTemplateListAsync(
        [Path] string user_mailbox_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取邮件模板。
    /// <para>获取指定邮件模板的完整详情，包括模板名称、主题、正文（HTML 或纯文本）、收件人/抄送/密送地址、附件信息等所有字段。常用于编辑模板前回填表单，或在发送邮件场景下读取模板内容做二次填充。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/get">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="template_id">
    /// <para>邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。</para>
    /// <para>示例值：7281187859195772947</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates/{template_id}")]
    Task<FeishuApiResult<GetMailTemplateResult>?> GetMailTemplateAsync(
       [Path] string user_mailbox_id,
       [Path] string template_id,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建邮件模板。
    /// <para>在指定用户邮箱下创建一份可复用的个人邮件模板。请求时需传入完整的模板对象（含名称、主题、正文、收件信息、附件等），创建成功后返回完整模板内容（含系统生成的 template_id），适用于将常用邮件内容沉淀为模板以便后续快速发送同类型邮件。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="createMailTemplateRequest">创建邮件模板请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates")]
    Task<FeishuApiResult<CreateMailTemplateResult>?> CreateMailTemplateAsync(
       [Path] string user_mailbox_id,
       [Body] CreateMailTemplateRequest createMailTemplateRequest,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除邮件模板。
    /// <para>永久删除指定用户邮箱下的某个个人邮件模板。删除操作不可恢复，删除后该模板将无法在「列出邮件模板」「获取邮件模板」等接口中再返回，常用于清理已废弃或不再使用的模板。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-template/delete">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="template_id">
    /// <para>邮件模板 ID。可通过列出个人邮件模板接口或创建个人邮件模板接口的返回值获取。</para>
    /// <para>示例值：7281187859195772947</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/templates/{template_id}")]
    Task<FeishuNullDataApiResult?> DeleteMailTemplateAsync(
      [Path] string user_mailbox_id,
      [Path] string template_id,
      CancellationToken cancellationToken = default);
}
