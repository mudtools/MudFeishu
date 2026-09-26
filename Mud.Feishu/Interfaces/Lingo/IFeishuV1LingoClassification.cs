// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 飞书词典（Lingo）分类入口域 SDK 是一组服务端 OpenAPI 的封装，用于分页获取飞书词典的词典分类（一级/二级分类与国际化分类名）。本接口全部端点支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV1LingoClassification"/>，用户态见 <see cref="IFeishuUserV1LingoClassification"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/lingo-v1/overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1LingoClassification : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取词典分类
    /// <para>分页获取飞书词典的分类列表（含二级分类名称、对应一级分类 ID 与国际化分类名）。不传 repo_id 时默认返回全员词库的分类。</para>
    /// <para>限频：1000 次/分钟、50 次/秒。所需权限（任一即可）：baike:entity（查看、创建、编辑、删除词典词条）、baike:entity:exempt_review（创建、更新词典免审词条）、baike:entity:readonly（查看词典词条）。支持的应用类型：自建应用。</para>
    /// <para>以应用身份获取非全员词库的分类需在「词库设置」页面添加应用；以用户身份获取需该用户拥有对应词库的可见权限。</para>
    /// <para><see href="https://open.feishu.cn/document/lingo-v1/classification/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，默认 20，取值范围 1 ～ 500</param>
    /// <param name="page_token">分页标记，首次请求不填，翻页时取上一次返回的 page_token</param>
    /// <param name="repo_id">词库 ID，不传默认范围为全员词库，示例值：7202510112396640276</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回分类分页列表（items 分类、page_token 分页标记、has_more 是否有下一页）</returns>
    [Get("/open-apis/lingo/v1/classifications")]
    Task<FeishuApiResult<GetClassificationListResult>?> GetClassificationListAsync(
        [Query("page_size")] int? page_size = null,
        [Query("page_token")] string? page_token = null,
        [Query("repo_id")] string? repo_id = null,
        CancellationToken cancellationToken = default);
}
