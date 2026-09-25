// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）职位 SDK 是一组服务端 OpenAPI 的封装，用于组合创建/更新职位、职位设置（面试轮次、登记表、自助约面）、职位管理人员维护、职位详情与列表查询、职位开放以及职位发布人查询。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/get"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireJob : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 组合创建职位
    /// <para>一次性提交职位基础信息、职位管理人员与自定义字段等完整信息创建职位，返回职位、职位管理人员、默认职位广告与登记表信息。</para>
    /// <para>限频：20 次/秒。所需权限：hire:job（更新职位）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/combined_create">接口文档</see></para>
    /// </summary>
    /// <param name="request">组合创建请求体（title 职位名称、department_id、job_process_id、job_type_id 等按需填写；job_managers 含 recruiter_id/hiring_manager_id_list）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位信息（job）、职位管理人员（job_manager）、默认职位广告（default_job_post）与登记表信息</returns>
    [Post("/open-apis/hire/v1/jobs/combined_create")]
    Task<FeishuApiResult<CombinedJobResult>?> CombinedCreateJobAsync(
        [Body] CombinedJobRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 组合更新职位
    /// <para>按职位 ID 全量更新职位信息；未填写的字段会被清空，请提交完整职位数据。</para>
    /// <para>限频：10 次/秒。所需权限：hire:job（更新职位）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/combined_update">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="request">组合更新请求体（title、job_managers 必填；其余字段未传即清空）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的职位信息（job）、职位管理人员（job_manager）、默认职位广告与登记表信息</returns>
    [Post("/open-apis/hire/v1/jobs/{job_id}/combined_update")]
    Task<FeishuApiResult<CombinedJobResult>?> CombinedUpdateJobAsync(
        [Path] string job_id,
        [Body] CombinedJobRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新职位设置
    /// <para>更新职位的面试官建议、Offer 申请表、面试/入职登记表、面试轮次类型与自助约面等设置；须按 update_option_list 声明的更新项填写对应必填字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:job（更新职位）。需先在飞书招聘「设置-基础设置」中开启 API 同步职位开关。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/update_config">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="request">职位设置请求体（update_option_list 必填：要更新的配置项编号；各项按需填写）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的职位设置（job_config）</returns>
    [Post("/open-apis/hire/v1/jobs/{job_id}/update_config")]
    Task<FeishuApiResult<UpdateJobConfigResult>?> UpdateJobConfigAsync(
        [Path] string job_id,
        [Body] UpdateJobConfigRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量更新职位管理人员
    /// <para>按 update_option_list 选择的成员项批量更新职位的招聘负责人、招聘助理与用人经理。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:job（更新职位）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/batch_update">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="request">管理人员更新请求体（update_option_list 必填：1 招聘负责人 / 2 招聘助理 / 3 用人经理；对应成员列表随之生效）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的职位管理人员（job_manager）</returns>
    [Post("/open-apis/hire/v1/jobs/{job_id}/managers/batch_update")]
    Task<FeishuApiResult<BatchUpdateJobManagerResult>?> BatchUpdateJobManagerAsync(
        [Path] string job_id,
        [Body] BatchUpdateJobManagerRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取职位信息
    /// <para>按职位 ID 获取职位基础信息、部门、职级、序列、工作城市等完整信息。</para>
    /// <para>限频：50 次/秒。所需权限：hire:job:readonly（获取职位信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/get">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位详情（job）</returns>
    [Get("/open-apis/hire/v1/jobs/{job_id}")]
    Task<FeishuApiResult<GetJobResult>?> GetJobAsync(
        [Path] string job_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取职位详情
    /// <para>获取职位聚合详情，包括基本信息、职位管理人员、招聘需求、职位地址、职位设置、门店、标签与投递阶段统计数据。</para>
    /// <para>限频：20 次/秒。所需权限：hire:job.composite_info:readonly（获取职位聚合信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/get_detail">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位聚合详情（job_detail）</returns>
    [Get("/open-apis/hire/v1/jobs/{job_id}/get_detail")]
    Task<FeishuApiResult<GetJobDetailResult>?> GetJobDetailAsync(
        [Path] string job_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取职位设置
    /// <para>获取职位的 Offer 申请表、审批流程、面试官建议、登记表、面试轮次类型与自助约面等设置。</para>
    /// <para>限频：20 次/秒。所需权限：hire:job:readonly（获取职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/config">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位设置（job_config）</returns>
    [Get("/open-apis/hire/v1/jobs/{job_id}/config")]
    Task<FeishuApiResult<GetJobConfigResult>?> GetJobConfigAsync(
        [Path] string job_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取职位列表
    /// <para>按更新时间、招聘负责人、用人经理、部门等条件分页查询职位列表（查询参数采用查询对象模式 <see cref="JobListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：50 次/秒。所需权限：hire:job:readonly（获取职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="query">更新时间范围、分页与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/jobs")]
    Task<FeishuApiResult<GetJobListResult>?> GetJobListAsync(
        [Query] JobListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 开放职位
    /// <para>开放指定职位用于投递，可指定到期日期或长期有效。</para>
    /// <para>限频：10 次/秒。所需权限：hire:job（更新职位）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/open">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="request">开放请求体（is_never_expired 必填：true 长期有效；false 时 expiry_time 必填且须晚于当前时间）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/jobs/{job_id}/open")]
    Task<FeishuNullDataApiResult?> OpenJobAsync(
        [Path] string job_id,
        [Body] OpenJobRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取职位发布人
    /// <para>获取指定职位的招聘负责人、用人经理与招聘助理列表。</para>
    /// <para>限频：50 次/秒。所需权限：hire:job:readonly（获取职位信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/recruiter">接口文档</see></para>
    /// </summary>
    /// <param name="job_id">职位 ID，示例值：6960663240925956660</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位管理人员信息（info）</returns>
    [Get("/open-apis/hire/v1/jobs/{job_id}/recruiter")]
    Task<FeishuApiResult<GetJobRecruiterResult>?> GetJobRecruiterAsync(
        [Path] string job_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
