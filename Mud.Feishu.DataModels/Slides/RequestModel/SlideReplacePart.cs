// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Slides;

/// <summary>
/// 页面元素级替换操作项
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Slides")]
public class SlideReplacePart
{
    /// <summary>
    /// <para>操作类型：block_replace（块替换）、block_insert（块插入）</para>
    /// <para>必填：是</para>
    /// <para>示例值：block_replace</para>
    /// </summary>
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    /// <summary>
    /// <para>替换内容，block_replace 时使用</para>
    /// <para>必填：否</para>
    /// <para>示例值：test from1</para>
    /// </summary>
    [JsonPropertyName("replacement")]
    public string? Replacement { get; set; }

    /// <summary>
    /// <para>目标块的唯一标识，block_replace 时使用，格式为 3 位 short element ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：bab</para>
    /// </summary>
    [JsonPropertyName("block_id")]
    public string? BlockId { get; set; }

    /// <summary>
    /// <para>要插入的 XML 元素片段，block_insert 时使用</para>
    /// <para>必填：否</para>
    /// <para>示例值：&lt;shape&gt;&lt;p&gt;inserted element&lt;/p&gt;&lt;/shape&gt;</para>
    /// </summary>
    [JsonPropertyName("insertion")]
    public string? Insertion { get; set; }

    /// <summary>
    /// <para>在指定块之前插入，为空时追加到末尾，block_insert 时使用，格式为 3 位 short element ID</para>
    /// <para>必填：否</para>
    /// <para>示例值：baa</para>
    /// </summary>
    [JsonPropertyName("insert_before_block_id")]
    public string? InsertBeforeBlockId { get; set; }
}
