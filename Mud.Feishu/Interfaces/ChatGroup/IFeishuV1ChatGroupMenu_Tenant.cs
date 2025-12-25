// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ChatGroupMenu;

namespace Mud.Feishu;

/// <summary>
/// 在飞书群组内设置自定义菜单，方便群成员快速访问特定链接或者执行特定操作。
/// <para>群菜单分为一级菜单和二级菜单，通过 OpenAPI 你可以添加、删除、修改或者查询群菜单。</para>
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/group/chat-menu_tree/overview"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "ChatGroup", TokenManage = nameof(ITenantTokenManager))]
[Header("Authorization")]
public interface IFeishuTenantV1ChatGroupMenu
{
    /// <summary>
    /// 在指定群组中添加一个或多个群菜单。成功调用后接口会返回当前群组内所有群菜单信息。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="addChatGroupMenuRequest">添加群菜单请求体。</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats/{chat_id}/menu_tree")]
    Task<FeishuApiResult<ChatGroupMenuResult>?> AddMenuByIdAsync(
         [Path] string chat_id,
         [Body] AddChatGroupMenuRequest addChatGroupMenuRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改指定群组内的某个一级菜单或者二级菜单的元信息，包括图标、名称、国际化名称和跳转链接。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="menu_item_id">一级菜单或者二级菜单的 ID。示例值："7156553273518882844"</param>
    /// <param name="updateChatMenuItemRequest">更新群菜单请求体。</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Patch("/open-apis/im/v1/chats/{chat_id}/menu_items/{menu_item_id}")]
    Task<FeishuApiResult<UpdateChatMenuItemResult>?> UpdateMenuByIdAsync(
         [Path] string chat_id,
         [Path] string menu_item_id,
         [Body] UpdateChatMenuItemRequest updateChatMenuItemRequest,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定群内的一级菜单。成功调用后接口会返回群组内最新的群菜单信息。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="deleteMenuIdsRequest">删除群内的一级菜单请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("/open-apis/im/v1/chats/{chat_id}/menu_tree")]
    Task<FeishuApiResult<ChatGroupMenuResult>?> DeleteMenuByIdAsync(
      [Path] string chat_id,
      [Body] ChartMenuIdsRequest deleteMenuIdsRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 调整指定群组内的群菜单排列顺序，成功调用后接口会返回群组内所有群菜单信息。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="sortMenuRequest">菜单项目排序请求体</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("/open-apis/im/v1/chats/{chat_id}/menu_tree/sort")]
    Task<FeishuApiResult<ChatGroupMenuResult>?> SortMenuByIdAsync(
       [Path] string chat_id,
       [Body] ChartMenuIdsRequest sortMenuRequest,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定群组内的群菜单信息，包括所有一级或二级菜单的名称、跳转链接、图标等信息。
    /// </summary>
    /// <param name="chat_id">群 ID。 示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("/open-apis/im/v1/chats/{chat_id}/menu_tree")]
    Task<FeishuApiResult<ChatGroupMenuResult>?> GetMenuByIdAsync(
       [Path] string chat_id,
       CancellationToken cancellationToken = default);
}
