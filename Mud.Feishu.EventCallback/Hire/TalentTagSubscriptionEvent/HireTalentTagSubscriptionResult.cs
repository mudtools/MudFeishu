// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.EventCallback.Hire;

/// <summary>
/// 人才标签订阅
/// <para>人才的标签、阶段或锁定状态发生变更时触发此事件。</para>
/// <para>事件类型:hire.talent.tag_subscription_v1</para>
/// <para>使用时请继承：<see cref="HireTalentTagSubscriptionEventHandler"/></para>
/// <para>文档地址：https://open.feishu.cn/document/hire-v1/candidate-management/talent/events/tag_subscription</para>
/// </summary>
[GenerateEventHandler(EventType = FeishuEventTypes.HireTalentTagSubscription, HandlerNamespace = Consts.HandlerNamespace,
              InheritedFrom = Consts.InheritedFrom, HeaderType = nameof(FeishuEventHeader))]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class HireTalentTagSubscriptionResult : IEventResult
{
    /// <summary>
    /// <para>人才 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>投递 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>变更事件类型。枚举值有：</para>
    /// <para>- 1：阶段变更</para>
    /// <para>- 2：锁定</para>
    /// <para>- 3：解锁</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("type")]
    public int? Type { get; set; }

    /// <summary>
    /// <para>标签信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("tag")]
    public HireTalentTag? Tag { get; set; }

    /// <summary>
    /// <para>锁定状态。枚举值有：</para>
    /// <para>- 1：未锁定</para>
    /// <para>- 2：锁定在其他职位</para>
    /// <para>- 3：锁定在当前职位</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("lock_status")]
    public int? LockStatus { get; set; }

    /// <summary>
    /// <para>投递阶段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("application_stage")]
    public HireTalentApplicationStage? ApplicationStage { get; set; }
}
