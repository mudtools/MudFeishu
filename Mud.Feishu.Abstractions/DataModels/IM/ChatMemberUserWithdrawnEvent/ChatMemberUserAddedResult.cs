// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions.DataModels.IM;

/// <summary>
/// 撤销拉用户进群事件处理器
/// <para>撤销拉用户进群后触发此事件，在群组内的、已订阅该事件的机器人会收到事件消息。撤销操作是指如下图所示的群内 撤销邀请。</para>
/// <para>事件类型:im.chat.member.user.withdrawn_v1</para>
/// <para>使用时请继承：<see cref="ChatMemberUserWithdrawnEventHandler"/></para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/server-docs/group/chat-member/event/withdrawn"/> </para>
/// </summary>
[EventHandler(EventType = FeishuEventTypes.ChatMemberUserWithdrawn, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom)]
public class ChatMemberUserWithdrawnResult : ChatMemberUserInfo, IEventResult
{

}
