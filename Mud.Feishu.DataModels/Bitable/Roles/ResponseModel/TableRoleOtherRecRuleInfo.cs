// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>记录筛选条件，在 `rec_rule.other_perm` 为 0 时生效。对于未命中 `rec_rule` 的记录，通过 `other_rec_rule` 指定可阅读记录范围；此时，既未命中 `rec_rule`、也未命中 `other_rec_rule` 的记录会被禁止阅读。</para>
/// </summary>
public class TableRoleOtherRecRuleInfo
{
    /// <summary>
    /// <para>记录筛选条件，用于指定可阅读的记录。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("conditions")]
    public RecRuleConditionInfo[]? Conditions { get; set; }



    /// <summary>
    /// <para>多个筛选条件的关系</para>
    /// <para>必填：否</para>
    /// <para>示例值：and</para>
    /// <para>可选值：<list type="bullet">
    /// <item>and：与</item>
    /// <item>or：或</item>
    /// </list></para>
    /// <para>默认值：and</para>
    /// </summary>
    [JsonPropertyName("conjunction")]
    public string? Conjunction { get; set; }

    /// <summary>
    /// <para>规则筛选记录对应的权限</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>1：仅可阅读</item>
    /// <item>2：可编辑</item>
    /// </list></para>
    /// <para>默认值：1</para>
    /// </summary>
    [JsonPropertyName("perm")]
    public int? Perm { get; set; }
}