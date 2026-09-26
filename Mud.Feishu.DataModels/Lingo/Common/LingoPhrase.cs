// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 词条高亮接口识别出的词条（phrase）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class LingoPhrase
{
    /// <summary>
    /// <para>文本中切分出的百科词条名称</para>
    /// <para>必填：否</para>
    /// <para>示例值：企业百科</para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>实体词 id 列表</para>
    /// <para>必填：否</para>
    /// <para>示例值：["enterprise_51587960"]</para>
    /// </summary>
    [JsonPropertyName("entity_ids")]
    public string[]? EntityIds { get; set; }

    /// <summary>
    /// <para>词条所在位置</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("span")]
    public LingoSpan? Span { get; set; }
}
