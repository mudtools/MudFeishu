// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.CoreHrV2;

/// <summary>
/// 电子签文件状态变更事件
/// <para>当电子签文件状态发生变更的时候，会推送变更事件，包含文件变更前后的状态等信息</para>
/// <para>事件类型:corehr.signature_file.status_updated_v2</para>
/// <para>使用时请继承：<see cref="CoreHrV2SignatureFileStatusUpdatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/signature_file/events/status_updated</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.CoreHrV2SignatureFileStatusUpdated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "CoreHrV2")]
public class CoreHrV2SignatureFileStatusUpdatedResult : IEventResult
{
    /// <summary>
    /// <para>发生状态变更的电子签文件id，[【查询电子签文件详情】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/corehr-v2/signature_file/query)</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("signature_file_id")]
    public string? SignatureFileId { get; set; }

    /// <summary>
    /// <para>变更前的电子签文件状态，枚举值可通过文档[【飞书人事枚举常量】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/feishu-people-enum-constant)电子签文件状态（signature_file_state）枚举定义部分获得</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("before_status")]
    public string? BeforeStatus { get; set; }

    /// <summary>
    /// <para>变更后的电子签文件状态，枚举值可通过文档[【飞书人事枚举常量】](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/corehr-v1/feishu-people-enum-constant)电子签文件状态（signature_file_state）枚举定义部分获得</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("after_status")]
    public string? AfterStatus { get; set; }

    /// <summary>
    /// <para>业务id, 由接入业务自行维护,可以通过该流程id获取该流程下的签署文件信息list</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("biz_process_id")]
    public string? BizProcessId { get; set; }
}
