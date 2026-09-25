// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）Offer 域 SDK 是一组服务端 OpenAPI 的封装，用于 Offer 申请表列表与模板 Schema 查询、申请表自定义字段更新、Offer 审批模板查询，以及投递流程中 Offer 的创建、更新、查询（按 Offer ID/按投递 ID）、列表、状态变更与实习 Offer 入/离职状态维护。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_application_form/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireOffer : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取 Offer 申请表列表
    /// <para>分页获取 Offer 申请表列表，返回申请表 ID、名称与创建时间。</para>
    /// <para>限频：10 次/秒。所需权限：hire:offer_schema:readonly（获取 Offer 申请表信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_application_form/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，默认 1</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 申请表分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/offer_application_forms")]
    Task<FeishuApiResult<GetOfferApplicationFormListResult>?> GetOfferApplicationFormListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Offer 申请表模板信息
    /// <para>按申请表 ID 获取 Offer 申请表模板的完整 Schema 信息，包括模块、字段、选项、公式与联动显示配置。公式（formula）类型字段由其他字段计算得出，创建 Offer 时无需也不可传入。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:offer_schema:readonly（获取 Offer 申请表信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_application_form/get">接口文档</see></para>
    /// </summary>
    /// <param name="offer_application_form_id">Offer 申请表 ID，示例值：237186812432</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 申请表详情（offer_apply_form）</returns>
    [Get("/open-apis/hire/v1/offer_application_forms/{offer_application_form_id}")]
    Task<FeishuApiResult<GetOfferApplicationFormResult>?> GetOfferApplicationFormAsync(
        [Path] string offer_application_form_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新 Offer 申请表自定义字段
    /// <para>更新 Offer 申请表自定义字段的名称与选项配置；不支持修改字段类型，公式类型字段不支持更新。每次更新后所有申请表的 schema_id 会升级为新版本。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:offer_selection_object（更新 Offer 自定义字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_application_form/update">接口文档</see></para>
    /// </summary>
    /// <param name="offer_custom_field_id">Offer 申请表自定义字段 ID，可通过获取 Offer 申请表模板信息接口获取，示例值：6906755946257615112</param>
    /// <param name="request">更新请求体（name 必填，zh_cn/en_us 至少一个；仅单选/多选字段需传 config.options）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Put("/open-apis/hire/v1/offer_custom_fields/{offer_custom_field_id}")]
    Task<FeishuNullDataApiResult?> UpdateOfferCustomFieldAsync(
        [Path] string offer_custom_field_id,
        [Body] UpdateOfferCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Offer 审批模板列表
    /// <para>分页获取 Offer 审批模板列表，返回模板名称、创建时间、备注与适用部门。</para>
    /// <para>限频：10 次/秒。所需权限：hire:offer_approval_template:readonly（获取 Offer 审批流程配置）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/offer-settings/offer_approval_template/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 200，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="department_id_type">部门 ID 类型（open_department_id/department_id/people_admin_department_id），默认 people_admin_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Offer 审批模板分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/offer_approval_templates")]
    Task<FeishuApiResult<GetOfferApprovalTemplateListResult>?> GetOfferApprovalTemplateListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("department_id_type")] string? department_id_type = null,
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
}
