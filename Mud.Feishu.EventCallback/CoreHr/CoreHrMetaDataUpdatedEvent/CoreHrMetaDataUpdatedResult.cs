// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 元数据信息变更
/// <para>People元数据定义变更会对外推送事件。例如在People系统中，设置-人员档案配置-个人信息-基本信息 中添加一个字段。就会收到person相关的元数据变更推送。</para>
/// <para>事件类型:corehr.common_data.meta_data.updated_v1</para>
/// <para>使用时请继承：<see cref="CoreHrMetaDataUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/common_data-meta_data/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrMetaDataUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrMetaDataUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>发生变更的对象 API Name，可通过[获取字段详情](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/custom_field/get_by_param)查询字段详情</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("api_name")]
    public string? ApiName { get; set; }

    /// <summary>
    /// <para>变更的字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_changes")]
    public string[]? FieldChanges { get; set; }

    /// <summary>
    /// <para>变更的元数据类型（包括对象和枚举）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("metadata_type")]
    public string? MetadataType { get; set; }

    /// <summary>
    /// <para>变更的枚举</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("enum_value_changes")]
    public string[]? EnumValueChanges { get; set; }
}
