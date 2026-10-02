// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「Objective」SDK 是一组服务端 OpenAPI 的封装，用于获取、修改与删除 OKR Cycle 下的 Objective（okr/v2）。本接口全部端点为 okr/v2，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV2OkrObjective"/>，用户态见 <see cref="IFeishuUserV2OkrObjective"/>）。
/// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective/get">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2OkrObjective : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取 Objective 详情
    /// <para>根据 Objective id 获取 Objective 详情，包含内容、归属者、得分、权重、截止时间与分类。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective/get">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，可通过「获取 OKR Cycle 下 Objective 列表」接口获得，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回 Objective 信息（objective）</returns>
    [Get("/open-apis/okr/v2/objectives/{objective_id}")]
    Task<FeishuApiResult<GetObjectiveResult>?> GetObjectiveAsync(
        [Path] string objective_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改 Objective
    /// <para>修改指定 Objective 的内容、得分、备注、截止时间与分类，支持部分字段更新。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective/patch">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">修改 Objective 请求体（content、score、notes、deadline、category_id 均可选）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回修改后的 Objective 信息（objective）</returns>
    [Patch("/open-apis/okr/v2/objectives/{objective_id}")]
    Task<FeishuApiResult<GetObjectiveResult>?> UpdateObjectiveAsync(
        [Path] string objective_id,
        [Body] PatchObjectiveRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除 Objective
    /// <para>从 OKR Cycle 中删除指定 Objective 及其关联的关键结果。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective/delete">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回被删除的 Objective id（objective_id）</returns>
    [Delete("/open-apis/okr/v2/objectives/{objective_id}")]
    Task<FeishuApiResult<DeleteObjectiveResult>?> DeleteObjectiveAsync(
        [Path] string objective_id,
        CancellationToken cancellationToken = default);
}
