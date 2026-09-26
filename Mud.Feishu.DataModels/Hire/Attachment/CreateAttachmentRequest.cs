// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Hire;

/// <summary>
/// 创建附件请求体（multipart/form-data，仅含一个文件字段 content）
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Hire")]
public partial class CreateAttachmentRequest
{
    /// <summary>
    /// <para>需要上传的文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。</para>
    /// <para>对应 multipart 字段名 content；文件大小不得超过 300 MB。</para>
    /// <para>必填：是</para>
    /// </summary>
    [FilePath]
    [JsonPropertyName("content")]
    public string? FilePath { get; set; }
}
