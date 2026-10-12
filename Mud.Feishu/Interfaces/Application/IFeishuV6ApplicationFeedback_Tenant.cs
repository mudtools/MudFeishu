// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Application;

namespace Mud.Feishu;

/// <summary>
/// 飞书应用反馈接口，用于查询应用反馈数据以及更新反馈的处理状态。
/// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application-feedback/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Application")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV6ApplicationFeedback : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取应用反馈列表。
    /// <para>查询应用的反馈数据，仅可查询最近 180 天内的数据。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application-feedback/list">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>目标应用 App ID（本租户创建的所有应用）</para>
    /// <para>示例值：cli_9f115af860f7901b</para>
    /// </param>
    /// <param name="from_date">查询起始日期，格式 yyyy-mm-dd，不填默认为当前日期。仅可查询 180 天内的数据</param>
    /// <param name="to_date">查询截止日期，格式 yyyy-mm-dd，不填默认为当前日期</param>
    /// <param name="feedback_type">反馈类型筛选，不填表示查询全部类型。1：故障反馈；2：产品建议</param>
    /// <param name="status">反馈处理状态筛选，不填表示查询全部处理状态。0：未处理；1：已处理；2：处理中；3：已关闭</param>
    /// <param name="page_size">本次拉取的最大反馈条数，取值范围：1 ～ 100。默认值：100</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/application/v6/applications/{app_id}/feedbacks")]
    Task<FeishuApiResult<GetFeedbacksResult>?> GetFeedbacksPageAsync(
      [Path] string app_id,
      [Query] string? from_date = null,
      [Query] string? to_date = null,
      [Query] int? feedback_type = null,
      [Query] int? status = null,
      [Query] int? page_size = Consts.PageSize_100,
      [Query] string? page_token = null,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新应用反馈。
    /// <para>更新应用反馈数据的处理状态。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/application-v6/application-feedback/patch">接口文档</see></para>
    /// </summary>
    /// <param name="app_id">
    /// <para>目标应用 App ID</para>
    /// <para>示例值：cli_9f115af860f7901b</para>
    /// </param>
    /// <param name="feedback_id">
    /// <para>应用反馈记录 ID</para>
    /// <para>示例值：7057888018203574291</para>
    /// </param>
    /// <param name="status">反馈处理状态。0：未处理；1：已处理；2：处理中；3：已关闭</param>
    /// <param name="operator_id">反馈处理人的用户 ID，取值类型与 user_id_type 对应</param>
    /// <param name="user_id_type">用户 ID 类型，默认值：open_id。当值为 user_id 时，需要申请「获取用户 user ID」权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/application/v6/applications/{app_id}/feedbacks/{feedback_id}")]
    Task<FeishuNullDataApiResult?> UpdateFeedbackStatusAsync(
      [Path] string app_id,
      [Path] string feedback_id,
      [Query] int status,
      [Query] string operator_id,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
