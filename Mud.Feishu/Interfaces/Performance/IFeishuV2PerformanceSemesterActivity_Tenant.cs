// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Performance;

namespace Mud.Feishu;


/// <summary>
/// 飞书绩效（Performance）后台配置「周期与项目」SDK 是一组服务端 OpenAPI 的封装，用于查询周期与项目配置、批量查询/导入/删除被评估人补充信息、更新人员组成员，以及查询被评估人与绩效周期人员快照信息。本接口全部端点仅支持 tenant_access_token 调用；除获取周期列表（performance/v1）外，其余端点为 performance/v2。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/semester/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Performance")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV2PerformanceSemesterActivity : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取周期列表
    /// <para>批量获取周期的基本信息（名称、类型、时间范围等），支持按时间段、周期年份、周期类型筛选；各查询参数之间为「与」关系，全部不传时返回所有周期。查询参数采用查询对象模式 <see cref="GetSemesterListQuery"/>，见 AGENTS.md API-2。</para>
    /// <para>限频：10 次/分钟。所需权限（开启任一即可）：performance:performance（管理绩效数据）、performance:performance:readonly（查看绩效数据）、performance:semester:read（查看周期数据）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/semester/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">周期起止时间、年份（0~9999）、类型分组、类型与用户 ID 类型等可选查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期列表（items：id/year/type_group/type/name/progress/start_time/end_time/create_time/modify_time/create_user_id/modify_user_id）</returns>
    [Get("/open-apis/performance/v1/semesters")]
    Task<FeishuApiResult<GetSemesterListResult>?> GetSemesterListAsync(
        [Query] GetSemesterListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取项目列表
    /// <para>批量获取项目的配置信息（项目名称、项目模式、项目状态等）。semester_ids 与 activity_ids 均未填写时返回空数据；填写 activity_ids 时 semester_ids 无效。</para>
    /// <para>限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:semester_activity:read（获取周期与项目配置信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/activity/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_ids 评估周期 ID 列表 0~10 个；activity_ids 项目 ID 列表 0~50 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回项目列表（activities：id/name/description/semester_id/mode/progress/create_time/modify_time/create_user_id/modify_user_id）</returns>
    [Post("/open-apis/performance/v2/activity/query")]
    Task<FeishuApiResult<QueryActivityListResult>?> QueryActivityListAsync(
        [Body] QueryActivityListRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量查询补充信息
    /// <para>批量查询被评估人的补充信息（事项、时间、具体描述）。item_ids、external_ids、reviewee_user_ids 均为空时返回 semester_id 指定周期的全部补充信息；多筛选参数按 item_ids &gt; external_ids &gt; reviewee_user_ids 的优先级取第一个有值者。</para>
    /// <para>限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:semester_activity:read、performance:semester_activity:write；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/additional_information/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 必填 1~100 字符；item_ids/external_ids/reviewee_user_ids 各 0~50 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">分页大小，默认 20，取值范围 0~50</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回补充信息分页列表（additional_informations、has_more、page_token）</returns>
    [Post("/open-apis/performance/v2/additional_informations/query")]
    Task<FeishuApiResult<QueryAdditionalInformationListResult>?> QueryAdditionalInformationListAsync(
        [Body] QueryAdditionalInformationListRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量导入补充信息
    /// <para>批量导入被评估人的补充信息作为绩效评估参考，同时支持创建与更新：先按已有 item_id 更新，再按已有 external_id 更新，否则按 reviewee_user_id + item + time + detailed_description 的组合匹配更新，均不匹配则新建。</para>
    /// <para>限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:semester_activity:write；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。client_token 用于幂等去重，重复提交将被拦截（错误码 1580110）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/additional_information/import">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 必填；additional_informations 1~1000 条；import_record_name 导入记录名称，默认「API导入」）</param>
    /// <param name="client_token">幂等请求标识，长度 0~64 字符（必填），示例值：12454646</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回导入记录 ID 与成功导入后的补充信息列表</returns>
    [Post("/open-apis/performance/v2/additional_informations/import")]
    Task<FeishuApiResult<ImportAdditionalInformationResult>?> ImportAdditionalInformationAsync(
        [Body] ImportAdditionalInformationRequest request,
        [Query("client_token")] string client_token,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量删除补充信息
    /// <para>按周期 ID 批量删除被评估人的补充信息。</para>
    /// <para>限频：10 次/分钟。所需权限（开启任一即可）：performance:performance、performance:semester_activity:write；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/additional_information/delete">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 必填；additional_informations 补充信息 ID 列表 1~100 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回被删除的补充信息 ID 列表（additional_informations）</returns>
    [Delete("/open-apis/performance/v2/additional_informations/batch")]
    Task<FeishuApiResult<DeleteAdditionalInformationResult>?> BatchDeleteAdditionalInformationAsync(
        [Body] DeleteAdditionalInformationRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新人员组成员
    /// <para>更新指定人员组的成员。该接口为覆盖式更新，更新操作会清除人员组原有成员；人员组需在后台「人员范围设置」中勾选「API 自动写入人员名单」，否则返回错误码 1580401。</para>
    /// <para>限频：20 次/分钟。所需权限：performance:semester_activity:write（管理周期与项目配置信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。client_token 用于幂等去重，重复提交将被拦截（错误码 1580110）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/user_group_user_rel/write">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（group_id 人员组 ID 必填 0~128 字符；scope_visible_setting 可见性 0 无限制/1 后台管理员不可见，默认 1，取值范围 0~10；user_ids 人员 ID 列表 0~10000 个）</param>
    /// <param name="client_token">幂等请求标识，长度 0~64 字符（必填），示例值：123456</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新结果（data.success_user_ids 更新成功列表、data.fail_user_datas 更新失败明细，响应体为两层 data 结构）</returns>
    [Post("/open-apis/performance/v2/user_group_user_rels/write")]
    Task<FeishuApiResult<WriteUserGroupUserRelResult>?> WriteUserGroupUserRelAsync(
        [Body] WriteUserGroupUserRelRequest request,
        [Query("client_token")] string client_token,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取被评估人信息
    /// <para>获取绩效周期中被圈定到项目中的被评估人信息（含未启动的项目），可按用户或按项目过滤。</para>
    /// <para>限频：20 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly、performance:semester_user:read（获取周期人员信息）；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/reviewee/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 必填；user_ids 0~50 个；activity_ids 项目 ID 列表）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">分页大小，默认 20，取值范围 1~50</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回被评估人分页列表（semester_id、reviewees、has_more、page_token）</returns>
    [Post("/open-apis/performance/v2/reviewees/query")]
    Task<FeishuApiResult<QueryRevieweeListResult>?> QueryRevieweeListAsync(
        [Body] QueryRevieweeListRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取绩效周期人员信息
    /// <para>获取指定绩效周期下，被评估人在评估时的部门、序列、职级等人员快照信息。</para>
    /// <para>限频：100 次/分钟。所需权限（开启任一即可）：performance:performance、performance:performance:readonly；字段权限：contact:user.employee_id:readonly（user_id_type 取 user_id 时的返回字段）、performance:user_snapshot.department:read（部门）、performance:user_snapshot.direct_leader:read（直属上级）、performance:user_snapshot.job_family:read（序列）、performance:user_snapshot.job_level:read（职级）。</para>
    /// <para><see href="https://open.feishu.cn/document/performance-v1/review_config/semester_activity/reviewee/query-2">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（semester_id 必填；user_ids 人员 ID 列表 0~10 个）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型：department_id/open_department_id，默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回周期人员快照列表（semester_id、user_infos：user_id/direct_leader_user_id/department/job_family/job_level）</returns>
    [Post("/open-apis/performance/v2/user_info/query")]
    Task<FeishuApiResult<QueryUserInfoListResult>?> QueryUserInfoListAsync(
        [Body] QueryUserInfoListRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        CancellationToken cancellationToken = default);
}
