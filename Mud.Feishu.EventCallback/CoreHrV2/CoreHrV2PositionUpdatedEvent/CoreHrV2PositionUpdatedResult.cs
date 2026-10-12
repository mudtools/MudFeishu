// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 更新岗位事件
/// <para>飞书人事中「岗位信息被更新」时将触发此事件。注意：触发时间为岗位更新实际生效时间，如在 2022-01-01 更新岗位，岗位更新生效时间设置为 2022-05-01，事件将在 2022-05-01 进行推送。 注意事项：收到推送后，可通过【查询岗位信息】接口查询变更详情</para>
/// <para>事件类型:corehr.position.updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2PositionUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/position/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2PositionUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2PositionUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>岗位ID，可通过[查询岗位](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/position/query)接口去获取岗位详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("position_id")]
    public string? PositionId { get; set; }

    /// <summary>
    /// <para>发生变更的字段，包括以下预置字段及自定义字段：</para>
    /// <para>- "name"：名称</para>
    /// <para>- "code"：编码</para>
    /// <para>- "active"：状态</para>
    /// <para>- "department"：所属部门</para>
    /// <para>- "cost_center"：岗位默认成本中心</para>
    /// <para>- "job"：职务</para>
    /// <para>- "job_family"：序列</para>
    /// <para>- "job_level"：职级</para>
    /// <para>- "job_grade"：职等</para>
    /// <para>- "work_location"：工作地点</para>
    /// <para>- "employee_type"：人员类型</para>
    /// <para>- "working_hours_type"：工时制度</para>
    /// <para>- "direct_leader"：直属上级</para>
    /// <para>- "dotted_line_leader"：虚线上级</para>
    /// <para>- "is_key_position"：是否关键岗位</para>
    /// <para>- "description"：描述</para>
    /// <para>- "effective_time"：版本生效日期</para>
    /// <para>- "expiration_time"：版本过期时间</para>
    /// <para>自定义字段详细见[获取自定义字段列表](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/custom_field/query)</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `200`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("field_changes")]
    public string[]? FieldChanges { get; set; }
}
