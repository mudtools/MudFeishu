// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu;

/// <summary>
/// 用于管理给多个用户或者多个部门发送消息，支持发送文本、富文本、卡片、群名片、个人名片、图片、视频、音频、文件、表情包。
/// <para>接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/im-v1/batch_message/send-messages-in-batches"/></para>
/// </summary> 
[HttpClientApi(TokenManage = nameof(ITenantTokenManager), RegistryGroupName = "Message")]
[Header("Authorization")]
public interface IFeishuTenantV1BatchMessage
{
    /// <summary>
    /// 给多个用户或者多个部门中的成员发送文本消息。
    /// </summary>
    /// <param name="sendMessageRequest">批量发送文本消息请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/message/v4/batch_send")]
    Task<FeishuApiResult<BatchMessageResult>?> BatchSendTextMessageAsync(
      [Body] BatchSenderTextMessageRequest sendMessageRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 给多个用户或者多个部门中的成员发送富文本消息。
    /// </summary>
    /// <param name="sendMessageRequest">批量发送富文本消息请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/message/v4/batch_send")]
    Task<FeishuApiResult<BatchMessageResult>?> BatchSendRichTextMessageAsync(
      [Body] BatchSenderRichTextMessageRequest sendMessageRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 给多个用户或者多个部门中的成员发送图片消息。
    /// </summary>
    /// <param name="sendMessageRequest">批量发送图片消息请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/message/v4/batch_send")]
    Task<FeishuApiResult<BatchMessageResult>?> BatchSendImageMessageAsync(
      [Body] BatchSenderMessageImageRequest sendMessageRequest,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 给多个用户或者多个部门中的成员发群分享消息。
    /// </summary>
    /// <param name="sendMessageRequest">批量发送群分享消息请求体。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/message/v4/batch_send")]
    Task<FeishuApiResult<BatchMessageResult>?> BatchSendGroupShareMessageAsync(
      [Body] BatchSenderMessageGroupShareRequest sendMessageRequest,
      CancellationToken cancellationToken = default);


    /// <summary>
    /// 用于撤回通过批量发送消息接口发送的消息。
    /// </summary>
    /// <param name="batch_message_id">待撤回的批量消息任务 ID，该 ID 为批量发送消息接口返回值中的message_id字段，用于标识一次批量发送消息请求。
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Delete("https://open.feishu.cn/open-apis/im/v1/batch_messages/{batch_message_id}")]
    Task<FeishuNullDataApiResult?> RevokeMessageAsync(
        [Path] string batch_message_id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量发送消息后，可通过该接口查询消息推送的总人数以及消息已读人数。
    /// </summary>
    /// <param name="batch_message_id">待查询的批量消息任务 ID，该 ID 为批量发送消息接口返回值中的 message_id 字段，用于标识一次批量发送消息请求。
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/batch_messages/{batch_message_id}/read_user")]
    Task<FeishuApiResult<BatchMessageReadStatusResult>?> GetUserReadMessageInfosAsync(
        [Path] string batch_message_id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量发送消息或者批量撤回消息后，可通过该接口查询消息的发送进度和撤回进度。
    /// </summary>
    /// <param name="batch_message_id">待查询的批量消息任务 ID，该 ID 为批量发送消息接口返回值中的 message_id 字段，用于标识一次批量发送消息请求。
    /// <para>示例值："om_dc13264520392913993dd051dba21dcf"</para>
    /// </param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Get("https://open.feishu.cn/open-apis/im/v1/batch_messages/{batch_message_id}/get_progress")]
    Task<FeishuApiResult<BatchMessageProgressResult>?> GetBatchMessageProgressAsync(
        [Path] string batch_message_id,
        CancellationToken cancellationToken = default);
}
