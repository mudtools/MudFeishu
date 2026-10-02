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
/// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/create">接口文档</see></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "HelpDesk", InheritedFrom = nameof(FeishuV1HelpDeskCategory))]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV1HelpDeskCategory : IFeishuV1HelpDeskCategory, ICurrentUserId
{
    /// <summary>
    /// 创建知识库分类
    /// <para>用于创建服务台知识库分类。注意事项：user_access_token 访问，需要操作者是当前服务台的客服、管理员或所有者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">创建知识库分类请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/helpdesk/v1/categories")]
    Task<FeishuApiResult<CreateCategoryResult>?> CreateCategoryAsync(
        [Body] CreateCategoryRequest request,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 删除知识库分类
    /// <para>用于删除单个服务台知识库分类。注意事项：user_access_token 访问，需要操作者是当前服务台的客服、管理员或所有者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/delete">接口文档</see></para>
    /// </summary>
    /// <param name="id">
    /// <para>知识库分类 ID</para>
    /// <para>示例值："6948728206392295444"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("/open-apis/helpdesk/v1/categories/{id}")]
    Task<FeishuNullDataApiResult?> DeleteCategoryAsync(
        [Path] string id,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 更新知识库分类
    /// <para>用于更新单个服务台知识库分类。注意事项：user_access_token 访问，需要操作者是当前服务台的客服、管理员或所有者。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/helpdesk-v1/faq-management/category/patch">接口文档</see></para>
    /// </summary>
    /// <param name="id">
    /// <para>知识库分类 ID</para>
    /// <para>示例值："6948728206392295444"</para>
    /// </param>
    /// <param name="request">更新知识库分类请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Patch("/open-apis/helpdesk/v1/categories/{id}")]
    Task<FeishuNullDataApiResult?> UpdateCategoryAsync(
        [Path] string id,
        [Body] UpdateCategoryRequest request,
        CancellationToken cancellationToken = default);
}
