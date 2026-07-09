// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Search;

namespace Mud.Feishu;


/// <summary>
/// 飞书AI套件搜索搜索根据搜索关键词（query）对当前用户可见的应用、消息进行搜索。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/search-v2/suite-search/create"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), RegistryGroupName = "Search")]
[Token(FeishuTokenTypes.UserAccessToken, Name = Consts.Authorization)]
public interface IFeishuUserV2SearchSuite : IFeishuAppContextSwitcher, ICurrentUserId
{

    /// <summary>
    /// 搜索消息。
    /// <para>用户可以通过关键字搜索可见消息，可见性和套件内搜索一致。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/suite-search/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">搜索消息请求体</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/search/v2/message")]
    Task<FeishuApiPageListResult<string>?> SearchMessagePageListAsync(
      [Body] SearchMessageRequest request,
      [Query] int? page_size = 20,
      [Query] string? page_token = null,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 搜索应用。
    /// <para>用户可以通过关键字搜索可见应用，可见性和套件内搜索一致。</para>
    /// <para><see href="https://open.feishu.cn/document/server-docs/search-v2/suite-search/create">接口文档</see></para>
    /// </summary>
    /// <param name="request">搜索应用请求体</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：20</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="user_id_type">
    /// <para>用户 ID 类型</para>
    /// <para>示例值：open_id</para>
    /// <list type="bullet">
    /// <item>open_id：标识一个用户在某个应用中的身份。同一个用户在不同应用中的 Open ID 不同。</item>
    /// <item>union_id：标识一个用户在某个应用开发商下的身份。同一用户在同一开发商下的应用中的 Union ID 是相同的，在不同开发商下的应用中的 Union ID 是不同的。通过 Union ID，应用开发商可以把同个用户在多个应用中的身份关联起来。</item>
    /// <item>user_id：标识一个用户在某个租户内的身份。同一个用户在租户 A 和租户 B 内的 User ID 是不同的。在同一个租户内，一个用户的 User ID 在所有应用（包括商店应用）中都保持一致。User ID 主要用于在不同的应用间打通用户数据。</item>
    /// </list>
    /// <para>默认值：open_id</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/search/v2/app")]
    Task<FeishuApiPageListResult<string>?> SearchAppPageListAsync(
      [Body] SearchAppRequest request,
      [Query] int? page_size = 20,
      [Query] string? page_token = null,
      [Query] string? user_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);
}
