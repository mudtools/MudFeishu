// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ApprovalExternal;

namespace Mud.Feishu;


/// <summary>
/// 三方审批定义 API 用于将企业原有的审批系统与飞书审批系统连通，连通仅为数据层的连通。
/// <para>即企业无需改造原有的审批系统，只需创建三方审批定义，设置三方审批系统的访问方式、回调 URL 以及加密密钥等数据，</para>
/// <para>使三方审批系统的审批数据可以在飞书审批之间来回流转，从而实现企业员工在飞书内一站式查看和处理所有审批任务。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/approval-v4/external_approval/overview"/>
/// </summary>
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Approval")]
[Header(Consts.Authorization)]
public interface IFeishuTenantV4ApprovalExternal
{

    /// <summary>
    /// 用于创建三方审批定义，设置审批的名称、描述等基本信息，以及三方审批系统的审批发起页、回调 URL 等信息，使企业员工在飞书审批内即可发起并操作三方审批。
    /// </summary>
    /// <param name="createApprovalRequest">创建三方审批定义请求体。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="department_id_type">此次调用中的部门 ID 类型。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/approval/v4/external_approvals")]
    Task<FeishuApiResult<CreateApprovalExternalResult>?> CreateApprovalAsync(
      [Body] CreateApprovalExternalRequest createApprovalRequest,
      [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
      [Query("department_id_type")] string? department_id_type = Consts.Department_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 通过三方审批定义 Code 获取审批定义的详细数据，包括三方审批定义的名称、说明、三方审批发起链接、回调 URL 以及审批定义可见人列表等信息。
    /// </summary>
    /// <param name="approval_code">三方审批定义 Code。示例值："7C468A54-8745-2245-9675-08B7C63E7A85"。</param>
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/approval/v4/external_approvals/{approval_code}")]
    Task<FeishuApiResult<GetApprovalExternalResult>?> GetApprovalByCodeAsync(
       [Path] string approval_code,
       [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
