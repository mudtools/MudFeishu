// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 猎头供应商（搜索猎头供应商列表响应子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class AgencySupplier
{
    /// <summary>
    /// <para>猎头供应商 ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：7398493486516799788</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>猎头供应商名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：北极无敌猎头</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>猎头标签列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("label_list")]
    public AgencySupplierLabel[]? LabelList { get; set; }

    /// <summary>
    /// <para>管理员列表</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("admin_list")]
    public AgencySupplierAdmin[]? AdminList { get; set; }

    /// <summary>
    /// <para>猎头简历保护期：候选人在「猎头简历保护期」内入职需支付猎头费用，且保护期内无法被其他猎头公司推荐（猎头公司可重复推荐）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("agency_protect_time")]
    public AgencySupplierProtectTime? AgencyProtectTime { get; set; }

    /// <summary>
    /// <para>合作创建时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1639992265035</para>
    /// </summary>
    [JsonPropertyName("cooperation_create_time")]
    public string? CooperationCreateTime { get; set; }

    /// <summary>
    /// <para>合作开始时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1639992265035</para>
    /// </summary>
    [JsonPropertyName("cooperation_start_time")]
    public string? CooperationStartTime { get; set; }

    /// <summary>
    /// <para>合作终止时间，毫秒时间戳</para>
    /// <para>必填：否</para>
    /// <para>示例值：1639992265035</para>
    /// </summary>
    [JsonPropertyName("cooperation_end_time")]
    public string? CooperationEndTime { get; set; }

    /// <summary>
    /// <para>合作状态：1 正式合作 / 2 试用单 / 3 合作终止 / 4 邀请中</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("cooperation_status")]
    public int? CooperationStatus { get; set; }

    /// <summary>
    /// <para>供应商邮箱</para>
    /// <para>必填：否</para>
    /// <para>示例值：28933718393.qq.com</para>
    /// </summary>
    [JsonPropertyName("invite_email")]
    public string? InviteEmail { get; set; }

    /// <summary>
    /// <para>猎头地区：1 中国大陆 / 2 非中国大陆</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("supplier_area")]
    public int? SupplierArea { get; set; }

    /// <summary>
    /// <para>企业自有简历保护期：猎头无法推荐在保护期内活跃的候选人，也无法推荐活跃流程中的候选人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("talent_protect_time")]
    public AgencySupplierTalentProtectTime? TalentProtectTime { get; set; }
}
