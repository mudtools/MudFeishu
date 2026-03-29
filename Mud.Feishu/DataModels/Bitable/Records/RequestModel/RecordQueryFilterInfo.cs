// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Bitable;

/// <summary>
/// <para>包含条件筛选信息的对象。了解 filter 填写指南和使用示例（如怎样同时使用 `and` 和 `or` 逻辑链接词），参考[记录筛选参数填写指南](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/bitable-v1/app-table-record/record-filter-guide)。</para>
/// </summary>
public class RecordQueryFilterInfo
{
    /// <summary>
    /// 目前仅支持使用一层 children 参数，不支持再次嵌套使用。
    /// </summary>
    [JsonPropertyName("children")]
    public RecordQueryFilterInfo[]? Children { get; set; }

    /// <summary>
    /// <para>表示条件之间的逻辑连接词，该字段必填，请忽略左侧必填列的否</para>
    /// <para>必填：否</para>
    /// <para>示例值：and</para>
    /// <para>最大长度：10</para>
    /// <para>最小长度：0</para>
    /// <para>可选值：<list type="bullet">
    /// <item>and：满足全部条件</item>
    /// <item>or：满足任一条件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("conjunction")]
    public string? Conjunction { get; set; }

    /// <summary>
    /// <para>筛选条件集合</para>
    /// <para>必填：否</para>
    /// <para>最大长度：50</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("conditions")]
    public RecordQueryCondition[]? Conditions { get; set; }
}
