// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;


/// <summary>
/// <para>记录筛选条件，当 `table_perm` 为 1 或 2 时生效。用于指定可编辑或可阅读的记录。</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Bitable")]
public class TableRoleRecRule
{
    /// <summary>
    /// <para>记录筛选条件，用于指定可编辑或可阅读的记录。</para>
    /// <para>必填：否</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("conditions")]
    public RecRuleCondition[]? Conditions { get; set; }

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
    /// <para>其他记录权限，仅在 `table_perm` 为 2 （数据表权限为可编辑）时生效。</para>
    /// <para>- 当 `other_perm` 为 1 时，表示未命中 `rec_rule` 的记录仅可阅读，不可编辑</para>
    /// <para>- 当 `other_perm` 为 0 时，表示既未命中 `rec_rule`、也未命中 `other_rec_rule` 的记录会被禁止阅读。即你可以通过 `other_rec_rule` 进一步指定可阅读的记录范围。</para>
    /// <para>必填：否</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：禁止查看</item>
    /// <item>1：仅可阅读</item>
    /// </list></para>
    /// <para>默认值：0</para>
    /// </summary>
    [JsonPropertyName("other_perm")]
    public int? OtherPerm { get; set; }
}
