// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）内推账户入口域 SDK 是一组服务端 OpenAPI 的封装，用于内推奖励账户的注册、启用/停用、余额查询、全额提现以及按时间段的提现数据对账。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/hire-v1/referral_account/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireReferralAccount : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 注册内推账户
    /// <para>通过内推人的手机号或邮箱注册「内推奖励账户」，返回账户 ID 与账户余额；mobile 与 email 二选一必传。</para>
    /// <para>限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/referral_account/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">注册请求体（mobile 含 code 国际区号与 number 手机号；email 邮箱；二者传其一）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回注册后的内推账户信息（account：account_id、assets、status）</returns>
    [Post("/open-apis/hire/v1/referral_account")]
    Task<FeishuApiResult<CreateReferralAccountResult>?> CreateReferralAccountAsync(
        [Body] CreateReferralAccountRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 启用内推账户
    /// <para>根据账户 ID 启用账户，启用后可通过「内推账户余额变更事件」监听余额变更、通过「全额提取内推账户余额」提取余额。</para>
    /// <para>限频：10 次/秒。所需权限：hire:referral_account（更新内推账号信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/referral_account/enable">接口文档</see></para>
    /// </summary>
    /// <param name="request">启用请求体（referral_account_id 选填：注册账户后获取的账户 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回启用后的内推账户信息（account：account_id、assets、status）</returns>
    [Post("/open-apis/hire/v1/referral_account/enable")]
    Task<FeishuApiResult<EnableReferralAccountResult>?> EnableReferralAccountAsync(
        [Body] EnableReferralAccountRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询内推账户
    /// <para>根据账户 ID 查询内推账户信息，返回账户余额、账户状态与账户绑定的内推人信息。</para>
    /// <para>限频：10 次/秒。所需权限：hire:referral_account:readonly（获取内推账户信息）或 hire:referral_account（更新内推账号信息）。字段权限：hire:employee.email:readonly（内推人邮箱）、hire:employee.mobile:readonly（内推人手机号）、contact:user.employee_id:readonly（取 user_id 时必填）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/referral_account/get_account_assets">接口文档</see></para>
    /// </summary>
    /// <param name="referral_account_id">账户 ID，注册账户后获取，示例值：6942778198054125570</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回内推账户信息（account：account_id、assets、status、referrer）</returns>
    [Get("/open-apis/hire/v1/referral_account/get_account_assets")]
    Task<FeishuApiResult<GetReferralAccountAssetsResult>?> GetReferralAccountAssetsAsync(
        [Query("referral_account_id")] string referral_account_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 停用内推账户
    /// <para>根据账户 ID 停用账户，停用后将不再发送「内推账户余额变更事件」，也无法通过「全额提取内推账户余额」提取余额。</para>
    /// <para>限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/referral_account/deactivate">接口文档</see></para>
    /// </summary>
    /// <param name="referral_account_id">账户 ID，示例值：6942778198054125570</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回停用后的内推账户信息（account：account_id、assets、status）</returns>
    [Post("/open-apis/hire/v1/referral_account/{referral_account_id}/deactivate")]
    Task<FeishuApiResult<DeactivateReferralAccountResult>?> DeactivateReferralAccountAsync(
        [Path] string referral_account_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 全额提取内推账户余额
    /// <para>通过账户 ID 全额提取内推账户下的积分/现金；提现后账户余额清零，对应奖励在招聘系统中标记为「已发放」。external_order_id 为幂等键，重复传入返回原单据的提取详情。</para>
    /// <para>限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/referral_account/withdraw">接口文档</see></para>
    /// </summary>
    /// <param name="referral_account_id">账户 ID，示例值：6942778198054125570</param>
    /// <param name="request">提现请求体（withdraw_bonus_type 必填：1 积分 / 2 现金；external_order_id 必填：请求方提供的唯一单据 ID，保证幂等）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回提现结果（external_order_id、trans_time、withdrawal_details）</returns>
    [Post("/open-apis/hire/v1/referral_account/{referral_account_id}/withdraw")]
    Task<FeishuApiResult<WithdrawReferralAccountResult>?> WithdrawReferralAccountAsync(
        [Path] string referral_account_id,
        [Body] WithdrawReferralAccountRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 内推账户提现数据对账
    /// <para>对一段时间内的内推账户积分提现数据进行对账，调用方需传入调用方系统的内推账户积分变动信息，返回核对失败的账户列表。</para>
    /// <para>限频：100 次/分钟。所需权限：hire:referral_account（更新内推账号信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/referral_account/reconciliation">接口文档</see></para>
    /// </summary>
    /// <param name="request">对账请求体（start_trans_time/end_trans_time 必填：对账时段的起止交易时间（毫秒时间戳）；trade_details 选填：账户积分变动信息列表）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回核对失败信息（check_failed_list：account_id、提取金额、充值金额）</returns>
    [Post("/open-apis/hire/v1/referral_account/reconciliation")]
    Task<FeishuApiResult<ReconciliationReferralAccountResult>?> ReconciliationReferralAccountAsync(
        [Body] ReconciliationReferralAccountRequest request,
        CancellationToken cancellationToken = default);
}
