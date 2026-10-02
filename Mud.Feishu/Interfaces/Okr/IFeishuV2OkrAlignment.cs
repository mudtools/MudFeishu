// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「对齐关系」SDK 是一组服务端 OpenAPI 的封装，用于创建、分页查询、获取与删除 Objective 间的对齐关系（okr/v2）。本接口全部端点为 okr/v2，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV2OkrAlignment"/>，用户态见 <see cref="IFeishuUserV2OkrAlignment"/>）。
/// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-alignment/create">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2OkrAlignment : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建 Objective 对齐关系
    /// <para>为指定 Objective 创建与其他 Objective 的对齐关系，建立 OKR 结构内的层级对齐。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-alignment/create">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="request">创建对齐关系请求体（to_entity_type 被对齐实体类型、to_entity_id 被对齐实体 id 均必填）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对齐关系 id（alignment_id）</returns>
    [Post("/open-apis/okr/v2/objectives/{objective_id}/alignments")]
    Task<FeishuApiResult<CreateAlignmentResult>?> CreateObjectiveAlignmentAsync(
        [Path] string objective_id,
        [Body] CreateAlignmentRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 Objective 对齐关系列表
    /// <para>分页获取指定 Objective 的对齐关系列表，包含被对齐到其他 Objective（aligning）与被其他 Objective 对齐（aligned）的信息。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-objective-alignment/list">接口文档</see></para>
    /// </summary>
    /// <param name="objective_id">Objective ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="page_size">分页大小，取值范围 1 ~ 100，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不传，存在更多数据时随响应返回，下次请求传入可继续翻页</param>
    /// <param name="align_type">对齐类型：aligned 被他人对齐、aligning 对齐他人</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对齐关系分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v2/objectives/{objective_id}/alignments")]
    Task<FeishuApiResult<ListAlignmentsResult>?> ListObjectiveAlignmentsAsync(
        [Path] string objective_id,
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("align_type")] string? align_type = null,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取单个对齐关系
    /// <para>根据对齐关系 id 获取对齐关系详情，包含对齐双方的实体与归属者信息。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:readonly（获取 OKR 内容）；user_id_type 取 user_id 时需字段权限 contact:user.employee_id:readonly。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-alignment/get">接口文档</see></para>
    /// </summary>
    /// <param name="alignment_id">对齐关系 ID，可通过获取 Objective 对齐关系列表接口获得，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id</param>
    /// <param name="department_id_type">部门 ID 类型（department_id/open_department_id），默认 open_department_id</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回对齐关系信息（alignment）</returns>
    [Get("/open-apis/okr/v2/alignments/{alignment_id}")]
    Task<FeishuApiResult<GetAlignmentResult>?> GetAlignmentAsync(
        [Path] string alignment_id,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        [Query("department_id_type")] string department_id_type = Consts.Department_Id_Type,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除对齐关系
    /// <para>根据对齐关系 id 删除指定对齐关系。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.content:writeonly（更新 OKR 内容）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-alignment/delete">接口文档</see></para>
    /// </summary>
    /// <param name="alignment_id">对齐关系 ID，长度 1 ~ 20 字符，示例值：7342342398472398473</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回被删除的对齐关系 id（alignment_id）</returns>
    [Delete("/open-apis/okr/v2/alignments/{alignment_id}")]
    Task<FeishuApiResult<DeleteAlignmentResult>?> DeleteAlignmentAsync(
        [Path] string alignment_id,
        CancellationToken cancellationToken = default);
}
