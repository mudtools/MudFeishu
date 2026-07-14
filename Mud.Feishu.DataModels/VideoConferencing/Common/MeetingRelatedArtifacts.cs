// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.VideoConferencing;


/// <summary>
/// <para>会议产物相关信息</para>
/// </summary>
[HttpJsonSerializable(SerializerClassName = "VideoConferencing")]
public class MeetingRelatedArtifacts
{
    /// <summary>
    /// <para>智能纪要的 doc_token</para>
    /// <para>- 文档一旦生成，就可以查到对应 token</para>
    /// <para>- 无字段权限时，该 key 不会出现在 related_artifacts 结构当中</para>
    /// <para>- 有字段权限而无内容时，related_artifacts 结构中会包含该 key，同时其值为空字符串</para>
    /// <para>必填：否</para>
    /// <para>示例值：J1X5wG7bFilbFDk42VNdhfS6n6g</para>
    /// </summary>
    [JsonPropertyName("note_doc_token")]
    public string? NoteDocToken { get; set; }

    /// <summary>
    /// <para>逐字稿的 doc_token</para>
    /// <para>- 文档一旦生成，就可以查到对应 token</para>
    /// <para>- 无字段权限时，该 key 不会出现在 related_artifacts 结构当中</para>
    /// <para>- 有字段权限而无内容时，related_artifacts 结构中会包含该 key，同时其值为空字符串</para>
    /// <para>必填：否</para>
    /// <para>示例值：J1X5wG7bFilbFDk42VNdhfS6n6g</para>
    /// </summary>
    [JsonPropertyName("verbatim_doc_token")]
    public string? VerbatimDocToken { get; set; }
}
