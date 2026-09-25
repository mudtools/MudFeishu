// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 导入外部内推奖励请求体
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class CreateExternalReferralRewardRequest
{
    /// <summary>
    /// <para>内推人 ID（与入参 user_id_type 类型一致）</para>
    /// <para>必填：是</para>
    /// <para>示例值：on_94a1ee5551019f18cd73d9f111898cf2</para>
    /// </summary>
    [JsonPropertyName("referral_user_id")]
    public string? ReferralUserId { get; set; }

    /// <summary>
    /// <para>奖励创建人，管理员与内推人可见；若不传，默认为「外部系统」</para>
    /// <para>必填：否</para>
    /// <para>示例值：on_94a1ee5551019f18cd73d9f111898cf2</para>
    /// </summary>
    [JsonPropertyName("create_user_id")]
    public string? CreateUserId { get; set; }

    /// <summary>
    /// <para>奖励确认人；导入状态为「已确认」时可传入，若不传，默认为「外部系统」</para>
    /// <para>必填：否</para>
    /// <para>示例值：on_94a1ee5551019f18cd73d9f111898cf2</para>
    /// </summary>
    [JsonPropertyName("confirm_user_id")]
    public string? ConfirmUserId { get; set; }

    /// <summary>
    /// <para>奖励发放人；导入状态为「已发放」时可传入，若不传，默认为「外部系统」</para>
    /// <para>必填：否</para>
    /// <para>示例值：on_94a1ee5551019f18cd73d9f111898cf2</para>
    /// </summary>
    [JsonPropertyName("pay_user_id")]
    public string? PayUserId { get; set; }

    /// <summary>
    /// <para>外部系统奖励唯一 ID（仅用于幂等）</para>
    /// <para>必填：是</para>
    /// <para>示例值：6930815272790114324</para>
    /// </summary>
    [JsonPropertyName("external_id")]
    public string? ExternalId { get; set; }

    /// <summary>
    /// <para>被内推候选人的投递 ID（推荐填飞书招聘的投递 ID）；与 talent_id 二选一，均不填时奖励无法关联投递，职位/招聘负责人/Offer 负责人字段显示为「--」</para>
    /// <para>必填：否</para>
    /// <para>示例值：6930815272790114325</para>
    /// </summary>
    [JsonPropertyName("application_id")]
    public string? ApplicationId { get; set; }

    /// <summary>
    /// <para>被内推候选人的人才 ID；未填 application_id 时必填</para>
    /// <para>必填：否</para>
    /// <para>示例值：6930815272790114326</para>
    /// </summary>
    [JsonPropertyName("talent_id")]
    public string? TalentId { get; set; }

    /// <summary>
    /// <para>内推职位 ID；未填时无职位权限的相关角色看不到该内推记录</para>
    /// <para>必填：否</para>
    /// <para>示例值：6930815272790114327</para>
    /// </summary>
    [JsonPropertyName("job_id")]
    public string? JobId { get; set; }

    /// <summary>
    /// <para>奖励原因；为空时展示为「--」，管理员与内推人可见</para>
    /// <para>必填：否</para>
    /// <para>示例值：Open source</para>
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }

    /// <summary>
    /// <para>导入的奖励规则类型：1 入职奖励 / 2 过程奖励 / 3 活动奖励 / 4 首推奖励 / 5 其他奖励</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("rule_type")]
    public int? RuleType { get; set; }

    /// <summary>
    /// <para>奖励额度（bonus_type 与 stage 同级必填结构）</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("bonus")]
    public ReferralRewardBonus? Bonus { get; set; }

    /// <summary>
    /// <para>导入的内推奖励状态：1 待确认 / 2 已确认 / 3 已发放</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("stage")]
    public int? Stage { get; set; }

    /// <summary>
    /// <para>奖励产生时间，毫秒时间戳；若未传入，取接口调用时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1704720275000</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string? CreateTime { get; set; }

    /// <summary>
    /// <para>奖励确认时间，毫秒时间戳；状态为「已确认」时可传入，若未传入，取接口调用时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1704720275000</para>
    /// </summary>
    [JsonPropertyName("confirm_time")]
    public string? ConfirmTime { get; set; }

    /// <summary>
    /// <para>奖励发放时间，毫秒时间戳；状态为「已发放」时可传入，若未传入，取接口调用时间</para>
    /// <para>必填：否</para>
    /// <para>示例值：1704720275001</para>
    /// </summary>
    [JsonPropertyName("pay_time")]
    public string? PayTime { get; set; }

    /// <summary>
    /// <para>入职时间，毫秒时间戳；管理员与内推人可见，规则类型为「入职奖励」时建议传入</para>
    /// <para>必填：否</para>
    /// <para>示例值：1704720275002</para>
    /// </summary>
    [JsonPropertyName("onboard_time")]
    public string? OnboardTime { get; set; }

    /// <summary>
    /// <para>转正时间，毫秒时间戳；管理员与内推人可见，规则类型为「入职奖励」时建议传入</para>
    /// <para>必填：否</para>
    /// <para>示例值：1704720275003</para>
    /// </summary>
    [JsonPropertyName("conversion_time")]
    public string? ConversionTime { get; set; }

    /// <summary>
    /// <para>操作备注，管理员与内推人可见；为空时展示奖励原因</para>
    /// <para>必填：否</para>
    /// <para>示例值：Issuance</para>
    /// </summary>
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }
}
