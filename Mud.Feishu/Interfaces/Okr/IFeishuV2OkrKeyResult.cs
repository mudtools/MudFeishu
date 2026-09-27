// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「关键结果」SDK 是一组服务端 OpenAPI 的封装，用于查询、创建、修改与删除 Objective 下的关键结果，以及调整其排序与权重（okr/v2）。本接口全部端点为 okr/v2，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV2OkrKeyResult"/>，用户态见 <see cref="IFeishuUserV2OkrKeyResult"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-key_result/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2OkrKeyResult : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取 Objective 下关键结果列表
    /// <para>分页获取指定 Objective 下的关键结果列表，包含内容、序号、得分、权重与截止时间。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-key_result/list">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="page_size">分页大小，取值范围 1 ~ 100，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不传，存在更多数据时随响应返回，下次请求传入可继续翻页</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回关键结果分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v2/objectives/{objective_id}/key_results")]
    Task<FeishuApiResult<ListObjectiveKeyResultsResult>?> ListObjectiveKeyResultsAsync(
        [Path] string objective_id,
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 创建 Objective 下关键结果
    /// <para>为指定 Objective 创建新的关键结果，可定义富文本内容、截止时间与得分。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-key_result/create">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">创建关键结果请求体（content、deadline、score 均可选）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回新建的关键结果 id（key_result_id）</returns>
    [Post("/open-apis/okr/v2/objectives/{objective_id}/key_results")]
    Task<FeishuApiResult<CreateKeyResultResult>?> CreateObjectiveKeyResultAsync(
        [Path] string objective_id,
        [Body] CreateKeyResultRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 调整关键结果排序
    /// <para>通过有序的关键结果 id 列表重新排列指定 Objective 下关键结果的顺序。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective/key_results_position">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">排序请求体（key_result_ids 按序号顺序排列的关键结果 id 列表，必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回调整后的关键结果列表（items）</returns>
    [Put("/open-apis/okr/v2/objectives/{objective_id}/key_results_position")]
    Task<FeishuApiResult<UpdateKeyResultsResult>?> UpdateKeyResultsPositionAsync(
        [Path] string objective_id,
        [Body] UpdateKeyResultsPositionRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 调整关键结果权重
    /// <para>调整指定 Objective 下关键结果的权重分配，用于 Objective 总分计算。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective/key_results_weight">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">权重请求体（key_result_weights 关键结果权重列表，必填）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回调整后的关键结果列表（items）</returns>
    [Put("/open-apis/okr/v2/objectives/{objective_id}/key_results_weight")]
    Task<FeishuApiResult<UpdateKeyResultsResult>?> UpdateKeyResultsWeightAsync(
        [Path] string objective_id,
        [Body] UpdateKeyResultsWeightRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取关键结果详情
    /// <para>根据关键结果 id 获取关键结果详情。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-key_result/get">接口文档</see></para>
    /// </summary>
    /// <param name="key_result_id">Key Result ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回关键结果信息（key_result）</returns>
    [Get("/open-apis/okr/v2/key_results/{key_result_id}")]
    Task<FeishuApiResult<GetKeyResultResult>?> GetKeyResultAsync(
        [Path] string key_result_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 修改关键结果
    /// <para>修改指定关键结果的内容、得分与截止时间。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-key_result/patch">接口文档</see></para>
    /// </summary>
    /// <param name="key_result_id">Key Result ID，可通过获取 Objective 下关键结果列表接口获得，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">修改关键结果请求体（content、score、deadline 均可选）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回修改后的关键结果信息（key_result）</returns>
    [Patch("/open-apis/okr/v2/key_results/{key_result_id}")]
    Task<FeishuApiResult<GetKeyResultResult>?> UpdateKeyResultAsync(
        [Path] string key_result_id,
        [Body] PatchKeyResultRequest request,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除关键结果
    /// <para>根据关键结果 id 删除指定关键结果。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-key_result/delete">接口文档</see></para>
    /// </summary>
    /// <param name="key_result_id">Key Result ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回被删除的关键结果 id（key_result_id）</returns>
    [Delete("/open-apis/okr/v2/key_results/{key_result_id}")]
    Task<FeishuApiResult<DeleteKeyResultResult>?> DeleteKeyResultAsync(
        [Path] string key_result_id,
        CancellationToken cancellationToken = default);
}
