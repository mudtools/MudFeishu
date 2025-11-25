// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ChatGroup;

namespace Mud.Feishu.ChatGroup;

/// <summary>
/// 飞书群组 OpenAPI 提供了群组管理能力，包括创建群、解散群、更新群信息、获取群信息、管理群置顶以及获取群分享链接等。
/// </summary>
public interface IFeishuV1ChatGroup
{
    /// <summary>
    /// 更新指定群的信息，包括群头像、群名称、群描述、群配置以及群主等。
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="updateChatRequest">更新群聊请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Put("https://open.feishu.cn/open-apis/im/v1/chats/{chat_id}")]
    Task<FeishuApiResult<CreateChatResult>?> UpdateChatGroupByIdAsync(
        [Path] string chat_id,
        [Body] UpdateChatRequest updateChatRequest,
        [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 通过 chat_id 解散指定群组。通过 API 解散群组后，群聊天记录将不会保存。
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/im/v1/chats/{chat_id}")]
    Task<FeishuNullDataApiResult?> DeleteChatGroupAsync(
       [Path] string chat_id,
       CancellationToken cancellationToken = default);
}
