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
/// <para>当前接口使用租户令牌访问，适应于租户应用场景。</para>
/// 接口详细文档请参见：<see href="https://open.feishu.cn/document/server-docs/group/chat/intro"/>
/// </summary>
[HttpClientApi(RegistryGroupName = "ChatGroup", TokenManage = nameof(ITenantTokenManager))]
[Header("Authorization")]
public interface IFeishuTenantV1ChatGroup : IFeishuV1ChatGroup
{
    /// <summary>
    /// 创建群聊，创建时支持设置群头像、群名称、群主以及群类型等配置，同时支持邀请群成员、群机器人入群。
    /// </summary>
    /// <param name="createChatRequest">创建群聊请求体。</param>
    /// <param name="user_id_type">用户 ID，ID 类型需要与查询参数中的 user_id_type 类型保持一致。</param>
    /// <param name="set_bot_manager">如果在请求体的 owner_id 字段指定了某个用户为群主，可以选择是否同时设置创建此群的机器人为管理员，此标志位用于标记是否设置创建群的机器人为管理员。
    ///  <para>示例值：false</para>
    /// </param>
    /// <param name="uuid">由开发者生成的唯一字符串序列，用于创建群组请求去重；持有相同 uuid + owner_id（若有） 的请求 10 小时内只可成功创建 1 个群聊。不传值表示不进行请求去重，每一次请求成功后都会创建一个群聊。</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/chats")]
    Task<FeishuApiResult<CreateChatResult>?> CreateChatGroupAsync(
       [Body] CreateChatRequest createChatRequest,
       [Query("user_id_type")] string user_id_type = Consts.User_Id_Type,
       [Query("set_bot_manager")] bool? set_bot_manager = false,
        [Query("uuid")] string? uuid = null,
       CancellationToken cancellationToken = default);
}
