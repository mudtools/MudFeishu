// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书词典（Lingo）词条入口域 SDK 是一组服务端 OpenAPI 的封装，本接口承载词条详情查询、分页列表、模糊搜索、精准匹配与高亮识别等租户态与用户态通用端点（两者令牌均可调用，租户态见 <see cref="IFeishuTenantV1LingoEntity"/>，用户态见 <see cref="IFeishuUserV1LingoEntity"/>）；免审创建/更新/删除仅支持 tenant_access_token，声明于租户态派生接口。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/lingo-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1LingoEntity : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取词条详情
    /// <para>按词条 ID 获取词条详情；也可通过 provider + outer_id 以外部系统关联方式查询。返回结果包含词条名、别名、释义、相关信息、反馈统计、外部关联、创建/更新信息等。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/get">接口文档</see></para>
    /// </summary>
    /// <param name="entity_id">词条 ID，示例值：enterprise_515879</param>
    /// <param name="provider">外部系统，长度 2 ～ 32 字符，示例值：星云</param>
    /// <param name="outer_id">词条在外部系统中对应的唯一 ID，长度 1 ～ 64 字符，示例值：123aaa</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回词条详情（entity）</returns>
    [Get("/open-apis/lingo/v1/entities/{entity_id}")]
    Task<FeishuApiResult<GetEntityResult>?> GetEntityAsync(
        [Path] string entity_id,
        [Query("provider")] string? provider = null,
        [Query("outer_id")] string? outer_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取词条列表
    /// <para>分页获取飞书词典的全部词条，支持按外部系统 provider 过滤、按词库 repo_id 拉取。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。</para>
    /// <para>以应用身份拉取非全员词库的词条需在「词库设置」页面添加应用；以用户身份需该用户拥有对应词库的可见权限。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，默认 20，取值范围 1 ～ 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="provider">相关外部系统，可用来过滤词条数据，长度 2 ～ 32 字符，示例值：星云</param>
    /// <param name="repo_id">词库 ID，不传时默认返回全员词库数据，示例值：7152790921053274113</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回词条分页列表（entities、page_token、has_more）</returns>
    [Get("/open-apis/lingo/v1/entities")]
    Task<FeishuApiResult<GetEntityListResult>?> GetEntityListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("provider")] string? provider = null,
        [Query("repo_id")] string? repo_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 模糊搜索词条
    /// <para>传入关键词，与词条名、别名、释义等信息进行模糊匹配，返回搜到的词条信息；支持按分类、创建来源、创建者过滤。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/search">接口文档</see></para>
    /// </summary>
    /// <param name="request">搜索请求体（query 搜索关键词 1 ～ 100 字符、classification_filter 分类筛选、sources 创建来源、creators 创建者）</param>
    /// <param name="page_size">每页返回的词条量，默认 20，取值范围 1 ～ 100</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="repo_id">词库 ID，不传时默认在全员词库内搜索，示例值：7202510112396640276</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回搜索结果（entities、page_token、has_more）</returns>
    [Post("/open-apis/lingo/v1/entities/search")]
    Task<FeishuApiResult<SearchEntityResult>?> SearchEntityAsync(
        [Body] SearchEntityRequest request,
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("repo_id")] string? repo_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 精准搜索词条
    /// <para>将关键词与词条名、别名精准匹配，并返回对应的词条 ID，可在外部系统中快速定位词条。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/match">接口文档</see></para>
    /// </summary>
    /// <param name="request">匹配请求体（word 搜索关键词必填，1 ～ 100 字符）</param>
    /// <param name="repo_id">词库 ID，不传时默认在全员词库内搜索，示例值：7202510112396640276</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回匹配结果（results：entity_id 词条 ID、type 匹配中的字段）</returns>
    [Post("/open-apis/lingo/v1/entities/match")]
    Task<FeishuApiResult<MatchEntityResult>?> MatchEntityAsync(
        [Body] MatchEntityRequest request,
        [Query("repo_id")] string? repo_id = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 词条高亮
    /// <para>传入一句话，智能识别句中对应的词条，并返回词条位置和 entity_id，可在外部系统中快速实现词条智能高亮。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/entity/highlight">接口文档</see></para>
    /// </summary>
    /// <param name="request">高亮请求体（text 需要识别的内容必填，1 ～ 1000 字符）</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回识别到的词条信息（phrases：name 词条名、entity_ids 实体词 ID 列表、span 词条位置）</returns>
    [Post("/open-apis/lingo/v1/entities/highlight")]
    Task<FeishuApiResult<HighlightEntityResult>?> HighlightEntityAsync(
        [Body] HighlightEntityRequest request,
        CancellationToken cancellationToken = default);
}
