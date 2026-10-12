// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHr;

/// <summary>
/// 员工完成入职
/// <para>以下业务场景会触发此事件： 开放平台操作员工完成入职接口 开放平台添加人员接口 「飞书人事-人员管理-入职」将待入职员工操作“完成入职” 「飞书人事-人员管理-花名册」操作”添加人员”或”导入人员”</para>
/// <para>事件类型:corehr.job_data.employed_v1</para>
/// <para>使用时请继承：<see cref="CoreHrJobDataEmployedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/job_data/events/employed</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrJobDataEmployed, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHr")]
public class CoreHrJobDataEmployedResult : IEventResult
{
    /// <summary>
    /// <para>任职记录 ID，详细信息可通过[【获取任职记录】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/job_data/get)接口查询</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_data_id")]
    public string? JobDataId { get; set; }

    /// <summary>
    /// <para>雇佣记录 ID，详细信息可通过[【查询员工信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/employee/batch_get)接口查询</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("employment_id")]
    public string? EmploymentId { get; set; }

    /// <summary>
    /// <para>用户 ID（备注：入职后开通飞书账号成功，会生成飞书通讯录相关ID，但由于开通过程是异步，本事件不保证一定返回，如未返回，说明飞书账号开通失败，业务需兼容处理）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("target_user_id")]
    public UserIdInfo? TargetUserId { get; set; }
}
