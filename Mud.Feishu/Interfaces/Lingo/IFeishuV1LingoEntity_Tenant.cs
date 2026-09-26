// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu;


/// <summary>
/// 飞书词典（Lingo）词条入口域租户态 SDK 是一组服务端 OpenAPI 的封装，除继承自 <see cref="IFeishuV1LingoEntity"/> 的通用查询端点外，本接口还提供免审创建/更新/删除词条端点（仅 tenant_access_token 可调用）。用户态见 <see cref="IFeishuUserV1LingoEntity"/>。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/lingo-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Lingo", InheritedFrom = nameof(FeishuV1LingoEntity))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1LingoEntity : IFeishuV1LingoEntity
{
    /// <summary>
    /// 创建免审词条
    /// <para>通过此接口创建的词条无需经过词典管理员审核，直接写入词库，调用时应当慎重操作。请求体为词条对象（<see cref="CreateOrUpdateEntityRequest"/>）。</para>
    /// <para>限频：100 次/分钟。所需权限：baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。仅支持 tenant_access_token 调用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）</param>
    /// <param name="repo_id">词库 ID，需要在指定词库创建词条时传入，不传时默认创建至全员词库，示例值：71527909****274113</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回词条信息（entity）</returns>
    [Post("/open-apis/lingo/v1/entities")]
    Task<FeishuApiResult<CreateEntityResult>?> CreateEntityAsync(
        [Body] CreateOrUpdateEntityRequest request,
        [Query("repo_id")] string? repo_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新免审词条
    /// <para>通过此接口更新已有的词条，无需经过词典管理员审核，直接写入词库，调用时应当慎重操作。请求体为词条对象（<see cref="CreateOrUpdateEntityRequest"/>）。</para>
    /// <para>限频：100 次/分钟。所需权限：baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。仅支持 tenant_access_token 调用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/update">接口文档</see></para>
    /// </summary>
    /// <param name="entity_id">词条 ID，示例值：enterprise_40217521</param>
    /// <param name="request">词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回更新后的词条信息（entity）</returns>
    [Put("/open-apis/lingo/v1/entities/{entity_id}")]
    Task<FeishuApiResult<UpdateEntityResult>?> UpdateEntityAsync(
        [Path] string entity_id,
        [Body] CreateOrUpdateEntityRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除免审词条
    /// <para>通过此接口删除词条，无需经过词典管理员审核，直接从词库移除。使用外部系统关联方式删除时，需将路径中的词条 ID 固定为 enterprise_0，并同时提供 provider 与 outer_id。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限：baike:entity:exempt_delete（免审删除词典词条）。支持的应用类型：自建应用。仅支持 tenant_access_token 调用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/delete">接口文档</see></para>
    /// </summary>
    /// <param name="entity_id">词条 ID，示例值：enterprise_43742132363；使用外部系统关联删除时固定为 enterprise_0</param>
    /// <param name="provider">外部系统，使用外部系统关联删除时必填（不能包含中横线 "-"），长度 2 ～ 32 字符，示例值：星云</param>
    /// <param name="outer_id">词条在外部系统中对应的唯一 ID，使用外部系统关联删除时必填（不能包含中横线 "-"），长度 1 ～ 64 字符，示例值：123aaa</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>删除成功时无业务字段，仅返回 code 与 msg</returns>
    [Delete("/open-apis/lingo/v1/entities/{entity_id}")]
    Task<FeishuApiResult<DeleteEntityResult>?> DeleteEntityAsync(
        [Path] string entity_id,
        [Query("provider")] string? provider = null,
        [Query("outer_id")] string? outer_id = null,
        CancellationToken cancellationToken = default);
}
