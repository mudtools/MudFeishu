// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）招聘需求 SDK 是一组服务端 OpenAPI 的封装，用于创建/更新/删除招聘需求、按 ID 或编号批量获取招聘需求、分页获取招聘需求列表以及获取招聘需求模板。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireJobRequirement : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建招聘需求
    /// <para>创建招聘需求；除招聘需求编号（short_code）必填外，其余字段是否必填以飞书招聘「招聘需求字段管理」设置为准。</para>
    /// <para>限频：5 次/秒。所需权限：hire:job_requirement（更新招聘需求）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job_requirement/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（short_code、name、display_progress、head_count 必填；recruitment_type_id 与 employee_type_id 必填其一）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回创建后的招聘需求信息（job_requirement）</returns>
    [Post("/open-apis/hire/v1/job_requirements")]
    Task<FeishuApiResult<CreateJobRequirementResult>?> CreateJobRequirementAsync(
        [Body] CreateJobRequirementRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新招聘需求
    /// <para>按招聘需求 ID 更新需求名称、需求状态、需求人数等信息（审批中的招聘需求不可更新）。除文档标注必填字段外，其余字段是否必填以飞书招聘「招聘需求字段管理」设置为准。</para>
    /// <para>限频：5 次/秒。所需权限：hire:job_requirement（更新招聘需求）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job_requirement/update">接口文档</see></para>
    /// </summary>
    /// <param name="job_requirement_id">招聘需求 ID，示例值：623455234</param>
    /// <param name="request">更新请求体（name、display_progress、head_count 必填；update_option 控制是否同步修改关联职位）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/hire/v1/job_requirements/{job_requirement_id}")]
    Task<FeishuNullDataApiResult?> UpdateJobRequirementAsync(
        [Path] string job_requirement_id,
        [Body] UpdateJobRequirementRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取招聘需求信息（按 ID 批量查询）
    /// <para>按招聘需求 ID 列表或需求编号列表批量获取招聘需求信息，单次最多 100 条；两种列表不可同时传入。审批中的招聘需求不返回。</para>
    /// <para>限频：10 次/秒。所需权限：hire:job_requirement:readonly（获取招聘需求）或 hire:job_requirement（更新招聘需求）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job_requirement/list_by_id">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（id_list 招聘需求 ID 列表与 short_code_list 需求编号列表二选一，单次上限 100 条；均不传时返回空）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id），默认 open_department_id</param>
    /// <param name="job_level_id_type">职级 ID 类型（people_admin_job_level_id/job_level_id），默认 people_admin_job_level_id</param>
    /// <param name="job_family_id_type">职位序列 ID 类型（people_admin_job_category_id/job_family_id），默认 people_admin_job_category_id</param>
    /// <param name="employee_type_id_type">人员类型 ID 类型（people_admin_employee_type_id/employee_type_enum_id），默认 people_admin_employee_type_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回招聘需求列表（items）</returns>
    [Post("/open-apis/hire/v1/job_requirements/search")]
    Task<FeishuApiResult<SearchJobRequirementResult>?> SearchJobRequirementAsync(
        [Body] SearchJobRequirementRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        [Query("department_id_type")] string? department_id_type = null,
        [Query("job_level_id_type")] string? job_level_id_type = null,
        [Query("job_family_id_type")] string? job_family_id_type = null,
        [Query("employee_type_id_type")] string? employee_type_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取招聘需求列表
    /// <para>按职位 ID、创建/更新时间范围等条件分页查询招聘需求列表（查询参数采用查询对象模式 <see cref="JobRequirementListQuery"/>，见 AGENTS.md API-2）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:job_requirement:readonly（获取招聘需求）或 hire:job_requirement（更新招聘需求）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job_requirement/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、职位 ID、创建/更新时间范围与各类 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回招聘需求分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/job_requirements")]
    Task<FeishuApiResult<GetJobRequirementListResult>?> GetJobRequirementListAsync(
        [Query] JobRequirementListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除招聘需求
    /// <para>删除指定招聘需求；已关联职位的招聘需求不可删除。</para>
    /// <para>限频：10 次/秒。所需权限：hire:job_requirement（更新招聘需求）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job_requirement/delete">接口文档</see></para>
    /// </summary>
    /// <param name="job_requirement_id">招聘需求 ID，示例值：1616161616</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/job_requirements/{job_requirement_id}")]
    Task<FeishuNullDataApiResult?> DeleteJobRequirementAsync(
        [Path] string job_requirement_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取招聘需求模板
    /// <para>分页获取招聘需求模板列表，返回模板内各模块与字段的配置（含选项、是否必填等）。</para>
    /// <para>限频：10 次/秒。所需权限：hire:job_requirement:readonly（获取招聘需求）或 hire:job_requirement（更新招聘需求）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/job_requirement/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回招聘需求模板分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/job_requirement_schemas")]
    Task<FeishuApiResult<GetJobRequirementSchemaListResult>?> GetJobRequirementSchemaListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);
}
