// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.EmployeeType;

/// <summary>
/// 人员类型信息
/// </summary>
public class EmployeeTypeEnum
{
    /// <summary>
    /// 枚举 ID。
    /// </summary>
    [JsonPropertyName("enum_id")]
    public string? EnumId { get; set; }

    /// <summary>
    /// 枚举值。
    /// </summary>
    [JsonPropertyName("enum_value")]
    public string? EnumValue { get; set; }

    /// <summary>
    /// 枚举内容。
    /// </summary>
    [JsonPropertyName("content")]
    public string? Content { get; set; }

    /// <summary>
    /// 枚举类型。
    /// <para>0：系统预定义枚举</para>
    /// <para>1：用户自定义枚举</para>
    /// </summary>
    [JsonPropertyName("enum_type")]
    public int EnumType { get; set; }

    /// <summary>
    /// 枚举状态。
    /// <para>0：已停用</para>
    /// <para>1：启用</para>
    /// </summary>
    [JsonPropertyName("enum_status")]
    public int EnumStatus { get; set; }

    /// <summary>
    /// 枚举内容的国际化配置。
    /// </summary>
    [JsonPropertyName("i18n_content")]
    public List<I18nContent>? I18nContent { get; set; }
}