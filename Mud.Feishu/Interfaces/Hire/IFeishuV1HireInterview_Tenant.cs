// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）面试域 SDK 是一组服务端 OpenAPI 的封装，用于招聘配置中的面试轮次类型、面试反馈表、面试登记表模板的查询与面试官认证信息的维护。本接口全部端点仅支持 tenant_access_token 调用（投递流程中的面试信息与评价记录等见 <see cref="IFeishuTenantV1HireCandidate"/>）。
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
}
