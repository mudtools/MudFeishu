// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）候选人入口域 SDK 是一组服务端 OpenAPI 的封装，用于内推信息与内推官网职位查询、招聘官网（列表/推广渠道/官网用户/官网职位）、官网投递创建与投递任务查询以及官网申请表模板查询。本接口全部端点仅支持 tenant_access_token 调用。
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
}
