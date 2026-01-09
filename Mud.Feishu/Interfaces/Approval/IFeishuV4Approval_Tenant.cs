// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Approval;

namespace Mud.Feishu;

/// <summary>
/// 原生审批：根据企业业务需要在飞书审批中心创建审批定义，用来定义一类审批的表单与流程，后续员工发起审批时，需要填写定义的表单，审批的流转也会按照定义的流程进行。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/approval/overview-of-approval-resources"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Approval")]
[Header(Consts.Authorization)]
public interface IFeishuTenantV4Approval
{

    /// <summary>
    /// 用于创建审批定义，可以灵活指定审批定义的基础信息、表单和流程等。
    /// </summary>
    /// <param name="createApprovalRequest">创建审批定义请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/approvals")]
    Task<FeishuApiResult<CreateApprovalResult>?> CreateApprovalAsync(
        [Body] CreateApprovalRequest createApprovalRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据审批定义 Code 以及语言、用户 ID 等筛选条件获取指定审批定义的信息，包括审批定义名称、状态、表单控件以及节点等信息。获取审批定义信息后，可根据信息构造创建审批实例的请求。
    /// </summary>
    /// <param name="approval_code">审批定义 Code。示例值："7C468A54-8745-2245-9675-08B7C63E7A85"</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="locale">语言可选值，默认为审批定义配置的默认语言。示例值："zh-CN"</param>
    /// <param name="with_admin_id">是否返回有数据管理权限的审批流程管理员 ID 列表（即响应参数 approval_admin_ids）。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/approval/v4/approvals/{approval_code}")]
    Task<FeishuApiResult<GetApprovalResult>?> GetApprovalByCodeAsync(
        [Path] string approval_code,
        [Query("locale")] string? locale = null,
        [Query("with_admin_id")] bool? with_admin_id = null,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 使用指定审批定义 Code 创建一个审批实例，接口调用者需对审批定义的表单有详细了解，按照定义的表单结构，将表单 Value 通过本接口传入。
    /// </summary>
    /// <param name="createInstanceRequest">创建审批实例请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances")]
    Task<FeishuApiResult<CreateInstancesResult>?> CreateInstanceAsync(
        [Body] CreateInstanceRequest createInstanceRequest,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 撤回审批实例
    /// <para>管理员在审批后台的某一审批定义的 更多设置 中，勾选了 允许撤销审批中的申请 或者 允许撤销 x 天内通过的审批，则在符合撤销规则的情况下，你可以调用本接口将指定提交人的审批实例撤回。</para>
    /// </summary>
    /// <param name="cancelInstancesRequest">撤回审批实例请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/cancel")]
    Task<FeishuNullDataApiResult?> CancelInstanceAsync(
        [Body] CancelInstancesRequest cancelInstancesRequest,
        [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 将当前审批实例抄送给指定用户。被抄送的用户可以查看审批实例详情。
    /// <para>例如，在飞书客户端的 工作台 > 审批 > 审批中心 > 抄送我 列表中查看到审批实例。</para>
    /// </summary>
    /// <param name="ccInstanceRequest">抄送审批实例请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/cc")]
    Task<FeishuNullDataApiResult?> CarbonCopyInstanceAsync(
      [Body] CarbonCopyInstanceRequest ccInstanceRequest,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 在创建审批实例之前，可调用本接口预览审批流程数据。在创建审批实例之后，可调用本接口预览某一审批节点的后续流程数据。
    /// </summary>
    /// <param name="previewInstanceRequest">预览审批流程请求体</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/approval/v4/instances/preview")]
    Task<FeishuApiResult<PreviewNodeResult>?> PreviewInstanceAsync(
         [Body] PreviewInstanceRequest previewInstanceRequest,
         [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
         CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过审批实例 Code 获取指定审批实例的详细信息，包括审批实例的名称、创建时间、发起审批的用户、状态以及任务列表等信息。
    /// </summary>
    /// <param name="instance_id">审批实例 Code。示例值："7C468A54-8745-2245-9675-08B7C63E7A85"</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="locale">语言可选值，默认为审批定义配置的默认语言。示例值："zh-CN"</param>
    /// <param name="user_id">发起审批的用户 ID，ID 类型由 user_id_type 参数指定。示例值："f7cb567e"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/approval/v4/instances/{instance_id}")]
    Task<FeishuApiResult<GetApprovalInstanceResult>?> GetInstanceByIdAsync(
       [Path] string instance_id,
       [Query("locale")] string? locale = null,
       [Query("user_id")] bool? user_id = null,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
