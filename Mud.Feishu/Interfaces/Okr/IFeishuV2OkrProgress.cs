// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「进展」SDK 是一组服务端 OpenAPI 的封装，用于分页查询 Objective 与 Key Result 的进展记录（okr/v2）。本接口全部端点为 okr/v2，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV2OkrProgress"/>，用户态见 <see cref="IFeishuUserV2OkrProgress"/>）。
/// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-progress/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2OkrProgress : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取 Objective 进展列表
    /// <para>分页获取指定 Objective 下的进展记录列表，返回进展内容、创建/更新时间、归属者与完成度。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.progress:readonly（获取 OKR 进展）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-progress/list">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="page_size">分页大小，取值范围 1 ~ 100，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不传，存在更多数据时随响应返回，下次请求传入可继续翻页</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回进展记录分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v2/objectives/{objective_id}/progresses")]
    Task<FeishuApiResult<ListProgressesResult>?> ListObjectiveProgressesAsync(
        [Path] string objective_id,
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Key Result 进展列表
    /// <para>分页获取指定 Key Result 下的进展记录列表，返回进展内容、创建/更新时间、归属者与完成度。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.progress:readonly（获取 OKR 进展）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-key_result-progress/list">接口文档</see></para>
    /// </summary>
    /// <param name="key_result_id">Key Result ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="page_size">分页大小，取值范围 1 ~ 100，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不传，存在更多数据时随响应返回，下次请求传入可继续翻页</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回进展记录分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v2/key_results/{key_result_id}/progresses")]
    Task<FeishuApiResult<ListProgressesResult>?> ListKeyResultProgressesAsync(
        [Path] string key_result_id,
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);
}
