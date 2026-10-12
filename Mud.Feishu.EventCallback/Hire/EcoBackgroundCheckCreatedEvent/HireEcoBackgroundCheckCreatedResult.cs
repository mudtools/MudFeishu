// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 背调创建
/// <para>招聘系统客户发起背调后，系统向对应背调服务商推送「创建背调」事件。</para>
/// <para>事件类型:hire.eco_background_check.created_v1</para>
/// <para>使用时请继承：<see cref="HireEcoBackgroundCheckCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/hire-v1/ecological-docking/eco_background_check/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireEcoBackgroundCheckCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireEcoBackgroundCheckCreatedResult : IEventResult
{
    /// <summary>
    /// <para>背调 ID，在招聘系统中唯一；服务商可将其与内部背调订单绑定</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("background_check_id")]
    public string? BackgroundCheckId { get; set; }

    /// <summary>
    /// <para>账户 ID，在招聘系统中唯一，可通过「账号绑定」事件包获取</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>
    /// <para>套餐 ID，由「创建背调套餐及附加调查项」传入</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("package_id")]
    public string? PackageId { get; set; }

    /// <summary>
    /// <para>附加调查项 ID 列表，由「创建背调套餐及附加调查项」传入</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("additional_item_id_list")]
    public string[]? AdditionalItemIdList { get; set; }

    /// <summary>
    /// <para>备注</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    /// <summary>
    /// <para>候选人信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("candidate_info")]
    public HireEcoBackgroundCheckCandidateInfo? CandidateInfo { get; set; }

    /// <summary>
    /// <para>客户联系信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("client_contact_info")]
    public HireEcoBackgroundCheckContactInfo? ClientContactInfo { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_field_list")]
    public HireEcoBackgroundCheckCustomField[]? CustomFieldList { get; set; }
}
