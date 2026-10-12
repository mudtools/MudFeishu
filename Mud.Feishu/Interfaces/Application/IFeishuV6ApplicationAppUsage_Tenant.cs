// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用使用情况接口，用于查看应用在某一天/某一周/某一个月的使用数据与机器人消息推送数据，可按部门查看。仅支持企业版/旗舰版租户，一般每天早上 10 点产出前一天的数据。
/// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/app-usage/overview">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationAppUsage : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取应用使用概览。
    /// <para>查看应用在某一天/某一周/某一个月的使用数据，可查看租户整体或分部门的使用情况。指标包括：应用的活跃用户数、累计用户数、新增用户数、访问页面数、打开次数。调用频控 100 次/分。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/app-usage/overview">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </param>
    /// <param name="request">获取应用使用概览请求体</param>
    /// <param name="department_id_type">部门 ID 类型，默认值：open_department_id。可选值：open_department_id、department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v6/applications/{app_id}/app_usage/overview")]
    Task<FeishuApiResult<GetAppUsageOverviewResult>?> GetAppUsageOverviewAsync(
      [Path] string app_id,
      [Body] AppUsageOverviewRequest request,
      [Query] string? department_id_type = Consts.Department_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取消息推送概览。
    /// <para>查看应用在某一天/某一周/某一个月的机器人消息推送数据，可根据部门筛选。指标包括：消息推送给用户的次数、消息触达的人数、消息 1 小时阅读量、消息 12 小时阅读量。调用频控 100 次/分。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/app-usage/message_push_overview">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </param>
    /// <param name="request">获取消息推送概览请求体</param>
    /// <param name="department_id_type">部门 ID 类型，默认值：open_department_id。可选值：open_department_id、department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v6/applications/{app_id}/app_usage/message_push_overview")]
    Task<FeishuApiResult<GetMessagePushOverviewResult>?> GetMessagePushOverviewAsync(
      [Path] string app_id,
      [Body] MessagePushOverviewRequest request,
      [Query] string? department_id_type = Consts.Department_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取多部门应用使用概览。
    /// <para>查看应用在某一天/某一周/某一个月的使用数据，可按部门做多层子部门筛选，分别展示当前部门及其子部门的使用情况。调用频控 100 次/分。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/app-usage/department_overview">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>应用 App ID</para>
    /// <para>示例值：cli_9b445f5258795107</para>
    /// </param>
    /// <param name="request">获取多部门应用使用概览请求体</param>
    /// <param name="department_id_type">部门 ID 类型，默认值：open_department_id。可选值：open_department_id、department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/application/v6/applications/{app_id}/app_usage/department_overview")]
    Task<FeishuApiResult<GetDepartmentUsageOverviewResult>?> GetDepartmentUsageOverviewAsync(
      [Path] string app_id,
      [Body] DepartmentUsageOverviewRequest request,
      [Query] string? department_id_type = Consts.Department_Id_Type,
      CancellationToken cancellationToken = default);
}
