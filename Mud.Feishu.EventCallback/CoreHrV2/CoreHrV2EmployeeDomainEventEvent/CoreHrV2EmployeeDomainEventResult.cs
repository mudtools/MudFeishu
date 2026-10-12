// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 人员信息变更
/// <para>人员领域事件变更，通过业务界面、开放平台接口对个人信息、工作信息（雇佣信息）、任职信息、兼职信息等进行操作时会触发相应事件 注意事项：- 【批量查询员工信息】、【搜索员工信息】 等数据查询存在一定延迟，不建议收到事件后立即查询以上接口。请在收到事件后，延迟 5~10 秒后查询。</para>
/// <para>事件类型:corehr.employee.domain_event_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2EmployeeDomainEventEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/events/domain_event</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2EmployeeDomainEvent, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2EmployeeDomainEventResult : IEventResult
{
    /// <summary>
    /// <para>事件类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1：员工入职</para>
    /// <para>- 2：更新人员信息</para>
    /// <para>- 4：员工兼职变动</para>
    /// <para>- 5：员工主任职变动</para>
    /// <para>- 7：员工离职</para>
    /// <para>- 8：自定义组织记录变动</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("event_type")]
    public int? EventType { get; set; }

    /// <summary>
    /// <para>子事件类型</para>
    /// <para>**可选值有**：</para>
    /// <para>- 1001：新入职</para>
    /// <para>- 2001：更新人员信息</para>
    /// <para>- 4001：创建兼职</para>
    /// <para>- 4002：编缉兼职（更新兼职的任一字段就会触发该事件，包括更新【结束日期】）</para>
    /// <para>- 4003：撤销兼职（弃用）</para>
    /// <para>- 4004：结束兼职（目前含义为数据变更，即首次设置【结束日期】时会触发）</para>
    /// <para>- 4005：删除兼职</para>
    /// <para>- 5001：添加异动</para>
    /// <para>- 5002：编辑异动信息</para>
    /// <para>- 5003：撤销异动</para>
    /// <para>- 7001：离职通过</para>
    /// <para>- 7002：撤销离职</para>
    /// <para>- 8001：新增第一条记录</para>
    /// <para>- 8002：新增第一条之后的自定义组织记录</para>
    /// <para>- 8003：编辑自定义组织记录</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("sub_event_type")]
    public int? SubEventType { get; set; }

    /// <summary>
    /// <para>操作用户 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("operator_user_id")]
    public string? OperatorUserId { get; set; }

    /// <summary>
    /// <para>业务场景</para>
    /// <para>- 标识触发此事件操作场景：系统页面操作、开发平台接口操作等</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("opt_scene")]
    public string? OptScene { get; set; }

    /// <summary>
    /// <para>操作描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("opt_desc")]
    public string? OptDesc { get; set; }

    /// <summary>
    /// <para>操作时间戳，单位：秒</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("opt_time")]
    public string? OptTime { get; set; }

    /// <summary>
    /// <para>操作 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("opt_id")]
    public string? OptId { get; set; }

    /// <summary>
    /// <para>被操作人的雇佣 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>变更数据</para>
    /// <para>**数据校验规则**：</para>
    /// <para>- 长度范围：`0` ～ `1000`</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("data")]
    public CoreHrV2EmployeeDomainEventData[]? Data { get; set; }
}
