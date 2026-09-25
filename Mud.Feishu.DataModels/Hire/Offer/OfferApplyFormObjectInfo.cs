// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// Offer 申请表字段（Offer 申请表模块子项）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Hire")]
public class OfferApplyFormObjectInfo
{
    /// <summary>
    /// <para>字段 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    /// <summary>
    /// <para>字段名称</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("name")]
    public I18nName? Name { get; set; }

    /// <summary>
    /// <para>字段描述</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("description")]
    public I18nName? Description { get; set; }

    /// <summary>
    /// <para>所属模块 ID</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("module_id")]
    public string? ModuleId { get; set; }

    /// <summary>
    /// <para>是否为自定义字段：true 自定义字段 / false 系统预置字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_customized")]
    public bool? IsCustomized { get; set; }

    /// <summary>
    /// <para>是否必填：true 必填 / false 非必填</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_required")]
    public bool? IsRequired { get; set; }

    /// <summary>
    /// <para>字段启用状态：1 启用 / 2 停用</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("active_status")]
    public int? ActiveStatus { get; set; }

    /// <summary>
    /// <para>修改后是否需要审批：true 需要审批 / false 不需要审批</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("need_approve")]
    public bool? NeedApprove { get; set; }

    /// <summary>
    /// <para>是否敏感字段（敏感字段会在发起 Offer 审批时隐藏）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("is_sensitive")]
    public bool? IsSensitive { get; set; }

    /// <summary>
    /// <para>字段类型枚举（已废弃）：1 单行文本 / 2 多行文本 / 3 单选 / 4 多选 / 5 日期选择 / 6 月份选择 / 7 年份选择 / 8 数字 / 9 金额 / 10 公式 / 11 默认字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object_type")]
    public int? ObjectType { get; set; }

    /// <summary>
    /// <para>字段类型枚举：text 单行文本 / long_text 多行文本 / select 单选 / multi_select 多选 / date_select 日期选择 / month_select 月份选择 / year_select 年份选择 / number 数字 / amount 金额 / formula 公式 / boolean 布尔 / file 附件 / personnel_select 人员单选 / personnel_multi_select 人员多选 / city_single_select 城市单选 / corehr_* 前缀为引用 CoreHR 的同名类型 / default 默认字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("object_type_v2")]
    public string? ObjectTypeV2 { get; set; }

    /// <summary>
    /// <para>字段配置信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("config")]
    public OfferApplyFormObjectConfigInfo? Config { get; set; }
}
