// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）设置与字典域 SDK 是一组服务端 OpenAPI 的封装，用于招聘流程、科目、信息登记表模板、人才标签、地点、角色与用户角色等基础配置/字典资源的查询。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireSetting : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取招聘流程列表
    /// <para>分页获取全部招聘流程信息，包括流程名称、流程类型以及各阶段的名称与类型。</para>
    /// <para>限频：20 次/秒。所需权限：hire:job_process:readonly（获取招聘流程信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job_process/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回招聘流程分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/job_processes")]
    Task<FeishuApiResult<GetJobProcessListResult>?> GetJobProcessListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取科目列表
    /// <para>分页获取招聘科目列表，返回科目名称、启用状态与创建人等信息。</para>
    /// <para>限频：特殊限频。所需权限：hire:subject:readonly（获取招聘项目信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/subject/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 200，默认 1</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回科目分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/subjects")]
    Task<FeishuApiResult<GetSubjectListResult>?> GetSubjectListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取信息登记表模板列表
    /// <para>分页获取信息登记表模板列表，可按适用场景（面试/入职/信息更新登记表）筛选，返回模板内模块与字段配置。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent:readonly（获取人才信息）或 hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/recruitment-related-configuration/application/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 50，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="scenario">登记表适用场景：5 面试登记表 / 6 入职登记表 / 14 信息更新登记表；不传表示获取全部类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回信息登记表模板分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/registration_schemas")]
    Task<FeishuApiResult<GetRegistrationSchemaListResult>?> GetRegistrationSchemaListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("scenario")] int? scenario = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取人才标签列表
    /// <para>按关键词、ID 列表、标签类型与是否包含停用等条件分页查询人才标签，按创建时间倒序排列（查询参数采用查询对象模式 <see cref="TalentTagListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:talent_tag（更新人才标签）或 hire:talent_tag:readonly（获取人才标签）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/recruitment-related-configuration/application/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="query">关键词、ID 列表、标签类型、启停状态与分页查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才标签分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/talent_tags")]
    Task<FeishuApiResult<GetTalentTagListResult>?> GetTalentTagListAsync(
        [Query] TalentTagListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询地点列表
    /// <para>根据地点类型（国家/省份/城市/区县）与地点码批量查询地点信息，获取地点名称（中文、英文、拼音）。</para>
    /// <para>限频：5 次/秒。所需权限：hire:location:readonly（获取地点信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/location/query">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（location_type 必填：1 国家 / 2 省份 / 3 城市 / 4 区县；code_list 地点码列表，可选，最大 100 个，不填则查询全部）</param>
    /// <param name="page_size">每页数量，必填，取值范围 1～100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回地点信息分页列表（items、has_more、page_token）</returns>
    [Post("/open-apis/hire/v1/locations/query")]
    Task<FeishuApiResult<QueryLocationResult>?> QueryLocationAsync(
        [Body] QueryLocationRequest request,
        [Query("page_size")] int page_size,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取地点列表
    /// <para>获取飞书招聘内置的工作地/面试地地点列表，返回区县、城市、省份、国家的层级编码与名称。</para>
    /// <para>限频：特殊限频（详见接口文档）。所需权限：hire:location:readonly（获取地点信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/location/list">接口文档</see></para>
    /// </summary>
    /// <param name="usage">地点用途，必填：position_location（工作地）/ interview_location（面试地）</param>
    /// <param name="page_size">每页数量，取值范围 1～100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回地点分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/locations")]
    Task<FeishuApiResult<GetLocationListResult>?> GetLocationListAsync(
        [Query("usage")] string usage,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取角色信息
    /// <para>按角色 ID 获取角色详情，包括角色名称、描述、适用范围与社招/校招权限配置。</para>
    /// <para>限频：10 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/get">接口文档</see></para>
    /// </summary>
    /// <param name="role_id">角色 ID，可通过获取角色列表接口获取，示例值：7350589232462807068</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回角色详情（role）</returns>
    [Get("/open-apis/hire/v1/roles/{role_id}")]
    Task<FeishuApiResult<GetRoleResult>?> GetRoleAsync(
        [Path] string role_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取角色列表
    /// <para>分页获取企业内飞书招聘角色列表，返回角色名称、描述与适用范围。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 200</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回角色分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/roles")]
    Task<FeishuApiResult<GetRoleListResult>?> GetRoleListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取用户角色列表
    /// <para>按用户、角色或更新时间分页查询用户角色分配关系，返回角色名称与业务管理范围（查询参数采用查询对象模式 <see cref="UserRoleListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:auth:readonly（获取权限信息）或 hire:auth（更新权限信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/auth/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、用户、角色、更新时间范围与用户 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回用户角色分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/user_roles")]
    Task<FeishuApiResult<GetUserRoleListResult>?> GetUserRoleListAsync(
        [Query] UserRoleListQuery? query = null,
        CancellationToken cancellationToken = default);
}
