// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 组织角色授权变更
/// <para>当组织上的角色授权发生变更时，触发该事件。例如在部门上修改了角色，并在 2030-01-01 年生效，则事件将在 2030-01-01 触发。注意：当前事件只返回在飞书人事中组织角色的变化，下游组织的影响，可以通过 「获取组织类角色授权列表」获取。</para>
/// <para>事件类型:corehr.org_role_authorization.updated_v1</para>
/// <para>使用时请继承：<see cref="CoreHrOrgRoleAuthorizationUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/org_role_authorization/events/updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrOrgRoleAuthorizationUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrOrgRoleAuthorizationUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>角色id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("role_id")]
    public string? RoleId { get; set; }

    /// <summary>
    /// <para>管理范围信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("management_scope_list")]
    public CoreHrManagementScope[]? ManagementScopeList { get; set; }

    /// <summary>
    /// <para>员工雇佣信息ID list</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id_list")]
    public string[]? EmploymentIdList { get; set; }
}
