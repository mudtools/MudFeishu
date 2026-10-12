// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 账号创建
/// <para>当飞书招聘客户在「飞书招聘」-「设置」-「生态对接」-「笔试/背调」中添加三方服务商账号时，系统会向服务商推送「账号绑定」事件。服务商可通过此事件获取账号类型、飞书招聘账号 ID 及客户添加的账号自定义字段信息，从而完成客户服务商账号与飞书招聘账号的绑定。</para>
/// <para>事件类型:hire.eco_account.created_v1</para>
/// <para>使用时请继承：<see cref="HireEcoAccountCreatedEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/server-docs/hire-v1/ecological-docking/eco_account/events/created</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireEcoAccountCreated, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireEcoAccountCreatedResult : IEventResult
{
    /// <summary>
    /// <para>账号范围。枚举值有：</para>
    /// <para>- 1：背调账号</para>
    /// <para>- 2：笔试账号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("scope")]
    public int? Scope { get; set; }

    /// <summary>
    /// <para>账号 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("account_id")]
    public string? AccountId { get; set; }

    /// <summary>
    /// <para>账号名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("account_name")]
    public string? AccountName { get; set; }

    /// <summary>
    /// <para>账号用途范围。枚举值有：</para>
    /// <para>- 1：社招</para>
    /// <para>- 2：校招</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("usage_list")]
    public int[]? UsageList { get; set; }

    /// <summary>
    /// <para>自定义字段列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("custom_field_list")]
    public HireEcoAccountCustomField[]? CustomFieldList { get; set; }
}
