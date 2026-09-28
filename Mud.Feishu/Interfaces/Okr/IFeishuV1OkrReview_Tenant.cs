// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Okr;

namespace Mud.Feishu;


/// <summary>
/// 飞书 OKR「复盘查询」SDK 是一组服务端 OpenAPI 的封装，用于按周期与用户批量查询 OKR 复盘信息（周期复盘文档、进展报告文档）。本接口全部端点为 okr/v1，仅支持 tenant_access_token 调用。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/okr-v1/review/query"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Okr")]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1OkrReview : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 查询复盘信息
    /// <para>根据周期与用户查询复盘信息，返回用户在指定周期下的周期复盘文档与进展报告文档链接。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr:readonly（获取 OKR 信息）、okr:okr（更新 OKR）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/review/query">接口文档</see></para>
    /// </summary>
    /// <param name="user_ids">目标用户 id 列表，最多 5 个，示例值：["ou-asdasdasdasdasd"]</param>
    /// <param name="period_ids">周期 id 列表，最多 5 个，示例值：["6951461264858777132"]</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回复盘列表（review_list：user_id 与 review_period_list，后者含 period_id/cycle_review_list/progress_report_list）</returns>
    [Get("/open-apis/okr/v1/reviews/query")]
    Task<FeishuApiResult<QueryReviewResult>?> QueryReviewAsync(
        [Query("user_ids")] string[] user_ids,
        [Query("period_ids")] string[] period_ids,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);
}
