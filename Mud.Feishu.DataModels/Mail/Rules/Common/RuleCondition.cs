// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Mail;

/// <summary>
/// <para>匹配条件</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Mail")]
public class RuleCondition
{
    /// <summary>
    /// <para>匹配类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>最大值：2</para>
    /// <para>最小值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：满足所有条件</item>
    /// <item>2：满足任意条件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("match_type")]
    public int MatchType { get; set; }

    /// <summary>
    /// <para>匹配规则列表</para>
    /// <para>必填：是</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("items")]
    public RuleConditionItem[] Items { get; set; } = [];
}
