// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu;

/// <summary>
/// 消息即飞书聊天中的一条消息。可以使用消息管理 API 对消息进行发送、回复、编辑、撤回、转发以及查询等操作。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/im-v1/message/intro"/></para>
/// </summary> 
public interface IFeishuV1Message
{
    /// <summary>
    /// 撤回指定消息。调用接口的身份不同（身份通过 Authorization 请求头参数指定），可实现的效果不同：
    /// <para> 机器人可以撤回该机器人自己发送的消息。</para>
    /// <para> 群聊的群主可以撤回群内指定的消息。</para>
    /// </summary>
    /// <param name="message_id">待撤回的消息 ID。示例值："om_dc13264520392913993dd051dba21dcf"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}")]
    Task<FeishuNullDataApiResult?> RevokeMessageAsync(
        [Path] string message_id,
        CancellationToken cancellationToken = default);

    #region 表情回复
    /// <summary>
    /// 给指定消息添加指定类型的表情回复。
    /// </summary>
    /// <param name="sendMessageRequest">添加消息表情回复请求体。</param>
    /// <param name="message_id">待添加表情回复的消息 ID。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Post("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/reactions")]
    Task<FeishuApiResult<ReactionResult>?> AddMessageReactionsAsync(
     [Path] string message_id,
     [Body] ReactionRequest sendMessageRequest,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取指定消息内的表情回复列表，支持仅获取特定类型的表情回复。
    /// </summary>
    /// <param name="message_id">待查询的消息ID。</param>
    /// <param name="reaction_type">待查询的表情类型，支持的枚举值参考表情文案说明中的 emoji_type 值。</param> 
    /// <param name="user_id_type">用户 ID 类型</param>
    /// <param name="page_size">分页大小，即本次请求所返回的用户信息列表内的最大条目数。默认值：10</param>
    /// <param name="page_token">分页标记，第一次请求不填，表示从头开始遍历；分页查询结果还有更多项时会同时返回新的 page_token，下次遍历可采用该 page_token 获取查询结果</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/reactions")]
    Task<FeishuApiListResult<ReactionResult>?> GetMessageReactionsPageListAsync(
    [Path] string message_id,
    [Query("reaction_type")] string reaction_type,
    [Query("page_size")] int page_size = 10,
    [Query("page_token")] string? page_token = null,
    [Query("user_id_type")] string? user_id_type = Consts.User_Id_Type,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定消息的某一表情回复。
    /// </summary>
    /// <param name="reaction_id">待删除的表情回复 ID。示例值："ZCaCIjUBVVWSrm5L-3ZTw*************sNa8dHVplEzzSfJVUVLMLcS_"</param>
    /// <param name="message_id">待删除表情回复的消息 ID。示例值："om_8964d1b4*********2b31383276113"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    [Delete("https://open.feishu.cn/open-apis/im/v1/messages/{message_id}/reactions/{reaction_id}")]
    Task<FeishuApiResult<ReactionResult>?> DeleteMessageReactionsAsync(
     [Path] string message_id,
     [Path] string reaction_id,
     CancellationToken cancellationToken = default);
    #endregion
}
