// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.VideoConferencing;

namespace Mud.Feishu;

/// <summary>
/// 会议报告用于记录一段时间内租户会议的使用情况，包括：获取会议报告、获取 Top 用户列表。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/vc-v1/report/meeting-report-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "VideoConferencing")]
[Token("TenantAccessToken", Name = Consts.Authorization)]
public interface IFeishuTenantV1VideoConferencingReport : IFeishuAppContextSwitcher
{

    /// <summary>
    /// 获取会议报告
    /// <para>获取一段时间内组织的每日会议使用报告。支持最近90天内的数据查询</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/report/get_daily">接口文档</see></para>
    /// </summary>
    /// <param name="start_time">
    /// <para>开始时间（unix时间，单位sec）</para>
    /// <para>示例值：1608888867</para>
    /// </param>
    /// <param name="end_time">
    /// <para>结束时间（unix时间，单位sec）</para>
    /// <para>示例值：1608888966</para>
    /// </param>
    /// <param name="unit">
    /// <para>数据驻留地（传参前提是租户存在多个驻留地数据且开通了该查询功能）</para>
    /// <para>示例值：0</para>
    /// <list type="bullet">
    /// <item>0：中国大陆</item>
    /// <item>1：美国</item>
    /// <item>2：新加坡</item>
    /// <item>3：日本</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/reports/get_daily")]
    Task<FeishuApiResult<GetDailyReportResult>?> GetDailyReportAsync(
       [Query] string start_time,
       [Query] string end_time,
       [Query] int? unit = null,
       CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Top 用户列表
    /// <para>获取一段时间内组织内会议使用的 Top 用户列表。支持最近90天内的数据查询；默认返回前10位，最多可查询前100位</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/vc-v1/report/get_top_user">接口文档</see></para>
    /// </summary>
    /// <param name="start_time">
    /// <para>开始时间（unix时间，单位sec）</para>
    /// <para>示例值：1608888867</para>
    /// </param>
    /// <param name="end_time">
    /// <para>结束时间（unix时间，单位sec）</para>
    /// <para>示例值：1608888966</para>
    /// </param>
    /// <param name="unit">
    /// <para>数据驻留地（传参前提是租户存在多个驻留地数据且开通了该查询功能）</para>
    /// <para>示例值：0</para>
    /// <list type="bullet">
    /// <item>0：中国大陆</item>
    /// <item>1：美国</item>
    /// <item>2：新加坡</item>
    /// <item>3：日本</item>
    /// </list>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="limit">
    /// <para>取前多少位</para>
    /// <para>示例值：10</para>
    /// </param>
    /// <param name="order_by">
    /// <para>排序依据（降序）</para>
    /// <para>示例值：1</para>
    /// <list type="bullet">
    /// <item>1：会议数量</item>
    /// <item>2：会议时长</item>
    /// </list>
    /// </param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/vc/v1/reports/get_top_user")]
    Task<FeishuApiResult<GetTopUserReportResult>?> GetTopUserReportAsync(
        [Query] string start_time,
        [Query] string end_time,
        [Query] int limit,
        [Query] int order_by,
        [Query] int? unit = null,
        [Query] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}