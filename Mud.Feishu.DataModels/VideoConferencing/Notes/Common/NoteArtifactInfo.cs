// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>纪要产物</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class NoteArtifactInfo
{
    /// <summary>
    /// <para>纪要产物类型</para>
    /// <para>必填：是</para>
    /// <para>示例值：1</para>
    /// <para>可选值：<list type="bullet">
    /// <item>0：保留值（正常业务流程中服务端不会返回）</item>
    /// <item>1：纪要文档</item>
    /// <item>2：逐字稿文档</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("artifact_type")]
    public int ArtifactType { get; set; }

    /// <summary>
    /// <para>产物创建时间（unix时间，单位sec）</para>
    /// <para>必填：是</para>
    /// <para>示例值：1773922587</para>
    /// </summary>
    [JsonPropertyName("create_time")]
    public string CreateTime { get; set; } = string.Empty;

    /// <summary>
    /// <para>产物的doc token</para>
    /// <para>必填：是</para>
    /// <para>示例值：BkX1wpU0gi6WP4klwRGchoqZntv</para>
    /// </summary>
    [JsonPropertyName("doc_token")]
    public string DocToken { get; set; } = string.Empty;
}
