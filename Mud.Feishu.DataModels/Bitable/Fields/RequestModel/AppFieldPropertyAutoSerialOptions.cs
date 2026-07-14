// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>自动编号规则列表</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class AppFieldPropertyAutoSerialOptions
{
    /// <summary>
    /// <para>自动编号的可选规则项类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：created_time</para>
    /// <para>可选值：<list type="bullet">
    /// <item>system_number：自增数字位,value范围1-9</item>
    /// <item>fixed_text：固定字符，最大长度：20</item>
    /// <item>created_time：创建时间，支持格式 "yyyyMMdd"、"yyyyMM"、"yyyy"、"MMdd"、"MM"、"dd"</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>与自动编号的可选规则项类型相对应的取值</para>
    /// <para>必填：是</para>
    /// <para>示例值：yyyyMMdd</para>
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;
}
