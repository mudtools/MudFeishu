// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu;

/// <summary>
/// 飞书服务台知识库分类API是开放平台基于飞书服务台知识库的分类功能开放的查看/创建/修改/删除等API，开发者可以基于这些API对服务台知识库分类进行操作。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/list-categories"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "HelpDesk", InheritedFrom = nameof(FeishuV1HelpDeskCategory))]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuTenantV1HelpDeskCategory : IFeishuV1HelpDeskCategory
{
    /// <summary>
    /// 获取全部知识库分类
    /// <para>用于获取服务台知识库所有分类。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/list-categories">接口文档</see></para>
    /// </summary>
    /// <param name="lang">
    /// <para>知识库分类语言</para>
    /// <para>示例值："zh_cn"</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="order_by">
    /// <para>排序key。1：按知识库分类修改时间排序</para>
    /// <para>示例值：1</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="asc">
    /// <para>顺序。true：升序；false：降序</para>
    /// <para>示例值：true</para>
    /// <para>默认值：null</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/categories")]
    Task<FeishuApiResult<GetCategoryListResult>?> GetCategoryListAsync(
        [Query] string? lang = null,
        [Query] int? order_by = null,
        [Query] bool? asc = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取知识库分类
    /// <para>用于获取单个服务台知识库分类。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/get">接口文档</see></para>
    /// </summary>
    /// <param name="id">
    /// <para>知识库分类 ID</para>
    /// <para>示例值："6948728206392295444"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Get("/open-apis/helpdesk/v1/categories/{id}")]
    Task<FeishuApiResult<GetCategoryResult>?> GetCategoryAsync(
        [Path] string id,
        CancellationToken cancellationToken = default);
}
