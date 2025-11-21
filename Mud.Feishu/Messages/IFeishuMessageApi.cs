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
[HttpClientApi(RegistryGroupName = "Message")]
[HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IFeishuMessage))]
public interface IFeishuMessageApi
{
    /// <summary>
    /// 向指定用户或者群聊发送消息。
    /// <para>支持发送的消息类型包括文本、富文本、卡片、群名片、个人名片、图片、视频、音频、文件以及表情包等。</para>
    /// </summary>
    /// <param name="tenant_access_token">应用调用 API 时，通过访问凭证（access_token）进行身份鉴权</param>
    /// <param name="sendMessageRequest">发送消息请求体。</param>
    /// <param name="receive_id_type">用户 ID 类型</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/messages")]
    Task<FeishuApiResult<MessageDataResult>> SendMessageAsync(
       [Token][Header("Authorization")] string tenant_access_token,
       [Body] SendMessageRequest sendMessageRequest,
       [Query("receive_id_type")] string receive_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);
}
