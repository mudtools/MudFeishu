// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.Interfaces;


/// <summary>
/// 飞书 OKR「分类」SDK 是一组服务端 OpenAPI 的封装，用于分页查询全部 OKR 分类（okr/v2）。本接口全部端点为 okr/v2，同时支持 tenant_access_token 与 user_access_token 调用（租户态见 <see cref="IFeishuTenantV2OkrCategory"/>，用户态见 <see cref="IFeishuUserV2OkrCategory"/>）。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-category/list"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV2OkrCategory : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 获取全部 OKR 分类
    /// <para>分页获取系统中的全部 OKR 分类，包含 id、多语言名称、颜色、类型、启用状态与创建/更新时间。</para>
    /// <para>限频：100 次/分钟。所需权限：okr:okr.setting:read（查看 OKR 设置）。支持的应用类型：自建应用。</para>
    /// <para><see href="https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/okr-v2/okr-category/list">接口文档</see></para>
    /// </summary>
    /// <param name="page_size">分页大小，取值范围 1 ~ 100，默认 10</param>
    /// <param name="page_token">分页标记，首次请求不传，存在更多数据时随响应返回，下次请求传入可继续翻页</param>
    /// <param name="owner_type">分类归属类型：user 员工、department 部门，默认 user</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns>返回分类分页列表（items、page_token、has_more）</returns>
    [Get("/open-apis/okr/v2/categories")]
    Task<FeishuApiResult<ListCategoriesResult>?> ListCategoriesAsync(
        [Query("page_size")] int page_size = Consts.PageSize_10,
        [Query("page_token")] string? page_token = null,
        [Query("owner_type")] string owner_type = "user",
        CancellationToken cancellationToken = default);
}
