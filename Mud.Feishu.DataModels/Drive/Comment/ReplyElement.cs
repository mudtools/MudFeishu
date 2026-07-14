// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive;

/// <summary>
/// <para>回复的内容</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Drive")]
public class ReplyElement
{
    /// <summary>
    /// <para>回复的内容元素</para>
    /// <para>必填：是</para>
    /// <para>示例值：text_run</para>
    /// <para>可选值：<list type="bullet">
    /// <item>text_run：普通文本</item>
    /// <item>docs_link：at 云文档链接</item>
    /// <item>person：at 联系人</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>文本内容</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("text_run")]
    public ReplyElementTextRun? TextRun { get; set; }

    /// <summary>
    /// <para>添加云文档链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("docs_link")]
    public ReplyElementDocsLink? DocsLink { get; set; }

    /// <summary>
    /// <para>添加用户的 user_id</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("person")]
    public ReplyElementPerson? Person { get; set; }
}
