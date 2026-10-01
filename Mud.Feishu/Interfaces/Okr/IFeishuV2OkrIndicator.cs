// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「指标」SDK 是一组服务端 OpenAPI 的封装，用于查询 Objective/Key Result 的指标以及更新指标配置与取值（okr/v2）。本接口全部端点为 okr/v2，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV2OkrIndicator"/>，用户态见 <see cref="IFeishuUserV2OkrIndicator"/>）。
/// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-indicator/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2OkrIndicator : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取 Objective 指标
    /// <para>获取指定 Objective 的指标信息（状态、起始值/目标值/当前值、计算方式与单位）。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-indicator/list">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回指标信息（indicator）</returns>
    [Get("/open-apis/okr/v2/objectives/{objective_id}/indicators")]
    Task<FeishuApiResult<IndicatorResult>?> GetObjectiveIndicatorAsync(
        [Path] string objective_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Key Result 指标
    /// <para>获取指定 Key Result 的指标信息（状态、起始值/目标值/当前值、计算方式与单位）。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-key_result-indicator/list">接口文档</see></para>
    /// </summary>
    /// <param name="key_result_id">Key Result ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回指标信息（indicator）</returns>
    [Get("/open-apis/okr/v2/key_results/{key_result_id}/indicators")]
    Task<FeishuApiResult<IndicatorResult>?> GetKeyResultIndicatorAsync(
        [Path] string key_result_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新指标
    /// <para>更新已有指标的计算方式、状态、当前值、目标值与单位设置。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-indicator/patch">接口文档</see></para>
    /// </summary>
    /// <param name="indicator_id">指标 ID，可通过获取 Objective/Key Result 指标接口获得，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">更新指标请求体（current_value_calculate_type、status_calculate_type、start_value、target_value、current_value、unit、indicator_status 均可选）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的指标信息（indicator）</returns>
    [Patch("/open-apis/okr/v2/indicators/{indicator_id}")]
    Task<FeishuApiResult<IndicatorResult>?> UpdateIndicatorAsync(
        [Path] string indicator_id,
        [Body] PatchIndicatorRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);
}
