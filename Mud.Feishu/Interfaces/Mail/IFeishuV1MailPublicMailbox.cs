// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书公共邮箱API接口实现公共邮箱管理、公共邮箱成员管理以及公共邮箱别名管理等管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-folder/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1MailPublicMailbox : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 分页查询所有公共邮箱。
    /// <para>分页批量获取公共邮箱列表。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/mail-v1/public-mailbox/public_mailbox/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/public_mailboxes")]
    Task<FeishuApiPageListResult<PublicMailboxInfo>?> GetPublicMailboxPageListAsync(
         [Query] int page_size = Consts.PageSize_20,
         [Query] string? page_token = null,
         CancellationToken cancellationToken = default);
}
