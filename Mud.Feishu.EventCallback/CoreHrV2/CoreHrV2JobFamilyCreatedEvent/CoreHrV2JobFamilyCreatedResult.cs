// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 【事件】创建序列
/// <para>飞书人事中「序列被创建」时将触发此事件。 注意事项：【查询租户的序列信息】、【获取单个序列信息 】、【通过序列 ID 批量查询序列信息 】接口数据查询存在一定延迟(2s内)，不建议收到事件后立即查询以上接口。 注意事项：- 该接口只会推送当前生效序列的变更事件。 未来生效的版本数据，会在生效日期当天凌晨推送事件。</para>
/// <para>事件类型:corehr.job_family.created_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2JobFamilyCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/job_family/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2JobFamilyCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2JobFamilyCreatedResult : IEventResult
{
    /// <summary>
    /// <para>序列ID。</para>
    /// <para>- 调用[【查询单个序列】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/job_family/get)、[【通过序列 ID 批量查询序列信息】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/job_family/batch_get)接口返回序列详细信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("job_family_id")]
    public string? JobFamilyId { get; set; }
}
