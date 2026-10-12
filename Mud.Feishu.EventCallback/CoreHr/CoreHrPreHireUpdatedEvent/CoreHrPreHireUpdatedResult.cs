// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 入职信息变更
/// <para>待入职人员任职信息更新后，触发此事件，包括两种场景： 通过开放平台接口创建待入职、更新待入职 在飞书人事-入职系统，HR 补充任职信息 如果有创建待入职后，更新数据的场景，请收到创建事件后延迟10s时间再执行更新操作 注意事项：- 通过开放平台删除待入职接口不会触发事件。</para>
/// <para>事件类型:corehr.pre_hire.updated_v1</para>
/// <para>使用时请继承：<see cref="CoreHrPreHireUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/pre_hire/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrPreHireUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrPreHireUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>待入职 ID，可通过[【搜索待入职人员】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/pre_hire/search)获取更详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("pre_hire_id")]
    public string? PreHireId { get; set; }

    /// <summary>
    /// <para>变更的字段。</para>
    /// <para>由于历史原因，部分字段（例如个人信息）变更会发送为‘wk_updated_at’字段，不会显示真实变更字段，需要通过[【搜索待入职人员】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/pre_hire/search)获取更详细信息。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_changes")]
    public string[]? FieldChanges { get; set; }
}
