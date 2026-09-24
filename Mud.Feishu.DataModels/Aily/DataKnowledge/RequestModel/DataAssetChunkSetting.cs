// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// <para>知识切片配置</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Aily")]
public class DataAssetChunkSetting
{
    /// <summary>
    /// <para>切片规则</para>
    /// <para>必填：否</para>
    /// <para>示例值：intelligent</para>
    /// <para>可选值：<list type="bullet">
    /// <item>intelligent：按标识符智能切片</item>
    /// </list></para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("rule_type")]
    public string? RuleType { get; set; }

    /// <summary>
    /// <para>切片分割符类型</para>
    /// <para>必填：否</para>
    /// <para>示例值：paragraph</para>
    /// <para>可选值：<list type="bullet">
    /// <item>paragraph：段落分隔符："\n\n"、"\n"、空格</item>
    /// <item>hash：标题分割符：######</item>
    /// </list></para>
    /// <para>最大长度：255</para>
    /// </summary>
    [JsonPropertyName("separate_type")]
    public string? SeparateType { get; set; }

    /// <summary>
    /// <para>分段最大长度（字符），按标识符切片时必须填写</para>
    /// <para>必填：否</para>
    /// <para>示例值：600</para>
    /// <para>取值范围：200 ～ 1000</para>
    /// </summary>
    [JsonPropertyName("size")]
    public long? Size { get; set; }

    /// <summary>
    /// <para>分段重叠字符数，按标识符切片时必须填写，不能超过 size 的数值</para>
    /// <para>必填：否</para>
    /// <para>示例值：10</para>
    /// <para>取值范围：0 ～ 200</para>
    /// </summary>
    [JsonPropertyName("overlap")]
    public long? Overlap { get; set; }
}
