// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Lingo;

/// <summary>
/// 词条的更多相关信息（related_meta）
/// </summary>
[HttpJsonSerializable(SerializerClassName = "Lingo")]
public class LingoRelatedMeta
{
    /// <summary>
    /// <para>相关联系人</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("users")]
    public LingoReferer[]? Users { get; set; }

    /// <summary>
    /// <para>关联公开群组信息</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("chats")]
    public LingoReferer[]? Chats { get; set; }

    /// <summary>
    /// <para>飞书文档或飞书 wiki</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("docs")]
    public LingoReferer[]? Docs { get; set; }

    /// <summary>
    /// <para>相关服务中的相关值班号</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("oncalls")]
    public LingoReferer[]? Oncalls { get; set; }

    /// <summary>
    /// <para>相关链接</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("links")]
    public LingoReferer[]? Links { get; set; }

    /// <summary>
    /// <para>相关词条</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("abbreviations")]
    public LingoAbbreviation[]? Abbreviations { get; set; }

    /// <summary>
    /// <para>当前词条所属分类；词条只能属于二级分类，且每个一级分类下只能选择一个二级分类</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("classifications")]
    public LingoClassification[]? Classifications { get; set; }

    /// <summary>
    /// <para>上传的相关图片，最多 10 张</para>
    /// <para>必填：否</para>
    /// </summary>
    [JsonPropertyName("images")]
    public LingoBaikeImage[]? Images { get; set; }
}
