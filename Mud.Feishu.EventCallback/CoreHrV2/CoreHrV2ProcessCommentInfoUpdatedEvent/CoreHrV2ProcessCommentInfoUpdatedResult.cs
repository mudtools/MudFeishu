// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 流程评论事件
/// <para>流程新增评论时会触发该事件，该事件包含评论所在的流程ID（process_id是其唯一标识）和评论唯一ID（comment_id）,此功能不受数据权限范围控制 注意事项：你需要在应用内配置事件订阅，并订阅该事件，这样才可以在事件触发时接收到事件数据。了解事件订阅可参见事件概述。</para>
/// <para>事件类型:corehr.process_comment_info.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2ProcessCommentInfoUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/process_comment_info/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2ProcessCommentInfoUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2ProcessCommentInfoUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>流程运行实例id</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("process_id")]
    public string? ProcessId { get; set; }

    /// <summary>
    /// <para>流程下评论的唯一id</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `64` 字符</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("comment_id")]
    public string? CommentId { get; set; }

    /// <summary>
    /// <para>评论事件的类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：新增</para>
    /// <para>**默认值**：`1`</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`1` ～ `100`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }
}
