
using Mud.Feishu.DataModels.Employees;

namespace Mud.Feishu.DataModels.DepartmentsV1;

/// <summary>
/// 部门信息
/// </summary>
public class DepartmentInfo
{
    /// <summary>
    /// 标识租户内一个唯一的部门，支持自定义，未自定义时系统自动生成。ID支持修改。注意：
    /// <para>除需要满足正则规则外，同时不能以od-开头</para>
    /// <para>正则校验：^[a-zA-Z0-9][a-zA-Z0-9_-@.]{0,63}$</para>
    /// </summary>
    [JsonPropertyName("custom_department_id")]
    public string? CustomDepartmentId { get; set; }

    /// <summary>
    /// 部门名称，最多可输入 100 字
    /// </summary>
    [JsonPropertyName("name")]
    public I18nContents? Name { get; set; }

    /// <summary>
    /// 父部门ID，与department_id_type类型保持一致。如果父部门为根部门，该参数值为 “0”
    /// </summary>
    [JsonPropertyName("parent_department_id")]
    public string? ParentDepartmentId { get; set; }

    /// <summary>
    /// 部门负责人
    /// </summary>
    [JsonPropertyName("leaders")]
    public List<DepartmentLeader>? Leaders { get; set; } = [];

    /// <summary>
    /// 在上级部门下的排序权重，返回结果按order_weight降序排列
    /// </summary>
    [JsonPropertyName("order_weight")]
    public string? OrderWeight { get; set; }

    /// <summary>
    /// 是否启用
    /// </summary>
    [JsonPropertyName("enabled_status")]
    public bool EnabledStatus { get; set; }

    /// <summary>
    /// 部门自定义字段值
    /// </summary>
    [JsonPropertyName("custom_field_values")]
    public List<CustomFieldValue>? CustomFieldValues { get; set; } = [];
}