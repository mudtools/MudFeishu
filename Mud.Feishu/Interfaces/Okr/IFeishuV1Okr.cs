// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Okr;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「OKR 查询」SDK 是一组服务端 OpenAPI 的封装，用于按用户获取 OKR 列表以及按 OKR id 批量获取 OKR 详情（目标、关键结果、进度与对齐关系）。本接口全部端点为 okr/v1，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV1Okr"/>，用户态见 <see cref="IFeishuUserV1Okr"/>）。
/// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/okr/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1Okr : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取用户的 OKR 列表
    /// <para>根据用户 id 分页获取其 OKR 列表，返回目标（Objective）、关键结果（KR）、进度与对齐关系等信息。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr:readonly（获取 OKR 信息）、okr:okr（更新 OKR）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/okr/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">目标用户 id，示例值：ou-xxxx</param>
    /// <param name="offset">请求列表的偏移量（对应响应体的 okr_list 字段），offset &gt;= 0，必填</param>
    /// <param name="limit">请求列表的长度，0 &lt; limit &lt;= 10，必填</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="lang">请求 OKR 的语言版本（如 @ 的名字），zh_cn/en_us，默认 zh_cn</param>
    /// <param name="period_ids">周期 id 列表，最多 10 个</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 OKR 列表（total、okr_list：id/permission/period_id/name/objective_list/confirm_status）</returns>
    [Get("/open-apis/okr/v1/users/{user_id}/okrs")]
    Task<FeishuApiResult<ListUserOkrsResult>?> ListUserOkrsAsync(
        [Path] string user_id,
        [Query("offset")] int offset,
        [Query("limit")] int limit,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("lang")] string lang = "zh_cn",
        [Query("period_ids")] string[]? period_ids = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 批量获取 OKR
    /// <para>根据 OKR id 批量获取 OKR 详情，单次最多 10 个。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：okr:okr:readonly（获取 OKR 信息）、okr:okr（更新 OKR）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用、商店应用。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/okr-v1/okr/batch_get">接口文档</see></para>
    /// </summary>
    /// <param name="okr_ids">OKR id 列表，最多 10 个，示例值：["632422323123"]</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id/people_admin_id），默认 open_id</param>
    /// <param name="lang">请求 OKR 的语言版本（如 @ 的名字），zh_cn/en_us，默认 zh_cn</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 OKR 列表（okr_list：id/permission/period_id/name/objective_list/confirm_status）</returns>
    [Get("/open-apis/okr/v1/okrs/batch_get")]
    Task<FeishuApiResult<BatchGetOkrsResult>?> BatchGetOkrsAsync(
        [Query("okr_ids")] string[] okr_ids,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("lang")] string lang = "zh_cn",
        CancellationToken cancellationToken = default);
}
