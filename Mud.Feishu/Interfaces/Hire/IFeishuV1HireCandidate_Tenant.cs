// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）候选人入口域 SDK 是一组服务端 OpenAPI 的封装，覆盖飞书文档 candidate-management 分组的全部能力：内推信息与内推官网职位、招聘官网（列表/推广渠道/官网用户/官网职位）、官网投递创建与投递任务、官网申请表模板，人才管理（人才池、人才文件夹、标签、组合创建/更新、黑名单、入职状态、人才查询），投递流程管理（面试信息与评价记录 v1/v2、记录附件、速记明细、满意度问卷，Offer 创建/更新/查询/列表/状态变更，背调订单，三方协议，入职转正/取消与员工信息维护），以及人才备注增删改查、简历评估/笔试阅卷/面试任务列表与简历来源列表查询。本接口全部端点仅支持 tenant_access_token 调用（待办事项批量获取见用户态接口 <see cref="IFeishuUserV1HireCandidate"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireCandidate : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询内推信息
    /// <para>按人才 ID 与创建时间范围查询内推记录，按内推投递的创建时间倒序排列；不填时间范围时默认返回全部，最多 200 条。</para>
    /// <para>限频：10 次/秒。所需权限：hire:referral:readonly（获取内推信息）或 hire:referral（更新内推信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/referral/search">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（talent_id 必填：人才 ID；start_time/end_time 最早/最晚创建时间，毫秒时间戳）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回内推信息列表（items）</returns>
    [Post("/open-apis/hire/v1/referrals/search")]
    Task<FeishuApiResult<SearchReferralResult>?> SearchReferralAsync(
        [Body] SearchReferralRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取内推官网职位列表
    /// <para>分页获取内推官网的职位列表（不含自定义数据；含自定义数据的详情请用获取内推官网职位详情接口）（查询参数采用查询对象模式 <see cref="ReferralWebsiteJobPostListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:referral_website:readonly（获取内推官网信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/referral/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">流程类型、分页与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回内推官网职位分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/referral_websites/job_posts")]
    Task<FeishuApiResult<GetReferralWebsiteJobPostListResult>?> GetReferralWebsiteJobPostListAsync(
        [Query] ReferralWebsiteJobPostListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取内推官网职位详情
    /// <para>按职位广告 ID 获取内推官网职位详情（含自定义数据）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:referral_website:readonly（获取内推官网信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/referral/get">接口文档</see></para>
    /// </summary>
    /// <param name="job_post_id">职位广告 ID，示例值：6701528341100366094</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回内推官网职位详情（job_post）</returns>
    [Get("/open-apis/hire/v1/referral_websites/job_posts/{job_post_id}")]
    Task<FeishuApiResult<GetReferralWebsiteJobPostResult>?> GetReferralWebsiteJobPostAsync(
        [Path] string job_post_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 按投递 ID 获取内推信息
    /// <para>按投递 ID 获取内推信息；仅适用于内推时选择了具体职位的场景，无具体职位的内推（未选职位投递）不存投递 ID，需改用内推 ID 或人才 ID 查询。</para>
    /// <para>限频：50 次/秒。所需权限：hire:referral:readonly（获取内推信息）或 hire:referral（更新内推信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/referral/get_by_application">接口文档</see></para>
    /// </summary>
    /// <param name="application_id">投递 ID，示例值：6134134355464633</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回内推信息（referral）</returns>
    [Get("/open-apis/hire/v1/referrals/get_by_application")]
    Task<FeishuApiResult<GetReferralByApplicationResult>?> GetReferralByApplicationAsync(
        [Query("application_id")] string application_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取官网申请表模板列表
    /// <para>分页获取「招聘官网申请表设置」页配置的申请表模板列表（官网/职位投递时按该模板填写申请表）。该能力处于灰度阶段。</para>
    /// <para>限频：20 次/秒。所需权限：hire:site:readonly（获取官网信息）或 hire:site（更新官网信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/portal_apply_schema/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 50，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回官网申请表模板分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/portal_apply_schemas")]
    Task<FeishuApiResult<GetPortalApplySchemaListResult>?> GetPortalApplySchemaListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建官网推广渠道
    /// <para>按招聘官网 ID 与渠道名称创建推广渠道，返回渠道链接与推广码。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site（更新官网信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/create-2">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：1618209327096</param>
    /// <param name="request">创建请求体（channel_name 推广渠道名称，必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回推广渠道信息（id、name、link、code）</returns>
    [Post("/open-apis/hire/v1/websites/{website_id}/channels")]
    Task<FeishuApiResult<WebsiteChannelInfo>?> CreateWebsiteChannelAsync(
        [Path] string website_id,
        [Body] WebsiteChannelRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新官网推广渠道
    /// <para>按推广渠道 ID 修改招聘官网推广渠道名称。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site（更新官网信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/update">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：1618209327096</param>
    /// <param name="channel_id">推广渠道 ID，示例值：7085989097067563300</param>
    /// <param name="request">更新请求体（channel_name 推广渠道名称，必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的推广渠道信息（id、name、link、code）</returns>
    [Put("/open-apis/hire/v1/websites/{website_id}/channels/{channel_id}")]
    Task<FeishuApiResult<WebsiteChannelInfo>?> UpdateWebsiteChannelAsync(
        [Path] string website_id,
        [Path] string channel_id,
        [Body] WebsiteChannelRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除官网推广渠道
    /// <para>按招聘官网 ID 与推广渠道 ID 删除推广渠道。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site（更新官网信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/delete">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：1618209327096</param>
    /// <param name="channel_id">推广渠道 ID，示例值：7085989097067563300</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/websites/{website_id}/channels/{channel_id}")]
    Task<FeishuNullDataApiResult?> DeleteWebsiteChannelAsync(
        [Path] string website_id,
        [Path] string channel_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取官网推广渠道列表
    /// <para>按招聘官网 ID 分页获取推广渠道列表。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site:readonly（获取官网信息）或 hire:site（更新官网信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/list-3">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：1618209327096</param>
    /// <param name="page_size">每页数量，最大 200，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回推广渠道分页列表（website_channel_list、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/websites/{website_id}/channels")]
    Task<FeishuApiResult<GetWebsiteChannelListResult>?> GetWebsiteChannelListAsync(
        [Path] string website_id,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建官网用户
    /// <para>在招聘官网创建用户；external_id 为幂等字段，同一外部 ID 只会创建 1 个官网用户，已存在时返回已有用户信息。</para>
    /// <para>限频：10 次/秒。所需权限：hire:site（更新官网信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/create">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，可通过获取官网列表接口获取，示例值：1618209327096</param>
    /// <param name="request">创建请求体（external_id 必填；name/email/mobile/mobile_country_code 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回官网用户信息（site_user）</returns>
    [Post("/open-apis/hire/v1/websites/{website_id}/site_users")]
    Task<FeishuApiResult<CreateWebsiteUserResult>?> CreateWebsiteUserAsync(
        [Path] string website_id,
        [Body] CreateWebsiteUserRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取官网列表
    /// <para>分页获取招聘官网列表，返回官网名称、流程类型与招聘渠道 ID。</para>
    /// <para>限频：10 次/秒。所需权限：hire:site:readonly（获取官网信息）或 hire:site（更新官网信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回官网分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/websites")]
    Task<FeishuApiResult<GetWebsiteListResult>?> GetWebsiteListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取官网职位详情
    /// <para>按官网 ID 与职位广告 ID 获取官网职位详情（含自定义数据与目标专业）。官网职位以 job_post_id（职位广告 ID）标识，而非 job_id。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site_job_post:readonly（获取官网职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/get">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：111</param>
    /// <param name="job_post_id">职位广告 ID，示例值：111</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回官网职位详情（job_post）</returns>
    [Get("/open-apis/hire/v1/websites/{website_id}/job_posts/{job_post_id}")]
    Task<FeishuApiResult<GetWebsiteJobPostResult>?> GetWebsiteJobPostAsync(
        [Path] string website_id,
        [Path] string job_post_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取官网职位列表
    /// <para>按官网 ID 分页获取职位列表（暂不支持获取自定义数据，请用详情接口）（查询参数采用查询对象模式 <see cref="WebsiteJobPostListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site_job_post:readonly（获取官网职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：111</param>
    /// <param name="query">分页、创建/更新时间范围与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回官网职位分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/websites/{website_id}/job_posts")]
    Task<FeishuApiResult<GetWebsiteJobPostListResult>?> GetWebsiteJobPostListAsync(
        [Path] string website_id,
        [Query] WebsiteJobPostListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 搜索官网职位列表
    /// <para>按职位类别、城市、职能、科目、关键词与创建/更新时间范围搜索官网职位。</para>
    /// <para>限频：50 次/秒。所需权限：hire:site_job_post:readonly（获取官网职位信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/search">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：111</param>
    /// <param name="request">搜索请求体（各类 ID 列表最大 100 个；keyword、创建/更新时间范围选填）</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">每页数量，最大 10，默认 10</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回官网职位分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/websites/{website_id}/job_posts/search")]
    Task<FeishuApiResult<SearchWebsiteJobPostResult>?> SearchWebsiteJobPostAsync(
        [Path] string website_id,
        [Body] SearchWebsiteJobPostRequest request,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 按简历创建官网投递
    /// <para>按结构化简历信息在招聘官网创建投递，需先创建官网用户；自定义字段取值格式见接口文档说明（单选传选项 ID、多选传选项 ID 数组字符串、时间段传毫秒时间戳数组等）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site_application（更新官网投递信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/create_by_resume">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：1618209327096</param>
    /// <param name="request">投递请求体（job_post_id、resume、user_id 必填；resume 含基本信息、教育/工作/实习经历、自定义模块等）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回投递信息（delivery：投递 ID、职位、简历、官网用户与人才 ID）</returns>
    [Post("/open-apis/hire/v1/websites/{website_id}/deliveries/create_by_resume")]
    Task<FeishuApiResult<CreateWebsiteDeliveryByResumeResult>?> CreateWebsiteDeliveryByResumeAsync(
        [Path] string website_id,
        [Body] CreateWebsiteDeliveryByResumeRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 按简历附件创建官网投递
    /// <para>上传简历附件解析后在招聘官网创建投递（异步任务），返回 task_id 后需轮询获取投递任务结果接口获取解析与投递结果。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site_application（更新官网投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/create_by_attachment">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：7047318856652261676</param>
    /// <param name="request">投递请求体（job_post_id、user_id、resume_file_id 必填；channel_id、手机号/邮箱/证件信息选填，与附件不一致时以参数为准）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回异步任务 ID（task_id）</returns>
    [Post("/open-apis/hire/v1/websites/{website_id}/deliveries/create_by_attachment")]
    Task<FeishuApiResult<CreateWebsiteDeliveryByAttachmentResult>?> CreateWebsiteDeliveryByAttachmentAsync(
        [Path] string website_id,
        [Body] CreateWebsiteDeliveryByAttachmentRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取官网投递任务结果
    /// <para>查询简历附件投递任务状态；返回数据为空时请继续轮询，直到有数据后再解析 delivery。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:site_application:readonly（获取官网投递信息）或 hire:site_application（更新官网投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/website/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="website_id">官网 ID，示例值：7047318856652261676</param>
    /// <param name="delivery_task_id">投递任务 ID，来自按简历附件创建官网投递的返回，示例值：f1c2a0f138ec492d99d7ab73594158ad</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回任务状态（status：0 新建/1 处理中/2 成功/3 失败）、投递信息与失败原因</returns>
    [Get("/open-apis/hire/v1/websites/{website_id}/delivery_tasks/{delivery_task_id}")]
    Task<FeishuApiResult<GetWebsiteDeliveryTaskResult>?> GetWebsiteDeliveryTaskAsync(
        [Path] string website_id,
        [Path] string delivery_task_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建人才备注
    /// <para>为人才创建备注信息，支持在备注中 @ 其他用户。</para>
    /// <para>限频：20 次/秒。所需权限：hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/note/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（talent_id、content 必填；application_id、creator_id、privacy 私密属性（1-私密/2-公开）、notify_mentioned_user、mention_entity_list 选填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回备注信息（note：备注 ID、人才/投递 ID、是否私密、创建/更新时间、创建人 ID、内容）</returns>
    [Post("/open-apis/hire/v1/notes")]
    Task<FeishuApiResult<CreateNoteResult>?> CreateNoteAsync(
        [Body] CreateNoteRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新人才备注
    /// <para>按备注 ID 更新备注内容，支持更新 @ 用户并选择是否通知被 @ 的用户。</para>
    /// <para>限频：20 次/秒。所需权限：hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/note/patch">接口文档</see></para>
    /// </summary>
    /// <param name="note_id">备注 ID，可通过获取人才备注列表接口获取，示例值：6960663240925956401</param>
    /// <param name="request">更新请求体（content 必填；operator_id、notify_mentioned_user、mention_entity_list 选填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的备注信息（note）</returns>
    [Patch("/open-apis/hire/v1/notes/{note_id}")]
    Task<FeishuApiResult<PatchNoteResult>?> PatchNoteAsync(
        [Path] string note_id,
        [Body] PatchNoteRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才备注
    /// <para>按备注 ID 获取备注详情。</para>
    /// <para>限频：20 次/秒。所需权限：hire:note:readonly（获取招聘备注）或 hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/note/get">接口文档</see></para>
    /// </summary>
    /// <param name="note_id">备注 ID，可通过获取人才备注列表接口获取，示例值：6949805467799537964</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回备注信息（note）</returns>
    [Get("/open-apis/hire/v1/notes/{note_id}")]
    Task<FeishuApiResult<GetNoteResult>?> GetNoteAsync(
        [Path] string note_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才备注列表
    /// <para>按人才 ID 分页获取备注列表。</para>
    /// <para>限频：20 次/秒。所需权限：hire:note:readonly（获取招聘备注）或 hire:note（更新招聘备注）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/note/list">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：6916472453069883661</param>
    /// <param name="page_size">每页数量，默认 10，最大 200</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回备注分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/notes")]
    Task<FeishuApiResult<GetNoteListResult>?> GetNoteListAsync(
        [Query("talent_id")] string talent_id,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除人才备注
    /// <para>按备注 ID 删除指定备注。</para>
    /// <para>限频：20 次/秒。所需权限：hire:note（更新招聘备注）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/note/delete">接口文档</see></para>
    /// </summary>
    /// <param name="note_id">备注 ID，可通过获取人才备注列表接口获取，示例值：6996605821056812588</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>删除成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/notes/{note_id}")]
    Task<FeishuNullDataApiResult?> DeleteNoteAsync(
        [Path] string note_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取简历评估任务列表
    /// <para>获取指定评估人的简历评估任务列表。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:evaluation:readonly（获取简历评估信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/recruitment-process-follow-up/list-3">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">评估人 ID，需与 user_id_type 类型保持一致，示例值：ou_e6139117c300506837def50545420c6a</param>
    /// <param name="page_size">每页数量，最大 20，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="activity_status">任务状态：1-待评估，2-已评估，3-无需评估；不传查询全部</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回评估任务分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/evaluation_tasks")]
    Task<FeishuApiResult<GetEvaluationTaskListResult>?> GetEvaluationTaskListAsync(
        [Query("user_id")] string user_id,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("activity_status")] int? activity_status = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取笔试阅卷任务列表
    /// <para>获取指定员工的笔试阅卷任务列表。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:exam:readonly（获取笔试信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/recruitment-process-follow-up/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">阅卷人 ID，需与 user_id_type 类型保持一致，示例值：ou_e6139117c300506837def50545420c6a</param>
    /// <param name="page_size">每页数量，最大 20，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="activity_status">任务状态：1-待阅卷，2-已阅卷；不传查询全部</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回笔试阅卷任务分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/exam_marking_tasks")]
    Task<FeishuApiResult<GetExamMarkingTaskListResult>?> GetExamMarkingTaskListAsync(
        [Query("user_id")] string user_id,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("activity_status")] int? activity_status = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试任务列表
    /// <para>获取指定员工的面试任务列表。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/recruitment-process-follow-up/list-4">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">面试官 ID，需与 user_id_type 类型保持一致，示例值：ou_e6139117c300506837def50545420c6a</param>
    /// <param name="page_size">每页数量，最大 20，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="activity_status">任务状态：1-未开始，2-未评估，3-已评估，5-已终止；不传查询全部</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试任务分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/interview_tasks")]
    Task<FeishuApiResult<GetInterviewTaskListResult>?> GetInterviewTaskListAsync(
        [Query("user_id")] string user_id,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("activity_status")] int? activity_status = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取简历来源列表
    /// <para>分页获取简历来源列表（内推、猎头、第三方招聘网站等来源配置）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/resume_source/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回简历来源分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/resume_sources")]
    Task<FeishuApiResult<GetResumeSourceListResult>?> GetResumeSourceListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量加入/移除人才库中人才
    /// <para>对同一个人才库批量执行人才加入或移除操作；不存在的 ID 报错返回，已在/不在库中则静默处理。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent_folder（更新人才库信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent_pool/batch_change_talent_pool">接口文档</see></para>
    /// </summary>
    /// <param name="talent_pool_id">人才库 ID，可通过获取人才库列表接口获取，示例值：6930815272790114324</param>
    /// <param name="request">请求体（talent_id_list 必填：人才 ID 列表，1～50 个；option_type 必填：1 加入 / 2 移除）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talent_pools/{talent_pool_id}/batch_change_talent_pool")]
    Task<FeishuNullDataApiResult?> BatchChangeTalentPoolAsync(
        [Path] string talent_pool_id,
        [Body] BatchChangeTalentPoolRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才库列表
    /// <para>分页获取人才库列表，返回人才库 ID、中英文名称与描述、父子关系、可见性与创建/修改时间。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder:readonly（获取人才库信息）或 hire:talent_folder（更新人才库信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent_pool/search">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="id_list">人才库 ID 列表，传入时按列表返回，最大 50 个；不传返回全部</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才库分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/talent_pools")]
    Task<FeishuApiResult<GetTalentPoolListResult>?> GetTalentPoolListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("id_list")] string[]? id_list = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 将人才加入人才库
    /// <para>将单个人才加入指定人才库，并可选择加入后是否从其他人才库移出。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder（更新人才库信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent_pool/move_talent">接口文档</see></para>
    /// </summary>
    /// <param name="talent_pool_id">人才库 ID，可通过获取人才库列表接口获取，示例值：6930815272790114324</param>
    /// <param name="request">请求体（talent_id 必填：人才 ID；add_type 必填：1 不从其他库移出 / 2 从其他库移出）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才库 ID 与人才 ID（talent_pool_id、talent_id）</returns>
    [Post("/open-apis/hire/v1/talent_pools/{talent_pool_id}/talent_relationship")]
    Task<FeishuApiResult<AddTalentToTalentPoolResult>?> AddTalentToTalentPoolAsync(
        [Path] string talent_pool_id,
        [Body] AddTalentToTalentPoolRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 操作人才标签
    /// <para>为人才批量新增或删除标签。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent/tag">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，可通过批量获取人才 ID 接口获取，示例值：6930815272790114324</param>
    /// <param name="request">请求体（operation 必填：1 新增 / 2 删除；tag_id_list 必填：标签 ID 列表）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talents/{talent_id}/tag")]
    Task<FeishuNullDataApiResult?> OperateTalentTagAsync(
        [Path] string talent_id,
        [Body] OperateTalentTagRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建人才
    /// <para>在企业内创建一个人才（简历级聚合写入）；预置姓名必填，邮箱/手机是否必填取决于飞书招聘标准简历模板设置。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/combined_create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（basic_info 必填；education_list 等各子经历数组最大 100 条）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才 ID 与创建者信息（talent_id、creator_id、creator_account_type）</returns>
    [Post("/open-apis/hire/v1/talents/combined_create")]
    Task<FeishuApiResult<CombinedCreateTalentResult>?> CombinedCreateTalentAsync(
        [Body] CombinedCreateTalentRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新人才信息
    /// <para>按人才 ID 全量聚合更新企业内人才信息；各子经历数组为全量覆盖，邮箱/手机必填性取决于标准简历模板设置。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/combined_update">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（talent_id 必填；basic_info 必填；operator_id/operator_account_type 为更新者信息）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才 ID 与更新者信息（talent_id、operator_id、operator_account_type）</returns>
    [Post("/open-apis/hire/v1/talents/combined_update")]
    Task<FeishuApiResult<CombinedUpdateTalentResult>?> CombinedUpdateTalentAsync(
        [Body] CombinedUpdateTalentRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 人才加入文件夹
    /// <para>把一批人才加入到指定文件夹。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）或 hire:talent_folder_association（更新人才文件夹关联信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/add_to_folder">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（talent_id_list 必填：人才 ID 列表，1～200 个；folder_id 必填：文件夹 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回回显的人才 ID 列表与文件夹 ID（talent_id_list、folder_id）</returns>
    [Post("/open-apis/hire/v1/talents/add_to_folder")]
    Task<FeishuApiResult<TalentToFolderResult>?> AddTalentToFolderAsync(
        [Body] TalentToFolderRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 人才移出文件夹
    /// <para>按人才 ID 列表把人才从指定文件夹移出。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent_folder_association（更新人才文件夹关联信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent/remove_to_folder">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（talent_id_list 必填：人才 ID 列表，1～200 个；folder_id 必填：文件夹 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回回显的人才 ID 列表与文件夹 ID（talent_id_list、folder_id）</returns>
    [Post("/open-apis/hire/v1/talents/remove_to_folder")]
    Task<FeishuApiResult<TalentToFolderResult>?> RemoveTalentFromFolderAsync(
        [Body] TalentToFolderRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才文件夹信息
    /// <para>分页获取招聘系统中的人才文件夹列表（文件夹维度的人才库）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent_folder:readonly（获取人才库信息）或 hire:talent_folder（更新人才库信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才文件夹分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/talent_folders")]
    Task<FeishuApiResult<GetTalentFolderListResult>?> GetTalentFolderListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 根据手机号或邮箱获取人才 ID
    /// <para>通过手机号、邮箱或证件号批量反查人才 ID 及基础信息；三类条件为且（AND）关系，至少传入一种，最多各 100 条。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：hire:talent_onboard_status:readonly（返回的入职状态字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/batch_get_id">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（mobile_code、mobile_number_list、email_list、identification_type、identification_number_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才信息列表（talent_list）</returns>
    [Post("/open-apis/hire/v1/talents/batch_get_id")]
    Task<FeishuApiResult<BatchGetTalentIdResult>?> BatchGetTalentIdAsync(
        [Body] BatchGetTalentIdRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才列表
    /// <para>按关键词、更新时间等条件分页获取候选人（人才）全量档案列表（查询参数采用查询对象模式 <see cref="TalentListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">关键词、更新时间范围、分页、排序与 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/talents")]
    Task<FeishuApiResult<GetTalentListResult>?> GetTalentListAsync(
        [Query] TalentListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才字段
    /// <para>获取人才档案的字段（模块）配置清单，含字段类型、选项与自定义字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/query">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回模块（字段组）列表（items）</returns>
    [Get("/open-apis/hire/v1/talent_objects/query")]
    Task<FeishuApiResult<QueryTalentObjectResult>?> QueryTalentObjectAsync(
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才信息（v1）
    /// <para>按人才 ID 获取候选人（人才）完整信息，返回 v1 版 talent 结构。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/get">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，可通过获取人才列表接口获取，示例值：6930815272790114324</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才信息（talent）</returns>
    [Get("/open-apis/hire/v1/talents/{talent_id}")]
    Task<FeishuApiResult<GetTalentResult>?> GetTalentAsync(
        [Path] string talent_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才详细信息（v2）
    /// <para>按人才 ID 获取人才 v2 版详细信息（data 直接为 composite_talent，含人才库、文件夹、标签、黑名单、备注等扩展数据）；不支持查询已删除人才。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）；备注/人才库/标签/黑名单等字段另需对应字段权限。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：6930815272790114324</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 v2 版人才聚合信息（talent_id、basic_info、各经历列表、人才库/标签/黑名单等）</returns>
    [Get("/open-apis/hire/v2/talents/{talent_id}")]
    Task<FeishuApiResult<GetTalentV2Result>?> GetTalentV2Async(
        [Path] string talent_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新人才在职状态
    /// <para>标记人才「入职/离职」；仅适用于未通过飞书招聘投递入职的候选人，且通过本接口标记入职的只能用本接口标记离职。仅自建应用可用。</para>
    /// <para>限频：20 次/分钟。所需权限：hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/talent/onboard_status">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：6930815272790114324</param>
    /// <param name="request">请求体（operation 必填：1 入职 / 2 离职；onboard_time/overboard_time 按操作类型必填，毫秒时间戳）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talents/{talent_id}/onboard_status")]
    Task<FeishuNullDataApiResult?> UpdateTalentOnboardStatusAsync(
        [Path] string talent_id,
        [Body] UpdateTalentOnboardStatusRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 加入/移出人才黑名单
    /// <para>按人才 ID 将人才加入或移出招聘黑名单。仅自建应用可用。</para>
    /// <para>限频：10 次/秒。所需权限：hire:talent_blocklist（更新黑名单信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/talent/change_talent_block">接口文档</see></para>
    /// </summary>
    /// <param name="request">请求体（talent_id 必填：人才 ID；option 必填：1 加入 / 2 移出；reason 加入黑名单时必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/talent_blocklist/change_talent_block")]
    Task<FeishuNullDataApiResult?> ChangeTalentBlockAsync(
        [Body] ChangeTalentBlockRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取背调信息列表
    /// <para>根据投递 ID 或背调更新时间，分页批量获取背调订单信息（含报告、进度、自定义字段等）（查询参数采用查询对象模式 <see cref="BackgroundCheckOrderListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新招聘背调信息）或 hire:background_check_order:readonly（获取招聘背调信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的发起人 ID）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/background_check_order/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、投递 ID、更新时间范围与用户 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回背调订单分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/background_check_orders")]
    Task<FeishuApiResult<GetBackgroundCheckOrderListResult>?> GetBackgroundCheckOrderListAsync(
        [Query] BackgroundCheckOrderListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询背调信息列表
    /// <para>按背调 ID 列表、投递 ID、时间范围、订单状态等条件组合分页查询背调订单；传入 background_check_order_id_list 时其余查询字段全部失效。</para>
    /// <para>限频：10 次/秒。所需权限：hire:background_check_order（更新招聘背调信息）或 hire:background_check_order:readonly（获取招聘背调信息）。字段权限：contact:user.employee_id:readonly、hire:employee.email:readonly、hire:employee.mobile:readonly、hire:talent.email:readonly、hire:talent.mobile:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/background_check_order/batch_query">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（background_check_order_id_list 最大 20 个；application_id、order_status、创建/更新时间范围选填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">每页数量，默认 10，最大 100</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回背调订单分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/background_check_orders/batch_query")]
    Task<FeishuApiResult<GetBackgroundCheckOrderListResult>?> BatchQueryBackgroundCheckOrderAsync(
        [Body] BatchQueryBackgroundCheckOrderRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建三方协议
    /// <para>在校招投递上创建一条三方协议记录；需在招聘后台「设置-候选人流程管理-三方协议设置」勾选「通过 API 维护三方协议」，且投递为校招投递、Offer 办公地点在中国大陆。</para>
    /// <para>限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/tripartite_agreement/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（application_id 必填：投递 ID；state 必填：协议状态；create_time 必填：创建时间毫秒时间戳）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回创建的三方协议 ID（id）</returns>
    [Post("/open-apis/hire/v1/tripartite_agreements")]
    Task<FeishuApiResult<CreateTripartiteAgreementResult>?> CreateTripartiteAgreementAsync(
        [Body] CreateTripartiteAgreementRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取三方协议
    /// <para>按三方协议 ID 或投递 ID 查询三方协议信息（状态、创建/修改时间）；application_id 与 tripartite_agreement_id 至少填一个，都填时以协议 ID 为准（当前接口只返回一条数据，page_size/page_token 实际无效）。</para>
    /// <para>限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）或 hire:tripartite_agreement:readonly（查看三方协议信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/tripartite_agreement/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小（当前接口实际无效）</param>
    /// <param name="page_token">分页标记（当前接口实际无效）</param>
    /// <param name="application_id">投递 ID，示例值：6930815272790114324</param>
    /// <param name="tripartite_agreement_id">三方协议 ID，由创建接口返回，示例值：6930815272790114325</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回三方协议分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/tripartite_agreements")]
    Task<FeishuApiResult<GetTripartiteAgreementListResult>?> GetTripartiteAgreementListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("application_id")] string? application_id = null,
        [Query("tripartite_agreement_id")] string? tripartite_agreement_id = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新三方协议
    /// <para>更新已有三方协议的状态与修改时间；需在招聘后台勾选「通过 API 维护三方协议」。</para>
    /// <para>限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/tripartite_agreement/update">接口文档</see></para>
    /// </summary>
    /// <param name="tripartite_agreement_id">三方协议 ID，示例值：7084008015948283905</param>
    /// <param name="request">更新请求体（state 必填：协议状态；modify_time 必填：修改时间毫秒时间戳，不可小于创建或上次修改时间）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/hire/v1/tripartite_agreements/{tripartite_agreement_id}")]
    Task<FeishuNullDataApiResult?> UpdateTripartiteAgreementAsync(
        [Path] string tripartite_agreement_id,
        [Body] UpdateTripartiteAgreementRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除三方协议
    /// <para>删除某投递下的三方协议记录；需在招聘后台勾选「通过 API 维护三方协议」。</para>
    /// <para>限频：10 次/秒。所需权限：hire:tripartite_agreement（更新三方协议）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/tripartite_agreement/delete">接口文档</see></para>
    /// </summary>
    /// <param name="tripartite_agreement_id">三方协议 ID，示例值：6930815272790114324</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/tripartite_agreements/{tripartite_agreement_id}")]
    Task<FeishuNullDataApiResult?> DeleteTripartiteAgreementAsync(
        [Path] string tripartite_agreement_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 操作候选人入职
    /// <para>按投递 ID 操作候选人入职并创建员工；需在招聘后台开启「通过 e-HR / OA 办公系统同步候选人入职、转正、离职事件」，且投递须处于「待入职」阶段；仅自建应用可用。</para>
    /// <para>限频：20 次/秒。所需权限：hire:application（更新投递信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/onboard/transfer_onboard">接口文档</see></para>
    /// </summary>
    /// <param name="application_id">投递 ID，示例值：7073372582620416300</param>
    /// <param name="request">请求体（actual_onboard_time 实际入职时间毫秒时间戳，不传默认当前时间；部门/上级/序列/职级等选填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回入职信息（employee：员工 ID、入职/转正状态与时间、部门/上级等）</returns>
    [Post("/open-apis/hire/v1/applications/{application_id}/transfer_onboard")]
    Task<FeishuApiResult<TransferOnboardResult>?> TransferOnboardAsync(
        [Path] string application_id,
        [Body] TransferOnboardRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 取消候选人入职
    /// <para>取消待入职阶段候选人的入职（已入职者请改用更新员工状态接口做离职）；集成了飞书人事且已在人事创建待入职记录的候选人只能在飞书人事取消；仅自建应用可用。</para>
    /// <para>限频：10 次/秒。所需权限：hire:application（更新投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/onboard/cancel_onboard">接口文档</see></para>
    /// </summary>
    /// <param name="application_id">投递 ID，示例值：1111111111</param>
    /// <param name="request">请求体（termination_type 必填：1 我们拒绝了候选人 / 22 候选人拒绝了我们 / 27 其他；具体原因 ID 列表与备注选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/applications/{application_id}/cancel_onboard")]
    Task<FeishuNullDataApiResult?> CancelOnboardAsync(
        [Path] string application_id,
        [Body] CancelOnboardRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过投递 ID 获取入职信息
    /// <para>通过投递 ID 查询员工入职信息（查询参数采用查询对象模式 <see cref="GetEmployeeByApplicationQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:employee（更新招聘员工信息）或 hire:employee:readonly（获取招聘员工信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/onboard/get_by_application">接口文档</see></para>
    /// </summary>
    /// <param name="query">投递 ID（必填）与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回入职信息（employee，含飞书人事雇佣 ID external_employment_id）</returns>
    [Get("/open-apis/hire/v1/employees/get_by_application")]
    Task<FeishuApiResult<GetEmployeeByApplicationResult>?> GetEmployeeByApplicationAsync(
        [Query] GetEmployeeByApplicationQuery query,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过员工 ID 获取入职信息
    /// <para>通过员工 ID 查询入职信息。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:employee:readonly（获取招聘员工信息）或 hire:employee（更新招聘员工信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/onboard/get">接口文档</see></para>
    /// </summary>
    /// <param name="employee_id">员工 ID，示例值：7379910335417927975</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回入职信息（employee）</returns>
    [Get("/open-apis/hire/v1/employees/{employee_id}")]
    Task<FeishuApiResult<GetEmployeeResult>?> GetEmployeeAsync(
        [Path] string employee_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新员工状态
    /// <para>根据员工 ID 更新招聘系统内的转正、离职、恢复待入职、撤销离职、撤销转正状态；仅自建应用可用。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:employee（更新招聘员工信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/onboard/patch">接口文档</see></para>
    /// </summary>
    /// <param name="employee_id">员工 ID，示例值：6891613503971461384</param>
    /// <param name="request">请求体（operation 必填：1 转正 / 2 离职 / 3 恢复至待入职 / 4 撤销离职 / 5 撤销转正；conversion_info 转正时必填、overboard_info 离职时必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的入职信息（employee）</returns>
    [Patch("/open-apis/hire/v1/employees/{employee_id}")]
    Task<FeishuApiResult<PatchEmployeeResult>?> PatchEmployeeAsync(
        [Path] string employee_id,
        [Body] PatchEmployeeRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新 e-HR 导入任务结果
    /// <para>处理完「导入 e-HR」事件后，向飞书回写该导入任务的成功/失败结果及跳转链接；仅自建应用可用。</para>
    /// <para>限频：50 次/秒（英文文档标注特殊限频）。所需权限：hire:ehr_import（更新导入 e-HR 任务）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/onboard/patch-2">接口文档</see></para>
    /// </summary>
    /// <param name="ehr_import_task_id">导入任务 ID，来源于「导入 e-HR」事件中的 task_id，示例值：6914551145542568199</param>
    /// <param name="request">请求体（state 必填：1 导入成功 / 2 导入失败；fail_reason 失败原因、redirect_url 跳转链接选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Patch("/open-apis/hire/v1/ehr_import_tasks/{ehr_import_task_id}")]
    Task<FeishuNullDataApiResult?> PatchEhrImportTaskAsync(
        [Path] string ehr_import_task_id,
        [Body] PatchEhrImportTaskRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建 Offer
    /// <para>传入 Offer 基本信息，为指定投递创建 Offer（正式或实习）；仅自建应用可用。</para>
    /// <para>限频：10 次/秒。所需权限：hire:offer（更新 offer 信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的敏感字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/offer/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（application_id 必填；basic_info 必填，含部门/直属上级/负责人/操作人；salary_info、customized_info_list 选填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回创建的 Offer 信息（offer_id、application_id、schema_id、offer_type、basic_info、salary_info、customized_info_list）</returns>
    [Post("/open-apis/hire/v1/offers")]
    Task<FeishuApiResult<CreateOfferResult>?> CreateOfferAsync(
        [Body] CreateOfferRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新 Offer 信息
    /// <para>全量覆盖更新 Offer 的基本信息、薪资信息与自定义信息；状态为「Offer 已发送(6)」「候选人已接受(7)」时不可更新；仅自建应用可用。</para>
    /// <para>限频：10 次/秒。所需权限：hire:offer（更新 offer 信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的敏感字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/offer/update">接口文档</see></para>
    /// </summary>
    /// <param name="offer_id">Offer ID，可通过获取 Offer 列表接口获取，示例值：7085989097067563300</param>
    /// <param name="request">更新请求体（schema_id 必填且须最新版；basic_info 必填；旧自定义字段不传=删除）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的 Offer 信息（offer_id、schema_id、basic_info、salary_info、customized_info_list）</returns>
    [Put("/open-apis/hire/v1/offers/{offer_id}")]
    Task<FeishuApiResult<UpdateOfferResult>?> UpdateOfferAsync(
        [Path] string offer_id,
        [Body] UpdateOfferRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Offer 信息（按投递 ID）
    /// <para>根据投递 ID 获取该投递下的 Offer 数据；暂不支持查询实习 Offer。</para>
    /// <para>限频：20 次/秒。所需权限：hire:application（更新投递信息）或 hire:application:readonly（获取投递信息）。字段权限：contact:user.employee_id:readonly（返回的敏感字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/offer/offer">接口文档</see></para>
    /// </summary>
    /// <param name="application_id">投递 ID，示例值：6701528341100366094</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 数据（offer：含 basic_info、salary_plan、offer_status、发送记录等）</returns>
    [Get("/open-apis/hire/v1/applications/{application_id}/offer")]
    Task<FeishuApiResult<GetApplicationOfferResult>?> GetApplicationOfferAsync(
        [Path] string application_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Offer 详情
    /// <para>根据 Offer ID 获取 Offer 详细信息（含薪资计划、发送记录与签署信息）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:offer:low_sensitive_info:readonly（查看 offer 的基础信息）/ hire:offer:readonly（获取 offer 信息）/ hire:offer（更新 offer 信息）。字段权限：hire:offer:readonly、hire:offer、contact:user.employee_id:readonly（remark、level、salary_plan 等敏感字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/offer/get">接口文档</see></para>
    /// </summary>
    /// <param name="offer_id">Offer ID，示例值：7085989097067563300</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 详情（offer：含 offer_type、offer_status、basic_info、salary_plan 等）</returns>
    [Get("/open-apis/hire/v1/offers/{offer_id}")]
    Task<FeishuApiResult<GetOfferResult>?> GetOfferAsync(
        [Path] string offer_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Offer 列表
    /// <para>根据人才 ID 分页获取 Offer 列表。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:offer:readonly（获取 offer 信息）或 hire:offer（更新 offer 信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/offer/list">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID（必填），示例值：6930815272790114324</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">每页数量，最大 200，默认 10</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id（将下线，不建议使用）</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/offers")]
    Task<FeishuApiResult<GetOfferListResult>?> GetOfferListAsync(
        [Query("talent_id")] string talent_id,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新 Offer 状态
    /// <para>通过 Offer ID 更新 Offer 的审批状态或发送和接受状态（对接 OA 场景）；需在飞书招聘 Offer 规则设置中开启对应 OA 开关，否则返回错误；仅自建应用可用。</para>
    /// <para>限频：100 次/分钟。所需权限：hire:offer（更新 offer 信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/offer/offer_status">接口文档</see></para>
    /// </summary>
    /// <param name="offer_id">Offer ID，示例值：7085989097067563300</param>
    /// <param name="request">请求体（offer_status 必填：2 审批中/3 审批已撤回/4 审批通过/5 审批不通过/6 已发送/7 被接受/8 被拒绝/9 已失效/10 已创建；6 时需 expiration_date，8 时需 termination_reason_id_list）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Patch("/open-apis/hire/v1/offers/{offer_id}/offer_status")]
    Task<FeishuNullDataApiResult?> ChangeOfferStatusAsync(
        [Path] string offer_id,
        [Body] ChangeOfferStatusRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新实习 Offer 入/离职状态
    /// <para>对「实习待入职」的实习 Offer 确认入职/放弃入职，或对「实习已入职」的实习 Offer 操作离职；仅自建应用可用。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:offer（更新 offer 信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/offer/intern_offer_status">接口文档</see></para>
    /// </summary>
    /// <param name="offer_id">实习 Offer ID，示例值：7085989097067563300</param>
    /// <param name="request">请求体（operation 必填：confirm_onboarding/cancel_onboarding/offboard；onboarding_info 在确认入职时必填，offboarding_info 在离职时必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回操作回显（offer_id、operation、onboarding_info、offboarding_info）</returns>
    [Post("/open-apis/hire/v1/offers/{offer_id}/intern_offer_status")]
    Task<FeishuApiResult<ChangeInternOfferStatusResult>?> ChangeInternOfferStatusAsync(
        [Path] string offer_id,
        [Body] ChangeInternOfferStatusRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试信息
    /// <para>按投递 ID、面试 ID 或面试开始时间区间筛选，分页获取面试列表；application_id、interview_id、start_time、end_time 不允许同时为空（查询参数采用查询对象模式 <see cref="InterviewListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/interview/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、投递/面试 ID、面试开始时间范围与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/interviews")]
    Task<FeishuApiResult<GetInterviewListResult>?> GetInterviewListAsync(
        [Query] InterviewListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才面试信息
    /// <para>按人才 ID 获取该人才下所有投递的全部面试信息（不分页）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly（user_id_type=user_id 时返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/interview/get_by_talent">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID（必填），示例值：6930815272790114324</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回按投递分组的面试列表（items：application_id、interview_list）</returns>
    [Get("/open-apis/hire/v1/interviews/get_by_talent")]
    Task<FeishuApiResult<GetInterviewByTalentResult>?> GetInterviewByTalentAsync(
        [Query("talent_id")] string talent_id,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试评价详细信息（v1）
    /// <para>按面试评价 ID 获取单条面试评价详情（结论、得分、面试官、题目与维度评价）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/interview/get">接口文档</see></para>
    /// </summary>
    /// <param name="interview_record_id">面试评价 ID，示例值：7047318856652261676</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试评价详情（interview_record）</returns>
    [Get("/open-apis/hire/v1/interview_records/{interview_record_id}")]
    Task<FeishuApiResult<GetInterviewRecordResult>?> GetInterviewRecordAsync(
        [Path] string interview_record_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试评价详细信息（新版 v2）
    /// <para>按面试评价 ID 获取新版结构详情（record_score 总得分与 module_assessments 模块评价）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/interview/get-3">接口文档</see></para>
    /// </summary>
    /// <param name="interview_record_id">面试评价 ID，示例值：7047318856652261676</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试评价详情 v2（interview_record）</returns>
    [Get("/open-apis/hire/v2/interview_records/{interview_record_id}")]
    Task<FeishuApiResult<GetInterviewRecordV2Result>?> GetInterviewRecordV2Async(
        [Path] string interview_record_id,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取面试评价详细信息（v1）
    /// <para>按评价 ID 列表或分页批量获取面试评价（v1 旧结构）；传入 ids 时不分页。</para>
    /// <para>限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/interview/list-3">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="ids">面试评价 ID 列表，最大 100 个；使用该筛选项时不分页</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试评价分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/interview_records")]
    Task<FeishuApiResult<GetInterviewRecordListResult>?> GetInterviewRecordListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("ids")] string[]? ids = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取面试评价详细信息（新版 v2）
    /// <para>按评价 ID 列表或分页批量获取面试评价（新版结构，含模块评价）；传入 ids 时不分页。</para>
    /// <para>限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。字段权限：contact:user.employee_id:readonly。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/interview/list-4">接口文档</see></para>
    /// </summary>
    /// <param name="ids">面试评价 ID 列表，长度 0～100；使用该筛选项时不分页</param>
    /// <param name="page_size">每页数量，0～100；不传时默认按 ids 参数获取数据</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试评价分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v2/interview_records")]
    Task<FeishuApiResult<GetInterviewRecordListV2Result>?> GetInterviewRecordListV2Async(
        [Query("ids")] string[]? ids = null,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试记录附件
    /// <para>获取面试记录 PDF 附件下载链接（含投递基本信息与面试评价信息）；不传 interview_record_id 时返回该投递下所有面试评价的附件。</para>
    /// <para>限频：20 次/分钟。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/interview/get-2">接口文档</see></para>
    /// </summary>
    /// <param name="application_id">投递 ID（必填），示例值：6701528341100366094</param>
    /// <param name="interview_record_id">面试评价 ID；不填则获取该投递下所有面试评价的附件</param>
    /// <param name="language">附件语言：1 中文（默认）/ 2 英文</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回附件下载信息（attachment：id、url 30 分钟有效、name、mime、create_time）</returns>
    [Get("/open-apis/hire/v1/interview_records/attachments")]
    Task<FeishuApiResult<GetInterviewRecordAttachmentResult>?> GetInterviewRecordAttachmentAsync(
        [Query("application_id")] string application_id,
        [Query("interview_record_id")] string? interview_record_id = null,
        [Query("language")] int? language = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试速记明细
    /// <para>获取指定面试的速记（逐句转写）明细记录；仅自建应用可用。</para>
    /// <para>限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/candidate-management/delivery-process-management/interview/get-4">接口文档</see></para>
    /// </summary>
    /// <param name="interview_id">面试 ID（必填），示例值：7047318856652261676</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">本次获取的语句最大数量，默认 20，范围 1～100</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回速记句子列表（minutes.sentences）与分页标记（page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/minutes")]
    Task<FeishuApiResult<GetInterviewMinutesResult>?> GetInterviewMinutesAsync(
        [Query("interview_id")] string interview_id,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试满意度问卷列表
    /// <para>批量获取面试满意度问卷（完成情况、题目及作答内容）；application_id 与 interview_id 不可同时填写（查询参数采用查询对象模式 <see cref="InterviewQuestionnaireListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:questionnaire:readonly（获取面试满意度问卷信息）或 hire:questionnaire（更新面试满意度问卷信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/interview/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、投递/面试 ID 与更新时间范围查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回满意度问卷分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/questionnaires")]
    Task<FeishuApiResult<GetInterviewQuestionnaireListResult>?> GetInterviewQuestionnaireListAsync(
        [Query] InterviewQuestionnaireListQuery? query = null,
        CancellationToken cancellationToken = default);
}
