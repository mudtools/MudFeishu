// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Employees;

/// <summary>
/// 自定义字段
/// </summary>
public class CustomFieldValue
{
    /// <summary>
    /// 自定义字段类型 可选值有：1：多行文本 2：网页链接 3：枚举选项 4：人员 9：电话 10：多选枚举类型 11：人员列表
    /// </summary>
    [JsonPropertyName("field_type")]
    public string? FieldType { get; set; }

    /// <summary>
    /// 文本字段值
    /// </summary>
    [JsonPropertyName("text_value")]
    public EmployeeI18nContent? TextValue { get; set; }

    /// <summary>
    /// 网页链接字段值
    /// </summary>
    [JsonPropertyName("url_value")]
    public UrlValue? UrlValue { get; set; }

    /// <summary>
    /// 枚举字段值
    /// </summary>
    [JsonPropertyName("enum_value")]
    public EnumValue? EnumValue { get; set; }

    /// <summary>
    /// 人员字段值
    /// </summary>
    [JsonPropertyName("user_values")]
    public List<UserValue>? UserValues { get; set; }

    /// <summary>
    /// 自定义字段key
    /// </summary>
    [JsonPropertyName("field_key")]
    public string? FieldKey { get; set; }
}