// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Performance;

namespace Mud.Feishu;


/// <summary>
/// 飞书绩效（Performance）「周期任务」SDK 是一组服务端 OpenAPI 的封装，用于按指定用户或全量分页方式获取周期内各用户的环节任务信息（任务分类、截止时间、环节状态等）。本接口全部端点为 performance/v1，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/performance-v1/stage_task/find_by_user_list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Performance")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1PerformanceStageTask : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取周期任务（指定用户）
    /// <para>根据用户 ID 批量获取指定周期的任务信息，支持传入任务分类、任务截止时间参数筛选周期内任务数据（该接口无分页）。</para>
    /// <para>限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/stage_task/find_by_user_list">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 周期 ID 必填；user_id_lists 用户 ID 列表必填，最大 50 个；task_option_lists 任务分类 1 待完成/2 已完成/3 已逾期，最大 3 个；after_time/before_time 截止时间范围，毫秒时间戳）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期基本信息（base：semester_id/semester_name/start_time/end_time）与各用户周期任务（items：user_id/stage_num_lists/stage_task_info_lists），无分页字段</returns>
    [Post("/open-apis/performance/v1/stage_tasks/find_by_user_list")]
    Task<FeishuApiResult<FindStageTaskByUserListResult>?> FindStageTaskByUserListAsync(
        [Body] FindStageTaskByUserListRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取周期任务（全部用户）
    /// <para>批量获取周期下所有用户的任务信息，支持传入任务分类、任务截止时间参数筛选周期内任务数据。</para>
    /// <para>限频：20 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/stage_task/find_by_page">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 周期 ID 必填；task_option_lists 任务分类 1 待完成/2 已完成/3 已逾期，最大 3 个；after_time/before_time 截止时间范围，毫秒时间戳；page_token 分页标记；page_size 分页大小，默认 20，最大 50）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期基本信息（base）、周期任务列表（items）、has_more 与 page_token</returns>
    [Post("/open-apis/performance/v1/stage_tasks/find_by_page")]
    Task<FeishuApiResult<FindStageTaskByPageResult>?> FindStageTaskByPageAsync(
        [Body] FindStageTaskByPageRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
