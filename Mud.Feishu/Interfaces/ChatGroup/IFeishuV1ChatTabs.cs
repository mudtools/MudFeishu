// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ChatTabs;

namespace Mud.Feishu.Interfaces;

/// <summary>
/// 会话标签页是指飞书客户端某一会话顶部的标签页，通过 OpenAPI 支持添加、删除、更新以及获取会话标签页等操作。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/group/chat-tab/intro"/></para>
/// </summary>
[HttpClientApi(TokenManage = nameof(IFeishuAppManager), IsAbstract = true)]
[Token(FeishuTokenTypes.TenantAccessToken, Name = Consts.Authorization)]
public interface IFeishuV1ChatTabs : IFeishuAppContextSwitcher
{
    /// <summary>
    /// 在指定会话中添加会话标签页，包括文档类型（doc）、URL 类型（url）和消息类型（message）。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat-tab/create">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="createChatTabsRequest">添加会话标签页请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats/{chat_id}/chat_tabs")]
    Task<FeishuApiResult<ChatTabsCreateResult>?> CreateChatTabsByIdAsync(
          [Path] string chat_id,
          [Body] CreateChatTabsRequest createChatTabsRequest,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新指定的会话标签页信息，包括名称、类型以及内容等。仅支持更新文档类型（doc）或 URL （url）类型的标签页。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat-tab/update_tabs">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="updateChatTabsRequest">更新会话标签页请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("/open-apis/im/v1/chats/{chat_id}/chat_tabs/update_tabs")]
    Task<FeishuApiResult<ChatTabsUpdateResult>?> UpdateChatTabsByIdAsync(
         [Path] string chat_id,
         [Body] UpdateChatTabsRequest updateChatTabsRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定会话内的一个或多个会话标签页。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat-tab/delete_tabs">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="deleteChatTabsRequest">删除会话标签页请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/im/v1/chats/{chat_id}/chat_tabs/delete_tabs")]
    Task<FeishuApiResult<DeleteTabsResult>?> DeleteChatTabsByIdAsync(
      [Path] string chat_id,
      [Body] ChatTabsIdsRequest deleteChatTabsRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 调整指定会话内的多个会话标签页排列顺序。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat-tab/sort_tabs">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="chatTabsSortRequest">排序会话标签页请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats/{chat_id}/chat_tabs/sort_tabs")]
    Task<FeishuApiResult<ChatTabsSortResult>?> ChatTabsSortByIdAsync(
     [Path] string chat_id,
     [Body] ChatTabsIdsRequest chatTabsSortRequest,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定会话内的会话标签页信息，包括 ID、名称、类型以及内容等。
    /// <para><see href="https://open.feishu.cn/document/server-docs/group/chat-tab/list_tabs">接口文档</see></para>
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/im/v1/chats/{chat_id}/chat_tabs/list_tabs")]
    Task<FeishuApiResult<GetChatTabsResult>?> GetChatTabsListByIdAsync(
    [Path] string chat_id,
    CancellationToken cancellationToken = default);
}
