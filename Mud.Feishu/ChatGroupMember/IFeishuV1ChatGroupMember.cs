// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ChatGroupMember;

namespace Mud.Feishu;

/// <summary>
/// 飞书群成员包括用户和机器人。在飞书群组内，支持添加用户或者机器人作为群成员，同时支持将用户或者机器人设置为群管理员。
/// </summary>
public interface IFeishuV1ChatGroupMember
{
    /// <summary>
    /// 指定群组，将群内指定的用户或者机器人设置为群管理员。
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="addGroupManagerRequest">指定群管理员请求体</param>
    /// <param name="member_id_type">用户 ID 类型，ID 类型需要与请求体参数中的 member_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/chats/{chat_id}/managers/add_managers")]
    Task<FeishuApiResult<GroupManagerResult>?> AddChatGroupManagersAsync(
       [Path] string chat_id,
       [Body] GroupManagerRequest addGroupManagerRequest,
       [Query("member_id_type")] string member_id_type = Consts.User_Id_Type,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// 指定群组，删除群组内指定的管理员，包括用户类型的管理员和机器人类型的管理员。
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="deleteGroupManagerRequest">删除群管理员请求体</param>
    /// <param name="member_id_type">用户 ID 类型，ID 类型需要与请求体参数中的 member_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/chats/{chat_id}/managers/delete_managers")]
    Task<FeishuApiResult<GroupManagerResult>?> DeleteChatGroupManagersAsync(
      [Path] string chat_id,
      [Body] GroupManagerRequest deleteGroupManagerRequest,
      [Query("member_id_type")] string member_id_type = Consts.User_Id_Type,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// 把指定的用户或机器人拉入指定群聊内。
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="addMemberRequest">将用户或机器人拉入群聊请求体</param>
    /// <param name="succeed_type">出现不可用ID后的处理方式 0/1/2   可选值有：
    /// <para>0：不存在/不可见的 ID 会拉群失败，并返回错误响应。存在已离职 ID 时，会将其他可用 ID 拉入群聊，返回拉群成功的响应。</para>
    /// <para>1：将参数中可用的 ID 全部拉入群聊，返回拉群成功的响应，并展示剩余不可用的 ID 及原因。</para>
    /// <para>2：参数中只要存在任一不可用的 ID ，就会拉群失败，返回错误响应，并展示出不可用的 ID。</para>
    /// </param>
    /// <param name="member_id_type">用户 ID 类型，ID 类型需要与请求体参数中的 member_id_type 类型保持一致。</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Post("https://open.feishu.cn/open-apis/im/v1/chats/{chat_id}/managers/delete_managers")]
    Task<FeishuApiResult<AddMemberResult>?> AddChatGroupMemberAsync(
         [Path] string chat_id,
         [Body] AddMemberRequest addMemberRequest,
         [Query("member_id_type")] string member_id_type = Consts.User_Id_Type,
         [Query("succeed_type")] int succeed_type = 0,
         CancellationToken cancellationToken = default);

    /// <summary>
    /// 将当前调用接口的操作者（用户或机器人）加入指定群聊。
    /// </summary>
    /// <param name="chat_id">群 ID。示例值："oc_a0553eda9014c201e6969b478895c230"</param>
    /// <param name="cancellationToken"><see cref="CancellationToken"/>取消操作令牌对象。</param>
    /// <returns></returns>
    [Patch("https://open.feishu.cn/open-apis/im/v1/chats/{chat_id}/members/me_join")]
    Task<FeishuNullDataApiResult?> MeJoinChatGroupAsync(
        [Path] string chat_id,
        CancellationToken cancellationToken = default);

}
