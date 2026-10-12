// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 用户ID映射变更
/// <para>用户ID映射变更事件 注意事项：你需要在应用中配置事件订阅，这样才可以在事件触发时接收到事件数据。了解事件订阅可参见事件订阅概述。</para>
/// <para>事件类型:corehr.common_data.id.user_mapping_changed_v1</para>
/// <para>使用时请继承：<see cref="CoreHrUserIdMappingChangedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/common_data-id/events/user_mapping_changed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrUserIdMappingChanged, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrUserIdMappingChangedResult : IEventResult
{
    /// <summary>
    /// <para>变更类型。insert：创建用户ID映射，delete：删除用户ID映射。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("change_type")]
    public string? ChangeType { get; set; }

    /// <summary>
    /// <para>ID转换类型。1:飞书人事&lt;-&gt;飞书，2:飞书人事&lt;-&gt;PeopleAdmin，3:PeopleAdmin&lt;-&gt;飞书。</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 取值范围：`1` ～ `3`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id_transform_type")]
    public int? IdTransformType { get; set; }

    /// <summary>
    /// <para>飞书人事ID，仅id_transform_type等于1、2时有值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("corehr_id")]
    public string? CorehrId { get; set; }

    /// <summary>
    /// <para>PeopleAdmin ID，仅id_transform_type等于2、3时有值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("people_admin_id")]
    public string? PeopleAdminId { get; set; }

    /// <summary>
    /// <para>飞书用户ID，仅id_transform_type等于1、3时有值。</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("feishu_id")]
    public UserIdInfo? FeishuId { get; set; }
}
