// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AttendanceGroups.Common;

/// <summary>
/// <para>圈人规则列表</para>
/// </summary>
public class AttendanceScopeGroup
{
    /// <summary>
    /// <para>**类型**：</para>
    /// <para>* 1: 部门</para>
    /// <para>* 2:人员</para>
    /// <para>* 3: 国家地区</para>
    /// <para>* 4: 员工类型</para>
    /// <para>* 5: 工作城市</para>
    /// <para>* 6: 职级</para>
    /// <para>* 7: 序列</para>
    /// <para>* 8: 职务（企业版）</para>
    /// <para>* 9: 工时制度（企业版）</para>
    /// <para>* 100: 自定义字段（企业版）</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("scope_value_type")]
    public int? ScopeValueType { get; set; }

    /// <summary>
    /// <para>范围类型（是否包含）</para>
    /// <para>* 1: 包含</para>
    /// <para>* 2: 不包含</para>
    /// <para>* 3: 相等</para>
    /// <para>* 4: 小于等于</para>
    /// <para>* 5: 大于等于</para>
    /// <para>* 6: 大于</para>
    /// <para>* 7: 小于</para>
    /// <para>* 8: 不相等</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// </summary>
    [JsonPropertyName("operation_type")]
    public int? OperationType { get; set; }

    /// <summary>
    /// <para>如果是人员/部门类型 不需要使用该字段</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("right")]
    public AttendanceScopeValue[]? Rights { get; set; }

    /// <summary>
    /// <para>部门/人员 ID 列表（根据 scope_value_type 判断为部门或人员）</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("member_ids")]
    public string[]? MemberIds { get; set; }

    /// <summary>
    /// <para>企业版自定义字段唯一键 ID, 需要从飞书人事获取（暂不支持）</para>
    /// <para>必填：否</para>
    /// <para>示例值：123213123</para>
    /// </summary>
    [JsonPropertyName("custom_field_ID")]
    public string? CustomFieldID { get; set; }

    /// <summary>
    /// <para>企业版自定义字段对象类型（暂不支持）</para>
    /// <para>* "Employment": 主数据对象，员工雇佣信息</para>
    /// <para>* "Person": 主数据对象，个人</para>
    /// <para>必填：否</para>
    /// <para>示例值：employment</para>
    /// </summary>
    [JsonPropertyName("custom_field_obj_type")]
    public string? CustomFieldObjType { get; set; }
}

/// <summary>
/// <para>如果是人员/部门类型 不需要使用该字段</para>
/// </summary>
public class AttendanceScopeValue
{
    /// <summary>
    /// <para>标识Key</para>
    /// <para>必填：否</para>
    /// <para>示例值：CH</para>
    /// </summary>
    [JsonPropertyName("key")]
    public string? Key { get; set; }

    /// <summary>
    /// <para>名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：中国大陆</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}