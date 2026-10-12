// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 【事件】创建部门
/// <para>飞书人事中「部门被创建」时将触发此事件。 注意事项：- 使用场景：此事件为无序事件（创建有上下级关系的部门创建之后，上下级部门事件可能乱序），若对事件顺序无依赖则可以使用V1版本事件，否则请使用【创建部门事件V2】 【搜索部门信息】【获取父部门信息】 等接口数据查询存在一定延迟，不建议收到事件后立即查询以上接口。</para>
/// <para>事件类型:corehr.department.created_v1</para>
/// <para>使用时请继承：<see cref="CoreHrDepartmentCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/department/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrDepartmentCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrDepartmentCreatedResult : IEventResult
{
    /// <summary>
    /// <para>新建部门的 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("department_id")]
    public string? DepartmentId { get; set; }
}
