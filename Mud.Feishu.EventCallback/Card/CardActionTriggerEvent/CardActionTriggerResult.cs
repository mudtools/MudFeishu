// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Card;

/// <summary>
/// 卡片事件回调（请求回调）
/// <para>当终端用户点击卡片上的「请求回调」交互组件时，开发者后台注册的回调请求地址将收到卡片回调交互，
/// 其中包含用户与卡片交互的信息。业务服务器收到回调请求后<b>必须在 3 秒内响应</b>，
/// 以声明弹出 Toast 通知、更新卡片或保持原内容不变等响应。</para>
/// <para>事件类型:card.action.trigger</para>
/// <para>使用时请继承：<see cref="CardActionTriggerEventHandler"/></para>
/// <para>响应体请使用 <see cref="CardCallbackResponse"/>；不得返回 3xx 重定向状态码，否则用户侧会提示交互请求错误。</para>
/// <para>文档地址：<see href="https://open.feishu.cn/document/feishu-cards/card-callback-communication"/> </para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CardActionTrigger, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Card")]
public class CardActionTriggerResult : IEventResult
{
    /// <summary>
    /// <para>回调发起者信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator")]
    public CardActionOperator? Operator { get; set; }

    /// <summary>
    /// <para>用于更新卡片的凭证</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 有效期 30 分钟</para>
    /// <para>- 最多可更新 2 次</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("token")]
    public string? Token { get; set; }

    /// <summary>
    /// <para>交互信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("action")]
    public CardActionInfo? Action { get; set; }

    /// <summary>
    /// <para>卡片展示场景</para>
    /// <para>如 <c>im_message</c>（消息卡片）等</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("host")]
    public string? Host { get; set; }

    /// <summary>
    /// <para>卡片分发类型，固定 <c>url_preview</c>（链接预览卡片）</para>
    /// <para>仅链接预览卡片返回</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("delivery_type")]
    public string? DeliveryType { get; set; }

    /// <summary>
    /// <para>展示场景上下文</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("context")]
    public CardActionContext? Context { get; set; }
}
