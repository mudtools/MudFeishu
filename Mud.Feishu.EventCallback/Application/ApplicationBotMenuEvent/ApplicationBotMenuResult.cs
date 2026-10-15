// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Application;

/// <summary>
/// 机器人自定义菜单事件
/// <para>当用户点击类型为事件的机器人菜单时触发</para>
/// <para>事件类型:application.bot.menu_v6</para>
/// <para>使用时请继承：<see cref="ApplicationBotMenuEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/application-v6/bot/events/menu</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.ApplicationBotMenu, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Application")]
public class ApplicationBotMenuResult : IEventResult
{
    /// <summary>
    /// <para>用户信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public ApplicationOperator? Operator { get; set; }

    /// <summary>
    /// <para>菜单事件的唯一标识</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `30` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_key")]
    public string? EventKey { get; set; }

    /// <summary>
    /// <para>用户点击菜单时间</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`1` ～ `30`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("timestamp")]
    public long? Timestamp { get; set; }
}
