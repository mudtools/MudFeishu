// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）面试域 SDK 是一组服务端 OpenAPI 的封装，用于面试轮次类型、面试反馈表、面试登记表模板的查询、面试官认证信息的维护，以及投递流程中的面试信息与面试评价（v1/v2）、面试记录附件、面试速记明细与面试满意度问卷的获取。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/interview-settings/list-2"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireInterview : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取面试轮次类型列表
    /// <para>获取面试轮次类型列表，返回轮次类型名称、流程类型、启用状态及关联的面试评价表。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/interview-settings/list-2">接口文档</see></para>
    /// </summary>
    /// <param name="process_type">职位流程类型：1 社会招聘流程 / 2 校园招聘流程</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试轮次类型列表（items）与启用状态（active_status）</returns>
    [Get("/open-apis/hire/v1/interview_round_types")]
    Task<FeishuApiResult<GetInterviewRoundTypeListResult>?> GetInterviewRoundTypeListAsync(
        [Query("process_type")] int? process_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试反馈表
    /// <para>分页获取面试反馈表详情，包括问题描述、问题选项、打分配置等；传入 interview_feedback_form_ids 时按 ID 精确查询并忽略其他参数。</para>
    /// <para>限频：10 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/recruitment-related-configuration/interview-settings/list-3">接口文档</see></para>
    /// </summary>
    /// <param name="interview_feedback_form_ids">面试反馈表 ID 列表，传入该参数时其他查询参数被忽略，最多 100 个</param>
    /// <param name="page_size">每页数量</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试反馈表分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/interview_feedback_forms")]
    Task<FeishuApiResult<GetInterviewFeedbackFormListResult>?> GetInterviewFeedbackFormListAsync(
        [Query("interview_feedback_form_ids")] string[]? interview_feedback_form_ids = null,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试登记表模板列表
    /// <para>分页获取面试登记表模板列表，返回模板名称、是否作为全局面试登记表及模块字段配置。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:interview:readonly（获取面试信息）或 hire:interview（更新面试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-related-configuration/interview-settings/list-3">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">每页数量，最大 10，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试登记表模板分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/interview_registration_schemas")]
    Task<FeishuApiResult<GetInterviewRegistrationSchemaListResult>?> GetInterviewRegistrationSchemaListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取面试官信息列表
    /// <para>分页查询面试官认证信息；仅返回已通过「更新面试官信息」接口写入的数据，未写入的用户默认不返回（查询参数采用查询对象模式 <see cref="InterviewerListQuery"/>，见 AGENTS.md API-2）。默认按更新时间、user_id 排序。</para>
    /// <para>限频：20 次/秒。所需权限：hire:interviewer（管理面试官信息）或 hire:interviewer:readonly（获取面试官信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/recruitment-related-configuration/interview-settings/interviewer/list">接口文档</see></para>
    /// </summary>
    /// <param name="query">分页、面试官 user_id 列表、认证状态、更新时间范围与用户 ID 类型查询参数</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回面试官信息分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/hire/v1/interviewers")]
    Task<FeishuApiResult<GetInterviewerListResult>?> GetInterviewerListAsync(
        [Query] InterviewerListQuery? query = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新面试官信息
    /// <para>更新指定面试官的认证状态（1 未认证 / 2 已认证）。</para>
    /// <para>限频：20 次/秒。所需权限：hire:interviewer（管理面试官信息）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。</para>
    /// <para><see href="https://open.feishu.cn/document/hire-v1/recruitment-related-configuration/interview-settings/interviewer/patch">接口文档</see></para>
    /// </summary>
    /// <param name="interviewer_id">面试官 userID，示例值：ou_7dab8a3d3cdcc9da365777c7ad535d62</param>
    /// <param name="request">更新请求体（interviewer 必填，含 verify_status 认证状态）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的面试官信息（interviewer）</returns>
    [Patch("/open-apis/hire/v1/interviewers/{interviewer_id}")]
    Task<FeishuApiResult<PatchInterviewerResult>?> PatchInterviewerAsync(
        [Path] string interviewer_id,
        [Body] PatchInterviewerRequest request,
        [Query("user_id_type")] string? user_id_type = null,
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
