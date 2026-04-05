// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// <para>仪表盘 block，仪表盘与数据看板类似，可以从不同的维度统计对多维表格中的数据进行统计。</para>
/// <para>仪表盘的唯一标识为 block_id，以 blk 开头，可通过多维表格 URL 获取 block_id。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-dashboard/copy"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuV1BitableDashboard : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 复制仪表盘
    /// <para>基于现有仪表盘复制出新的仪表盘。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-dashboard/copy">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="block_id">
    /// <para>多维表格仪表盘的唯一标识，以 blk 开头。获取方式：</para>
    /// <para>示例值：blkEsvEEaNllY2UV</para>
    /// </param>
    /// <param name="copyDashboardRequest">复制仪表盘请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/bitable/v1/apps/{app_token}/dashboards/{block_id}/copy")]
    Task<FeishuApiResult<CopyDashboardResult>?> CopyDashboardAsync(
      [Path] string app_token,
      [Path] string block_id,
      [Body] CopyDashboardRequest copyDashboardRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 复制仪表盘
    /// <para>基于现有仪表盘复制出新的仪表盘。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/docs/bitable-v1/app-dashboard/copy">接口文档</see></para>
    /// </summary>
    /// <param name="app_token">
    /// <para>多维表格 App 的唯一标识。不同形态的多维表格，其 app_token 的获取方式不同，参考[<see href="https://open.feishu.cn/document/ukTMukTMukTM/uUDN04SN0QjL1QDN/bitable-overview">多维表格 app_token 获取方式</see>]获取。</para>
    /// <para>示例值：AW3Qbtr2cakCnesXzXVbbsrIcVT</para>
    /// </param>
    /// <param name="page_size">分页大小，即本次请求所返回的信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/bitable/v1/apps/{app_token}/dashboards")]
    Task<FeishuApiPageListResult<AppDashboard>?> GetDashboardPageListAsync(
       [Path] string app_token,
       [Query("page_size")] int page_size = 20,
       [Query("page_token")] string? page_token = null,
       CancellationToken cancellationToken = default);
}
