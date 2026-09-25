// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书妙搭（Spark）应用 SDK 是一组服务端 OpenAPI 的封装，用于批量查询妙搭应用、查询应用 AI 额度消耗与运营数据。本接口仅声明支持 tenant_access_token 与 user_access_token 双令牌调用的只读端点；创建、更新、图标上传、HTML 发布与可用范围等 user-only 写端点见 <see cref="IFeishuUserV1SparkApp"/>。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1SparkApp : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 批量获取妙搭应用
    /// <para>查询当前令牌有权限查看的妙搭应用列表，支持按类型、关键词、归属范围筛选与分页。</para>
    /// <para>查询参数采用查询对象模式（<see cref="GetAppListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、应用类型、关键词与归属范围查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回妙搭应用分页列表（items 为应用详细信息数组）</returns>
    [Get("/open-apis/spark/v1/apps")]
    Task<FeishuApiResult<GetAppListResult>?> GetAppListAsync(
        [Query] GetAppListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取妙搭应用消耗 AI 额度
    /// <para>查询指定应用在时间区间内按天的 AI 额度消耗数据点与汇总。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/open_api_credit_usage">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 ID，示例值：app_4jbp6bx8fwjgm</param>
    /// <param name="start_time">时间范围起始时间戳，单位：秒（Unix 时间戳），示例值：1717286400</param>
    /// <param name="end_time">时间范围结束时间戳，单位：秒（Unix 时间戳）。须满足 end_time &gt;= start_time，示例值：1717372800</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回总额度消耗、按天数据点列表与企业/个人额度汇总</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/credit_usage")]
    Task<FeishuApiResult<GetCreditUsageResult>?> GetAppCreditUsageAsync(
        [Path] string app_id,
        [Query("start_time")] string start_time,
        [Query("end_time")] string end_time,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取妙搭应用运营数据总览
    /// <para>查询指定应用在时间区间内的活跃用户、新增用户与页面访问数总览，含上一等长区间环比。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/open_api_analytics_overview">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 ID，示例值：app_4jbp6bx8fwjgm</param>
    /// <param name="start_time">运营分析区间起始时间戳，单位：秒（Unix 时间戳），示例值：1690000000</param>
    /// <param name="end_time">运营分析区间结束时间戳，单位：秒（Unix 时间戳）。须满足 end_time &gt;= start_time，示例值：1690086400</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 active_users、signups、page_views 三项指标对象（值、环比基准、环比变化）</returns>
    [Get("/open-apis/spark/v1/apps/{app_id}/analytics/overview")]
    Task<FeishuApiResult<GetAnalyticsOverviewResult>?> GetAppAnalyticsOverviewAsync(
        [Path] string app_id,
        [Query("start_time")] string start_time,
        [Query("end_time")] string end_time,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取妙搭应用运营数据趋势
    /// <para>按指标、时间聚合单元与过滤条件查询应用运营数据趋势序列。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/spark-v1/app/query_analytics_data">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">妙搭应用 ID，长度 1～1000 字符，示例值：app_4jbp6bx8fwjgm</param>
    /// <param name="request">查询请求体（metric_types、start_timestamp_ns、end_timestamp_ns、time_aggregation_unit 必填；可选 filter、page、device_types、need_pack_lack_point、group_by）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回按指标划分的趋势序列（series：metric_type + points 数据点数组）</returns>
    [Post("/open-apis/spark/v1/apps/{app_id}/query_analytics_data")]
    Task<FeishuApiResult<QueryAnalyticsDataResult>?> QueryAppAnalyticsDataAsync(
        [Path] string app_id,
        [Body] QueryAnalyticsDataRequest request,
        CancellationToken cancellationToken = default);
}
