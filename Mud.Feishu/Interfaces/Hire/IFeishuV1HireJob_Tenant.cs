// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）职位域 SDK 是一组服务端 OpenAPI 的封装，用于职位的组合创建/更新与设置维护、职位管理人员批量维护、职位详情与列表查询、职位开放，以及职位类别、职能分类、职位模板、职位发布记录与职位广告发布。本接口全部端点仅支持 tenant_access_token 调用。
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


    /// <summary>
    /// 获取职位类别列表
    /// <para>分页获取招聘系统预置的职位类别列表，按创建时间升序返回，并包含节点的父子层级关系（parent_id），可用于构建职位类别树。</para>
    /// <para>限频：20 次/秒。所需权限：hire:job:readonly（获取职位信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-4">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位类别分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/job_types")]
    Task<FeishuApiResult<GetJobTypeListResult>?> GetJobTypeListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取职位职能分类列表
    /// <para>分页获取招聘系统内置的职位职能分类列表（含父级职能分类 ID，可据此构建职能分类树）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:job:readonly（获取职位信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list-3">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 50</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职能分类分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/job_functions")]
    Task<FeishuApiResult<GetJobFunctionListResult>?> GetJobFunctionListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取职位模板列表
    /// <para>按招聘场景（社招/校招）分页获取职位模板列表，返回模板内各模块的字段与选项配置。</para>
    /// <para>限频：10 次/秒。所需权限：hire:job:readonly（获取职位信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/list">接口文档</see></para>
    /// </summary>
    /// <param name="scenario">招聘场景：1 社招 / 2 校招</param>
    /// <param name="page_size">每页数量，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位模板分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/job_schemas")]
    Task<FeishuApiResult<GetJobSchemaListResult>?> GetJobSchemaListAsync(
        [Query("scenario")] int? scenario = null,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询职位发布记录
    /// <para>按招聘渠道分页查询已发布到官网/拉勾等渠道的职位广告记录（查询参数采用查询对象模式 <see cref="JobPublishRecordSearchQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:job:readonly（获取职位信息）或 hire:job（更新职位）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/search">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（job_channel_id 渠道 ID，如官网渠道取 "2"、拉勾渠道取 "3"，可通过获取招聘渠道列表接口获取）</param>
    /// <param name="query">分页与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回职位发布记录分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/job_publish_records/search")]
    Task<FeishuApiResult<SearchJobPublishRecordResult>?> SearchJobPublishRecordAsync(
        [Body] SearchJobPublishRecordRequest request,
        [Query] JobPublishRecordSearchQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 发布职位广告
    /// <para>将指定职位广告发布至所选招聘渠道（如官网招聘渠道）。</para>
    /// <para>限频：10 次/秒。所需权限：hire:advertisement（获取或更新招聘广告信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job/publish">接口文档</see></para>
    /// </summary>
    /// <param name="advertisement_id">职位广告 ID，来自职位创建响应中的 default_job_post.id</param>
    /// <param name="request">发布请求体（job_channel_id 渠道 ID，可通过获取招聘渠道列表接口获取，如官网渠道为 "3"）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/advertisements/{advertisement_id}/publish")]
    Task<FeishuNullDataApiResult?> PublishAdvertisementAsync(
        [Path] string advertisement_id,
        [Body] PublishAdvertisementRequest request,
        CancellationToken cancellationToken = default);
}
