// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）投递流程管理域 SDK 是一组服务端 OpenAPI 的封装，用于背调订单查询、三方协议维护、候选人入职与取消入职、员工入职信息查询与状态更新（转正/离职等）以及 e-HR 导入任务结果回写。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/delivery-process-management/background_check_order/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireApplication : IFeishuAppContextSwitcher
{
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
}
