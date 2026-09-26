// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）生态对接入口域 SDK 是一组服务端 OpenAPI 的封装，用于背调/笔试服务商与飞书招聘的生态对接：账号自定义字段的创建/更新/删除、背调订单进度与最终结果回传、背调自定义字段与背调套餐/附加调查项维护、笔试安排结果与笔试结果回传、试卷列表维护。本接口全部端点仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/ecological-docking/summary"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HireEco : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建账号自定义字段
    /// <para>飞书招聘的背调或笔试服务商可通过此接口创建账号自定义字段，用来标识飞书招聘客户在服务商处的身份（如客户在服务商处的租户 ID、账号 ID 等）。本接口为全量更新，多次调用时以最后一次传入的自定义字段为准。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）或 hire:exam（更新笔试信息）。自建应用须先在「飞书招聘」-「设置」-「生态对接」-「笔试/背景调查」添加后才可调用。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_account_custom_field/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（scope 必填：1 背调/2 笔试；custom_field_list 必填：key、name、is_required 必填，description 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_account_custom_fields")]
    Task<FeishuNullDataApiResult?> CreateEcoAccountCustomFieldAsync(
        [Body] EcoAccountCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新账号自定义字段
    /// <para>飞书招聘的背调或笔试服务商可通过此接口更新账号自定义字段（如客户在服务商处的租户 ID、账号 ID 等）的名称和描述；更新仅影响添加新账号时的表单展示，不影响已有账号的字段值。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）或 hire:exam（更新笔试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_account_custom_field/batch_update">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（scope 必填；custom_field_list 必填，key 须为当前 scope 下已存在的自定义字段标识）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Patch("/open-apis/hire/v1/eco_account_custom_fields/batch_update")]
    Task<FeishuNullDataApiResult?> BatchUpdateEcoAccountCustomFieldAsync(
        [Body] EcoAccountCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除账号自定义字段
    /// <para>飞书招聘的背调或笔试服务商可通过此接口删除账号自定义字段。删除不可撤销且 key 不可复用；不支持一次删除全部字段（须至少保留一个）。删除后已有账号的字段值不受影响。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）或 hire:exam（更新笔试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_account_custom_field/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="request">删除请求体（scope 必填；custom_field_key_list 选填：要删除的自定义字段 key 列表）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_account_custom_fields/batch_delete")]
    Task<FeishuNullDataApiResult?> BatchDeleteEcoAccountCustomFieldAsync(
        [Body] BatchDeleteEcoAccountCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 终止背调订单
    /// <para>调用此接口会将背调订单状态置为「已终止」；已终止订单无法再通过更新进度、回传最终结果接口变更。建议先调用更新进度接口将订单进度更新为「已终止」。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check/cancel">接口文档</see></para>
    /// </summary>
    /// <param name="request">终止请求体（background_check_id 必填：背调 ID，通过「创建背调」事件获取）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_background_checks/cancel")]
    Task<FeishuNullDataApiResult?> CancelEcoBackgroundCheckAsync(
        [Body] CancelEcoBackgroundCheckRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新背调订单进度
    /// <para>更新指定背调订单的进度信息和阶段性报告；进度信息将展示在「飞书招聘」-「投递详情页」-「背调卡片」上。订单状态为已完成时不可再更新。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check/update_progress">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（background_check_id、stage_id、stage_name、stage_time 必填；stage_en_name、result、operator_role、report_file_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_background_checks/update_progress")]
    Task<FeishuNullDataApiResult?> UpdateProgressEcoBackgroundCheckAsync(
        [Body] UpdateProgressEcoBackgroundCheckRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 回传背调订单的最终结果
    /// <para>回传背调的最终结果和终版报告。若租户未启用背调报告审批，订单状态直接变为「已完成」；若启用审批，则审批通过后流转为「已完成」。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check/update_result">接口文档</see></para>
    /// </summary>
    /// <param name="request">回传请求体（background_check_id、result、result_time 必填；operator_role、report_file_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_background_checks/update_result")]
    Task<FeishuNullDataApiResult?> UpdateResultEcoBackgroundCheckAsync(
        [Body] UpdateResultEcoBackgroundCheckRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建背调自定义字段
    /// <para>创建用户在发起背调时展示的表单自定义字段，支持单行文本、单选等多种类型，可设置必填/非必填。本接口为全量更新，多次调用时以最后一次传入的自定义字段为准。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check_custom_field/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（account_id 必填；custom_field_list 必填：type、key、name、is_required 必填，description、options 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_background_check_custom_fields")]
    Task<FeishuNullDataApiResult?> CreateEcoBackgroundCheckCustomFieldAsync(
        [Body] EcoBackgroundCheckCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新背调自定义字段
    /// <para>更新用户发起背调时展示的表单自定义字段的名称和描述。仅允许更新已有字段的名称与描述，不支持变更字段类型，也不允许新增或删除字段。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check_custom_field/batch_update">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（account_id 必填；custom_field_list 必填，列表长度须与创建时一致）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Patch("/open-apis/hire/v1/eco_background_check_custom_fields/batch_update")]
    Task<FeishuNullDataApiResult?> BatchUpdateEcoBackgroundCheckCustomFieldAsync(
        [Body] EcoBackgroundCheckCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除背调自定义字段
    /// <para>删除用户发起背调时展示的表单自定义字段。删除不影响已创建的背调；被删除字段的 key 不可重复使用。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check_custom_field/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="request">删除请求体（account_id 必填：背调账号 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_background_check_custom_fields/batch_delete")]
    Task<FeishuNullDataApiResult?> BatchDeleteEcoBackgroundCheckCustomFieldAsync(
        [Body] BatchDeleteEcoBackgroundCheckCustomFieldRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建背调套餐和附加调查项
    /// <para>在指定背调账号下创建可用的背调套餐列表和附加调查项列表。本接口为增量创建，每次调用在原有列表基础上新增；已创建过的套餐 ID 与附加调查项 ID 不可重复创建。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check_package/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（account_id 必填；package_list 必填：id、name 必填，description 选填；additional_item_list 选填：id、name 必填，description 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_background_check_packages")]
    Task<FeishuNullDataApiResult?> CreateEcoBackgroundCheckPackageAsync(
        [Body] EcoBackgroundCheckPackageRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新背调套餐和附加调查项
    /// <para>更新指定背调账号下的背调套餐和附加调查项信息；新增请使用创建接口。更新将影响已发起背调订单的表单项展示。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check_package/batch_update">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（account_id 必填；package_list 必填：id 须为账号下已有套餐 ID；additional_item_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Patch("/open-apis/hire/v1/eco_background_check_packages/batch_update")]
    Task<FeishuNullDataApiResult?> BatchUpdateEcoBackgroundCheckPackageAsync(
        [Body] EcoBackgroundCheckPackageRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除背调套餐和附加调查项
    /// <para>删除背调账号下的背调套餐和附加调查项信息；删除操作不会影响已创建的背调。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:background_check_order（更新背调信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_background_check_package/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="request">删除请求体（account_id 必填；package_id_list、additional_item_id_list 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_background_check_packages/batch_delete")]
    Task<FeishuNullDataApiResult?> BatchDeleteEcoBackgroundCheckPackageAsync(
        [Body] BatchDeleteEcoBackgroundCheckPackageRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 回传笔试安排结果
    /// <para>飞书招聘的笔试服务商在收到「创建笔试」事件并安排笔试后，通过本接口回传笔试安排结果；安排成功须返回笔试链接，若链接需登录鉴权还须返回登录凭证。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:exam（更新笔试信息）或 hire:exam:readonly（获取笔试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_exam/login_info">接口文档</see></para>
    /// </summary>
    /// <param name="exam_id">笔试 ID，通过「创建笔试」事件获取，示例值：7178536692385679677</param>
    /// <param name="request">回传请求体（result、msg 选填：0 表示成功；exam_login_info 必填：exam_url 必填，username/password 按需返回）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_exams/{exam_id}/login_info")]
    Task<FeishuNullDataApiResult?> LoginInfoEcoExamAsync(
        [Path] string exam_id,
        [Body] LoginInfoEcoExamRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 回传笔试结果
    /// <para>飞书招聘的笔试服务商可通过此接口回传候选人的笔试结果；回传后候选人在飞书招聘内的笔试状态变为「已作答」。本接口支持重复调用（覆盖前序结果），但客户已完成阅卷后不可再更新。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:exam（更新笔试信息）或 hire:exam:readonly（获取笔试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_exam/update_result">接口文档</see></para>
    /// </summary>
    /// <param name="exam_id">笔试 ID，通过「创建笔试」事件获取，示例值：7178536692385679677</param>
    /// <param name="request">回传请求体（result 必填：笔试成绩；result_time、report_list、detail_list、status 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_exams/{exam_id}/update_result")]
    Task<FeishuNullDataApiResult?> UpdateResultEcoExamAsync(
        [Path] string exam_id,
        [Body] UpdateResultEcoExamRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建试卷列表
    /// <para>飞书招聘的笔试服务商完成账号绑定后，可通过本接口在客户的笔试账号下创建试卷列表；客户账号为「未激活」/「停用」时，创建成功后账号将变为「正常」。本接口为全量更新，多次调用时以最后一次传入的试卷列表为准。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:exam（更新笔试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_exam_paper/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建请求体（account_id 必填；paper_list 必填：id、name 必填，duration、question_count、start_time、end_time 选填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_exam_papers")]
    Task<FeishuNullDataApiResult?> CreateEcoExamPaperAsync(
        [Body] EcoExamPaperRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新试卷列表
    /// <para>飞书招聘的笔试服务商可通过此接口更新客户笔试账号下的试卷列表。仅支持按试卷 ID 修改已有试卷（不支持新增/删除），本接口为全量更新，字段值为空则原有值将被清空。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:exam（更新笔试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_exam_paper/batch_update">接口文档</see></para>
    /// </summary>
    /// <param name="request">更新请求体（account_id 必填；paper_list 必填：id 须为创建时传入的试卷 ID）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Patch("/open-apis/hire/v1/eco_exam_papers/batch_update")]
    Task<FeishuNullDataApiResult?> BatchUpdateEcoExamPaperAsync(
        [Body] EcoExamPaperRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除试卷列表
    /// <para>飞书招聘的笔试服务商可通过此接口删除客户笔试账号下的试卷列表。删除不影响已安排的笔试，删除不存在的试卷不会报错。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：hire:exam（更新笔试信息）。</para>
    /// <para><see href="https://open.feishu.cn/document/ukTMukTMukTM/uMzM1YjLzMTN24yMzUjN/hire-v1/eco_exam_paper/batch_delete">接口文档</see></para>
    /// </summary>
    /// <param name="request">删除请求体（account_id 必填；paper_id_list 必填：要删除的试卷 ID 列表）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>成功时 data 为空对象</returns>
    [Post("/open-apis/hire/v1/eco_exam_papers/batch_delete")]
    Task<FeishuNullDataApiResult?> BatchDeleteEcoExamPaperAsync(
        [Body] BatchDeleteEcoExamPaperRequest request,
        CancellationToken cancellationToken = default);
}
