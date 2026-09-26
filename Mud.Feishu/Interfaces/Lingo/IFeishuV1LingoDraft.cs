// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书词典（Lingo）草稿入口域 SDK 是一组服务端 OpenAPI 的封装，用于发起创建新词条或更新现有词条的草稿申请，以及按草稿 ID 更新草稿内容。本接口全部端点支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV1LingoDraft"/>，用户态见 <see cref="IFeishuUserV1LingoDraft"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/lingo-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1LingoDraft : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 创建草稿
    /// <para>通过此接口发起创建新词条或更新现有词条的申请，草稿需经词典管理员审核通过后才会写入词库。请求体为词条对象（<see cref="CreateOrUpdateEntityRequest"/>），创建新词条时不填 id，更新已有词条时填入词条 ID。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。</para>
    /// <para>以应用身份创建草稿到非全员词库需在「词库设置」页面添加应用；以用户身份需该用户拥有对应词库的可见权限。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/draft/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）</param>
    /// <param name="repo_id">词库 ID，需要在指定词库创建草稿时填写，不填写默认创建至全员词库，示例值：7202510112396640276</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回草稿信息（draft：draft_id 草稿 ID、entity 词条内容）</returns>
    [Post("/open-apis/lingo/v1/drafts")]
    Task<FeishuApiResult<CreateDraftResult>?> CreateDraftAsync(
        [Body] CreateOrUpdateEntityRequest request,
        [Query("repo_id")] string? repo_id = null,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新草稿
    /// <para>根据 draft_id 更新草稿内容，已审批的草稿无法编辑。请求体为词条对象（<see cref="CreateOrUpdateEntityRequest"/>）。</para>
    /// <para>限频：100 次/分钟。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）。字段权限：contact:user.employee_id:readonly（返回的创建者/更新者字段）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/draft/update">接口文档</see></para>
    /// </summary>
    /// <param name="draft_id">草稿 ID，示例值：7241543272228814852</param>
    /// <param name="request">词条请求体（main_keys 词条名必填最多 1 个；description 与 rich_text 至少填一个，否则报错 1540001）</param>
    /// <param name="user_id_type">用户 ID 类型（open_id/union_id/user_id），默认 open_id；取 user_id 时需 contact:user.employee_id:readonly 字段权限</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回草稿（draft：draft_id 草稿 ID、entity 词条内容）</returns>
    [Put("/open-apis/lingo/v1/drafts/{draft_id}")]
    Task<FeishuApiResult<UpdateDraftResult>?> UpdateDraftAsync(
        [Path] string draft_id,
        [Body] CreateOrUpdateEntityRequest request,
        [Query("user_id_type")] string? user_id_type = null,
        CancellationToken cancellationToken = default);
}
