// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Mail;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书邮箱收信规则API接口实现了修改、查询、删除等邮箱收信规则管理功能。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/mail-v1/user_mailbox-rule/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1MailRule : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 创建收信规则。
    /// <para>创建收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-rule/create">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="request">创建用户邮箱收信规则请求对象，包含待创建的收信规则信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules")]
    Task<FeishuApiResult<CreateUserMailboxRuleResult>?> CreateUserMailboxRuleAsync(
      [Path] string user_mailbox_id,
      [Body] CreateUserMailboxRuleRequest request,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除收信规则。
    /// <para>删除收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-rule/delete">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="rule_id">
    /// <para>规则 id，获取方式见 [列出收信规则](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-rule/list)</para>
    /// <para>示例值：123123123</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules/{rule_id}")]
    Task<FeishuNullDataApiResult?> DeleteUserMailboxRuleAsync(
        [Path] string user_mailbox_id,
        [Path] string rule_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新收信规则。
    /// <para>更新收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-rule/update">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="rule_id">
    /// <para>规则 id，获取方式见 [列出收信规则](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/mail-v1/user_mailbox-rule/list)</para>
    /// <para>示例值：123123123</para>
    /// </param>
    /// <param name="request">更新用户邮箱收信规则请求对象，包含待更新的收信规则信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Put("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules/{rule_id}")]
    Task<FeishuNullDataApiResult?> UpdateUserMailboxRuleAsync(
          [Path] string user_mailbox_id,
          [Path] string rule_id,
          [Body] UpdateUserMailboxRuleRequest request,
          CancellationToken cancellationToken = default);


    /// <summary>
    /// 列出收信规则。
    /// <para>列出收信规则。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-rule/update">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules")]
    Task<FeishuApiResult<GetMailboxRuleListResult>?> GetMailboxRuleListAsync(
        [Path] string user_mailbox_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 对收信规则进行排序。
    /// <para>对收信规则进行排序。使用 tenant_access_token 时，需要申请收信规则资源的数据权限。</para>
    /// <para>当使用该接口时，需要传递所有规则 id</para>
    /// <para><see href="https://open.feishu.cn/document/mail-v1/user_mailbox-rule/reorder">接口文档</see></para>
    /// </summary>
    /// <param name="user_mailbox_id">
    /// <para>用户邮箱地址，作为用户邮箱身份标识。使用 user_access_token 调用时，可使用占位符 `me` 表示当前授权用户的主邮箱。</para>
    /// <para>示例值：user@example.com</para>
    /// </param>
    /// <param name="request">重新排序用户邮箱收信规则请求对象，包含新的收信规则顺序信息。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/mail/v1/user_mailboxes/{user_mailbox_id}/rules/reorder")]
    Task<FeishuNullDataApiResult?> ReorderUserMailboxRuleAsync(
       [Path] string user_mailbox_id,
       [Body] ReorderUserMailboxRuleRequest request,
       CancellationToken cancellationToken = default);
}
