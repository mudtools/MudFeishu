// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书词典（Lingo）词库入口域 SDK 是一组服务端 OpenAPI 的封装，用于获取飞书词典的词库列表。本接口全部端点支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV1LingoRepo"/>，用户态见 <see cref="IFeishuUserV1LingoRepo"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/lingo-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1LingoRepo : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取词库列表
    /// <para>获取当前访问主体有权限查看的飞书词库列表，包含词库 ID（repo_id）、词库名（name）与词库类型（type，1 为全员词库、2 为个人词库、3 为自建词库）。注意：仅当应用或用户添加了对应词库后，才能查询到该词库，如需查询当前租户的全部词库，请使用飞书管理员账号创建的自建应用。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/repo/list">接口文档</see></para>
    /// </summary>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回词库列表（items：id 词库 ID、name 词库名称、type 词库类型）</returns>
    [Get("/open-apis/lingo/v1/repos")]
    Task<FeishuApiResult<GetRepoListResult>?> GetRepoListAsync(
        CancellationToken cancellationToken = default);
}
