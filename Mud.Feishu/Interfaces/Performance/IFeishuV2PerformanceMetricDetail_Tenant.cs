// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Performance;

namespace Mud.Feishu;


/// <summary>
/// 飞书绩效（Performance）「关键指标数据」SDK 是一组服务端 OpenAPI 的封装，用于批量获取指定周期中被评估人的关键指标结果，以及批量录入被评估人的关键指标数据。本接口全部端点为 performance/v2，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/performance-v1/metric_detail/query"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Performance")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV2PerformanceMetricDetail : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取被评估人关键指标结果
    /// <para>批量获取指定周期中被评估人的关键指标结果，1 次只允许查询 1 个周期。</para>
    /// <para>限频：20 次/分钟。所需权限（开启任一即可）：performance:metric:write（管理关键指标数据）、performance:metric:read（获取关键指标数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/metric_detail/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 周期 ID 必填，1 次只允许查询 1 个周期；reviewee_user_ids 被评估人 ID 列表必填，1~50 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期 ID 与指标明细列表（reviewee_metrics：reviewee_user_id/metric_template_id/metric_details/reviewee_stage_statuses）</returns>
    [Post("/open-apis/performance/v2/metric_details/query")]
    Task<FeishuApiResult<QueryMetricDetailListResult>?> QueryMetricDetailListAsync(
        [Body] QueryMetricDetailListRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 录入被评估人关键指标数据
    /// <para>批量录入指定周期中被评估人的关键指标数据；同一个被评估人不允许传入重复的指标，指标的一个字段只允许传入唯一的字段值。</para>
    /// <para>限频：10 次/分钟。所需权限：performance:metric:write（管理关键指标数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。client_token 用于幂等去重，重复提交将被拦截（错误码 1580110）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/metric_detail/import">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 周期 ID 必填；import_record_name 录入记录名称，默认「API录入」；imported_metrics 指标明细列表必填，1~50 条）</param>
    /// <param name="client_token">幂等请求标识，长度 0~64 字符（必填），示例值：12454646</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回导入记录 ID（import_record_id）</returns>
    [Post("/open-apis/performance/v2/metric_details/import")]
    Task<FeishuApiResult<ImportMetricDetailResult>?> ImportMetricDetailAsync(
        [Body] ImportMetricDetailRequest request,
        [Query("client_token")] string client_token,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
