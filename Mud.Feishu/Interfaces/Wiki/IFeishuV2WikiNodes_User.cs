// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu;

/// <summary>
/// <para>知识空间中的节点，支持文档、表格等多种文件类型。</para>
/// <para>文件是各种类型的文件的统称，泛指云空间内所有的文件。每个文件都有唯一 token 作为标识。</para>
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/docs/wiki-v2/wiki-overview"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Wiki", InheritedFrom = nameof(FeishuV2WikiNodes))]
[Token("UserAccessToken", Name = Consts.Authorization)]
public interface IFeishuUserV2WikiNodes : IFeishuV2WikiNodes, ICurrentUserId
{
    /// <summary>
    /// <para>搜索 Wiki，用户通过关键词查询 Wiki，只能查找自己可见的 wiki</para>
    /// <para>**注：** Wiki 存在，但用户搜索不到并不一定是搜索有问题，可能是用户没有查看该 Wiki 的权限</para>  /// </summary>
    /// <param name="wikiSearchRequest">搜索 Wiki 请求体</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/wiki/v2/nodes/search")]
    Task<FeishuApiPageListResult<WikiSearchResult>?> SearchPageListAsync(
         [Body] WikiSearchRequest wikiSearchRequest,
         [Query("page_size")] int page_size = Consts.PageSize_10,
         [Query("page_token")] string? page_token = null,
         CancellationToken cancellationToken = default);
}
