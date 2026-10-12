// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用商店付费信息接口，用于查询用户是否在应用付费功能开通范围内、查询应用租户下的付费订单，以及查询订单详情。
/// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/appstore-paid-info/query-a-user%27s-app-access">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationAppStore : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询用户是否在应用付费功能开通范围。
    /// <para>当付费套餐按人数收费或限制最大使用人数时，可在付费功能入口判断是否允许某个用户进入使用。open_id 与 user_id 必须包含其一，同时传入时取 open_id。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/appstore-paid-info/query-a-user%27s-app-access">接口文档</see></para>
    /// </summary>
    /// <param name="open_id">用户 open_id，与 user_id 必须包含其一，同时传入时取 open_id</param>
    /// <param name="user_id">用户 user_id，与 open_id 必须包含其一</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/pay/v1/paid_scope/check_user")]
    Task<FeishuApiResult<CheckUserPaidScopeResult>?> CheckUserPaidScopeAsync(
      [Query] string? open_id = null,
      [Query] string? user_id = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询应用租户下的付费订单。
    /// <para>分页查询应用租户下的已付费订单，每次购买对应一个唯一的订单。免费模式的应用不会产生订单，仅收费应用会产生订单（含免费版）。业务方需自行处理套餐的有效期和付费方案的升级。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/appstore-paid-info/query-an-app-tenant%27s-paid-orders">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页显示的订单数量</param>
    /// <param name="status">订单状态过滤。normal：正常状态；refunded：已退款；为空或 all 表示所有。未支付的订单无法查到</param>
    /// <param name="page_token">翻页标识，可从上次请求的响应中获取，不填或为空时表示从开头获取</param>
    /// <param name="tenant_key">购买应用的租户唯一标识，为空表示获取应用下所有订单</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/pay/v1/order/list")]
    Task<FeishuApiResult<GetPaidOrdersResult>?> GetPaidOrdersPageAsync(
      [Query] int page_size,
      [Query] string? status = null,
      [Query] string? page_token = null,
      [Query] string? tenant_key = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询订单详情。
    /// <para>查询单个订单的详细信息。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/appstore-paid-info/query-order-information">接口文档</see></para>
    /// </summary>
    /// <param name="order_id">
    /// <para>订单 ID</para>
    /// <para>示例值：6708978506916697671</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/pay/v1/order/get")]
    Task<FeishuApiResult<GetOrderResult>?> GetOrderAsync(
      [Query] string order_id,
      CancellationToken cancellationToken = default);
}
