// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「Cycle」SDK 是一组服务端 OpenAPI 的封装，用于查询用户 OKR Cycle 列表，以及在 Cycle 下创建、查询 Objective 并调整其排序与权重（okr/v2）。本接口全部端点为 okr/v2，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV2OkrCycle"/>，用户态见 <see cref="IFeishuUserV2OkrCycle"/>）。
/// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-cycle/list">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2OkrCycle : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取用户 OKR Cycle 列表
    /// <para>分页获取指定用户的 OKR Cycle 列表，包含周期状态、起止时间与得分。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.period:readonly（获取 OKR 周期）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-cycle/list">接口文档</see></para>
    /// </summary>
    /// <param name="user_id">用户 ID，类型与 user_id_type 一致，示例值：ou_3bbe8a09c20e89cce9bff989ed840674</param>
    /// <param name="page_size">分页大小，取值范围 1 ~ 100，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不传，存在更多数据时随响应返回，下次请求传入可继续翻页</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回用户 OKR Cycle 分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v2/cycles")]
    Task<FeishuApiResult<ListCyclesResult>?> ListCyclesAsync(
        [Query("user_id")] string user_id,
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 调整 Objective 在 OKR Cycle 下的排序
    /// <para>通过有序的 Objective id 列表重新排列指定 Cycle 下 Objective 的顺序。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-cycle/objectives_position">接口文档</see></para>
    /// </summary>
    /// <param name="cycle_id">用户 Cycle ID，可通过「获取用户 OKR Cycle 列表」接口获得，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">排序请求体（objective_ids 按序号顺序排列的 Objective id 列表，必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回调整后的 Objective 列表（items）</returns>
    [Put("/open-apis/okr/v2/cycles/{cycle_id}/objectives_position")]
    Task<FeishuApiResult<UpdateCycleObjectivesResult>?> UpdateCycleObjectivesPositionAsync(
        [Path] string cycle_id,
        [Body] UpdateCycleObjectivesPositionRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 调整 Objective 在 OKR Cycle 下的权重
    /// <para>批量修改指定 Cycle 下多个 Objective 的权重，权重取值范围 [0,1]，保留三位小数。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-cycle/objectives_weight">接口文档</see></para>
    /// </summary>
    /// <param name="cycle_id">用户 Cycle ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">权重请求体（objective_weights Objective 权重列表，必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回调整后的 Objective 列表（items）</returns>
    [Put("/open-apis/okr/v2/cycles/{cycle_id}/objectives_weight")]
    Task<FeishuApiResult<UpdateCycleObjectivesResult>?> UpdateCycleObjectivesWeightAsync(
        [Path] string cycle_id,
        [Body] UpdateCycleObjectivesWeightRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 在 OKR Cycle 下创建 Objective
    /// <para>在指定用户 OKR Cycle 下创建新的 Objective，可定义富文本内容、备注、截止时间、权重、分类与初始得分。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-cycle-objective/create">接口文档</see></para>
    /// </summary>
    /// <param name="cycle_id">用户 Cycle ID，可通过「获取用户 OKR Cycle 列表」接口获得，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">创建 Objective 请求体（content、notes、deadline、weight、category_id、score 均可选）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回新建的 Objective id（objective_id）</returns>
    [Post("/open-apis/okr/v2/cycles/{cycle_id}/objectives")]
    Task<FeishuApiResult<CreateCycleObjectiveResult>?> CreateCycleObjectiveAsync(
        [Path] string cycle_id,
        [Body] CreateCycleObjectiveRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 OKR Cycle 下 Objective 列表
    /// <para>分页获取指定用户 OKR Cycle 下的 Objective 列表，包含内容、归属者、得分与进展状态。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-cycle-objective/list">接口文档</see></para>
    /// </summary>
    /// <param name="cycle_id">用户 Cycle ID，可通过「获取用户 OKR Cycle 列表」接口获得，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="page_size">分页大小，取值范围 1 ~ 100，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不传，存在更多数据时随响应返回，下次请求传入可继续翻页</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Objective 分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v2/cycles/{cycle_id}/objectives")]
    Task<FeishuApiResult<ListCycleObjectivesResult>?> ListCycleObjectivesAsync(
        [Path] string cycle_id,
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);
}
