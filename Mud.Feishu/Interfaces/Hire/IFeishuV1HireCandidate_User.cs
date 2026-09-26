// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Hire;

namespace Mud.Feishu;


/// <summary>
/// 飞书招聘（Hire）候选人入口域用户态 SDK 是一组服务端 OpenAPI 的封装，用于以用户身份批量获取招聘待办事项（评估/offer/笔试/面试待办）。本接口全部端点仅支持 user_access_token 调用（租户态备注、任务列表等能力见 <see cref="IFeishuTenantV1HireCandidate"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/hire-v1/recruitment-development-guide"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Hire")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1HireCandidate : IFeishuAppContextSwitcher, ICurrentUserId
{
    /// <summary>
    /// 批量获取待办事项
    /// <para>批量获取当前用户的招聘待办事项信息，包含评估待办、Offer 待办、笔试待办和面试待办；type 决定返回的待办类别。</para>
    /// <para>限频：10 次/秒。所需权限：hire:todo:readonly（访问待办事项）。字段权限：contact:user.employee_id:readonly（返回的用户 ID 字段）。仅支持 user_access_token 调用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/hire-v1/candidate-management/recruitment-process-follow-up/list">接口文档</see></para>
    /// </summary>
    /// <param name="type">待办类型：evaluation-待评估，offer-待办 Offer，exam-待笔试，interview-待面试</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="page_size">每页数量，默认 10，最大 100</param>
    /// <param name="user_id">用户 ID，用户令牌调用时无需传入</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 people_admin_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回待办事项分页列表（items、has_more、page_token）</returns>
    [Get("/open-apis/hire/v1/todos")]
    Task<FeishuApiResult<GetTodoListResult>?> GetTodoListAsync(
        [Query("type")] string type,
        [Query("page_token")] string? page_token = null,
        [Query("page_size")] int? page_size = null,
        [Query("user_id")] string? user_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
