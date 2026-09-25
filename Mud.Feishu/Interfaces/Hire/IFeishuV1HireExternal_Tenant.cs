// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）外部系统信息导入入口域 SDK 是一组服务端 OpenAPI 的封装，用于将外部系统（ATS/RMS）中的人才外部创建时间、外部投递、外部面试与面评、外部 Offer、外部背调以及内推奖励导入或同步到飞书招聘。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireExternal : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建人才外部信息
    /// <para>为人才创建外部系统信息（人才在外部系统的创建时间）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/create-5">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：7043758982146345223</param>
    /// <param name="request">创建请求体（external_create_time 必填：人才在外部系统的创建时间，毫秒时间戳）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才外部信息（external_info）</returns>
    [Post("/open-apis/hire/v1/talents/{talent_id}/external_info")]
    Task<FeishuApiResult<CreateTalentExternalInfoResult>?> CreateTalentExternalInfoAsync(
        [Path] string talent_id,
        [Body] TalentExternalInfoRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新人才外部信息
    /// <para>更新人才的外部系统信息（人才在外部系统的创建时间）。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:talent（更新人才信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/update">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：7043758982146345223</param>
    /// <param name="request">更新请求体（external_create_time 必填：人才在外部系统的创建时间，毫秒时间戳）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回人才外部信息（external_info）</returns>
    [Put("/open-apis/hire/v1/talents/{talent_id}/external_info")]
    Task<FeishuApiResult<UpdateTalentExternalInfoResult>?> UpdateTalentExternalInfoAsync(
        [Path] string talent_id,
        [Body] TalentExternalInfoRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建外部投递
    /// <para>创建来自外部系统的投递；external_id 为幂等字段，同一 external_id 24 小时内仅可创建一次。</para>
    /// <para>限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（talent_id 必填；external_id、职位/简历来源/阶段/终止原因/投递类型/时间字段选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部投递信息（external_application）</returns>
    [Post("/open-apis/hire/v1/external_applications")]
    Task<FeishuApiResult<CreateExternalApplicationResult>?> CreateExternalApplicationAsync(
        [Body] CreateExternalApplicationRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新外部投递
    /// <para>按外部投递 ID 覆盖更新外部投递的字段。</para>
    /// <para>限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/update-2">接口文档</see></para>
    /// </summary>
    /// <param name="external_application_id">外部投递 ID，示例值：6960663240925956660</param>
    /// <param name="request">更新请求体（job_recruitment_type、job_title、resume_source、stage、termination_reason、delivery_type、modify_time、create_time、termination_type 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部投递信息（external_application）</returns>
    [Put("/open-apis/hire/v1/external_applications/{external_application_id}")]
    Task<FeishuApiResult<UpdateExternalApplicationResult>?> UpdateExternalApplicationAsync(
        [Path] string external_application_id,
        [Body] UpdateExternalApplicationRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取外部投递列表
    /// <para>按人才 ID 分页获取外部投递信息列表。</para>
    /// <para>限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/list">接口文档</see></para>
    /// </summary>
    /// <param name="talent_id">人才 ID，示例值：6960663240925956660</param>
    /// <param name="page_size">每页数量，最大 20</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部投递分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/external_applications")]
    Task<FeishuApiResult<GetExternalApplicationListResult>?> GetExternalApplicationListAsync(
        [Query("talent_id")] string? talent_id = null,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除外部投递
    /// <para>按外部投递 ID 删除外部投递。</para>
    /// <para>限频：20 次/分钟。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/delete">接口文档</see></para>
    /// </summary>
    /// <param name="external_application_id">外部投递 ID，示例值：6960663240925956660</param>
    /// <param name="talent_id">人才 ID，示例值：6960663240925956660</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回被删除的外部投递信息（external_application）</returns>
    [Delete("/open-apis/hire/v1/external_applications/{external_application_id}")]
    Task<FeishuApiResult<DeleteExternalApplicationResult>?> DeleteExternalApplicationAsync(
        [Path] string external_application_id,
        [Query("talent_id")] string? talent_id = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建外部面试
    /// <para>创建来自外部系统的面试；external_id 为幂等字段，可携带面试评价列表。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/create-3">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（external_application_id 必填；external_id、participate_status、begin_time、end_time、interview_assessments 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部面试信息（external_interview）</returns>
    [Post("/open-apis/hire/v1/external_interviews")]
    Task<FeishuApiResult<CreateExternalInterviewResult>?> CreateExternalInterviewAsync(
        [Body] CreateExternalInterviewRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新外部面试
    /// <para>按外部面试 ID 覆盖更新外部面试的字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-interview-info/update">接口文档</see></para>
    /// </summary>
    /// <param name="external_interview_id">外部面试 ID，可通过查询外部面试列表接口获取，示例值：6960663240925956660</param>
    /// <param name="request">更新请求体（external_application_id 必填；participate_status、begin_time、end_time、interview_assessments 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部面试信息（external_interview）</returns>
    [Put("/open-apis/hire/v1/external_interviews/{external_interview_id}")]
    Task<FeishuApiResult<UpdateExternalInterviewResult>?> UpdateExternalInterviewAsync(
        [Path] string external_interview_id,
        [Body] UpdateExternalInterviewRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询外部面试列表
    /// <para>按外部投递 ID 或外部面试 ID 列表分页查询外部面试信息；传 external_interview_id_list 时以其为准。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）或 hire:external_application:readonly（查看外部投递）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-interview-info/batch_query">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（external_interview_id_list 最多 20 个，传入时以其为准）</param>
    /// <param name="external_application_id">外部投递 ID，示例值：6960663240925956660</param>
    /// <param name="page_size">每页数量，范围 1~20，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部面试分页列表（items、page_token、has_more）</returns>
    [Post("/open-apis/hire/v1/external_interviews/batch_query")]
    Task<FeishuApiResult<BatchQueryExternalInterviewResult>?> BatchQueryExternalInterviewAsync(
        [Body] BatchQueryExternalInterviewRequest request,
        [Query("external_application_id")] string? external_application_id = null,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除外部面试
    /// <para>按外部面试 ID 删除外部面试。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-interview-info/delete">接口文档</see></para>
    /// </summary>
    /// <param name="external_interview_id">外部面试 ID，示例值：6960663240925956660</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/external_interviews/{external_interview_id}")]
    Task<FeishuNullDataApiResult?> DeleteExternalInterviewAsync(
        [Path] string external_interview_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建外部面试评价
    /// <para>导入来自其他系统的面评信息，创建为外部面评；external_id 为幂等字段。</para>
    /// <para>限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/create-4">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（external_interview_id 必填；external_id、username、conclusion、assessment_dimension_list、content 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部面评信息（external_interview_assessment）</returns>
    [Post("/open-apis/hire/v1/external_interview_assessments")]
    Task<FeishuApiResult<CreateExternalInterviewAssessmentResult>?> CreateExternalInterviewAssessmentAsync(
        [Body] CreateExternalInterviewAssessmentRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新外部面试评价
    /// <para>按外部面评 ID 局部更新外部面评字段，留空的字段不更新。</para>
    /// <para>限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-interview-info/patch">接口文档</see></para>
    /// </summary>
    /// <param name="external_interview_assessment_id">外部面评 ID，示例值：6930815272790114324</param>
    /// <param name="request">更新请求体（username、conclusion、assessment_dimension_list、content 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部面评信息（external_interview_assessment）</returns>
    [Patch("/open-apis/hire/v1/external_interview_assessments/{external_interview_assessment_id}")]
    Task<FeishuApiResult<PatchExternalInterviewAssessmentResult>?> PatchExternalInterviewAssessmentAsync(
        [Path] string external_interview_assessment_id,
        [Body] PatchExternalInterviewAssessmentRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建外部 Offer
    /// <para>从其他系统导入 Offer 信息并创建为外部 Offer；external_id 为幂等字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_offer（更新外部 Offer）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-offer-info/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（external_application_id 必填；external_id、biz_create_time、owner、offer_status、attachment_id_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部 Offer 信息（external_offer）</returns>
    [Post("/open-apis/hire/v1/external_offers")]
    Task<FeishuApiResult<CreateExternalOfferResult>?> CreateExternalOfferAsync(
        [Body] CreateExternalOfferRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新外部 Offer
    /// <para>按外部 Offer ID 覆盖更新外部 Offer 的字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_offer（更新外部 Offer）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-offer-info/update">接口文档</see></para>
    /// </summary>
    /// <param name="external_offer_id">外部 Offer ID，可通过查询外部 Offer 列表接口获取，示例值：6960663240925956660</param>
    /// <param name="request">更新请求体（external_application_id 必填；biz_create_time、owner、offer_status、attachment_id_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部 Offer 信息（external_offer）</returns>
    [Put("/open-apis/hire/v1/external_offers/{external_offer_id}")]
    Task<FeishuApiResult<UpdateExternalOfferResult>?> UpdateExternalOfferAsync(
        [Path] string external_offer_id,
        [Body] UpdateExternalOfferRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询外部 Offer 列表
    /// <para>按外部投递 ID 或外部 Offer ID 列表分页查询外部 Offer 信息；传 external_offer_id_list 时以其为准。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_offer（更新外部 Offer）或 hire:external_offer:readonly（查看外部 Offer）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-offer-info/batch_query">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（external_offer_id_list 最多 20 个，传入时以其为准）</param>
    /// <param name="external_application_id">外部投递 ID，示例值：6960663240925956660</param>
    /// <param name="page_size">每页数量，最大 20，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部 Offer 分页列表（items、page_token、has_more）</returns>
    [Post("/open-apis/hire/v1/external_offers/batch_query")]
    Task<FeishuApiResult<BatchQueryExternalOfferResult>?> BatchQueryExternalOfferAsync(
        [Body] BatchQueryExternalOfferRequest request,
        [Query("external_application_id")] string? external_application_id = null,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除外部 Offer
    /// <para>按外部 Offer ID 删除外部 Offer。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-offer-info/delete">接口文档</see></para>
    /// </summary>
    /// <param name="external_offer_id">外部 Offer ID，示例值：6960663240925956660</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/external_offers/{external_offer_id}")]
    Task<FeishuNullDataApiResult?> DeleteExternalOfferAsync(
        [Path] string external_offer_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建外部背调
    /// <para>导入来自外部系统的背调信息；external_id 为幂等字段，同一 external_id 24 小时内仅可创建一次。</para>
    /// <para>限频：20 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/get-candidates/import-external-system-information/create-2">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（external_application_id 必填；external_id、date、name、result、attachment_id_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部背调信息（external_background_check）</returns>
    [Post("/open-apis/hire/v1/external_background_checks")]
    Task<FeishuApiResult<CreateExternalBackgroundCheckResult>?> CreateExternalBackgroundCheckAsync(
        [Body] CreateExternalBackgroundCheckRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新外部背调
    /// <para>按外部背调 ID 覆盖更新外部背调的字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-background-info/update">接口文档</see></para>
    /// </summary>
    /// <param name="external_background_check_id">外部背调 ID，可通过查询外部背调列表接口获取，示例值：6960663240925956660</param>
    /// <param name="request">更新请求体（external_application_id 必填；date、name、result、attachment_id_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部背调信息（external_background_check）</returns>
    [Put("/open-apis/hire/v1/external_background_checks/{external_background_check_id}")]
    Task<FeishuApiResult<UpdateExternalBackgroundCheckResult>?> UpdateExternalBackgroundCheckAsync(
        [Path] string external_background_check_id,
        [Body] UpdateExternalBackgroundCheckRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 查询外部背调列表
    /// <para>按外部投递 ID 或外部背调 ID 列表分页查询外部背调信息；传 external_background_check_id_list 时以其为准。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）或 hire:external_application:readonly（查看外部投递）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-background-info/batch_query">接口文档</see></para>
    /// </summary>
    /// <param name="request">查询请求体（external_background_check_id_list 最多 20 个，传入时以其为准）</param>
    /// <param name="external_application_id">外部投递 ID，示例值：6960663240925956660</param>
    /// <param name="page_size">每页数量，范围 1~20，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回外部背调分页列表（items、page_token、has_more）</returns>
    [Post("/open-apis/hire/v1/external_background_checks/batch_query")]
    Task<FeishuApiResult<BatchQueryExternalBackgroundCheckResult>?> BatchQueryExternalBackgroundCheckAsync(
        [Body] BatchQueryExternalBackgroundCheckRequest request,
        [Query("external_application_id")] string? external_application_id = null,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除外部背调
    /// <para>按外部背调 ID 删除外部背调。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_application（更新外部投递信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-background-info/delete">接口文档</see></para>
    /// </summary>
    /// <param name="external_background_check_id">外部背调 ID，示例值：6960663240925956660</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/external_background_checks/{external_background_check_id}")]
    Task<FeishuNullDataApiResult?> DeleteExternalBackgroundCheckAsync(
        [Path] string external_background_check_id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 导入外部内推奖励
    /// <para>将外部系统的内推奖励（积分/现金）导入到招聘的「内推账号」；external_id 为幂等字段。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_referral_reward（导入内推奖励信息）。字段权限：contact:user.employee_id:readonly（取 user_id 时必填）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-referral-reward-info/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">导入请求体（referral_user_id、external_id、rule_type、bonus、stage 必填；application_id 与 talent_id 二选一，其余选填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回创建的内推奖励 ID（id）</returns>
    [Post("/open-apis/hire/v1/external_referral_rewards")]
    Task<FeishuApiResult<CreateExternalReferralRewardResult>?> CreateExternalReferralRewardAsync(
        [Body] CreateExternalReferralRewardRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除外部内推奖励
    /// <para>按 ID 删除导入的外部内推奖励，删除后招聘系统「内推奖励管理」中的对应明细会消失；删除「已确认/已发放」奖励前请先与相关内推人沟通。</para>
    /// <para>限频：10 次/秒。所需权限：hire:external_referral_reward（导入内推奖励信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/get-candidates/import-external-system-information/import-external-referral-reward-info/delete">接口文档</see></para>
    /// </summary>
    /// <param name="external_referral_reward_id">内推奖励 ID，示例值：6930815272790114324</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Delete("/open-apis/hire/v1/external_referral_rewards/{external_referral_reward_id}")]
    Task<FeishuNullDataApiResult?> DeleteExternalReferralRewardAsync(
        [Path] string external_referral_reward_id,
        CancellationToken cancellationToken = default);
}
