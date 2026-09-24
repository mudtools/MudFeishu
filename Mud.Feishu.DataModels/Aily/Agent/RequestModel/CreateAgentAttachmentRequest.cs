// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Aily;

/// <summary>
/// 上传智能体附件（Create Agent Attachment）请求体
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Aily")]
public partial class CreateAgentAttachmentRequest
{
    /// <summary>
    /// <para>需要上传的文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。</para>
    /// <para>仅在 type=file 或 type=image 时需要传入；支持 png/jpg/pdf 格式，文件最大 40M，图片最大 5M。</para>
    /// <para>必填：否（type 为 file/image 时必传）</para>
    /// </summary>
    [FilePath]
    [JsonPropertyName("file")]
    public string? FilePath { get; set; }

    /// <summary>
    /// <para>附件类型</para>
    /// <para>- 当设置为 image 或者 file 时，file 必传，doc_url 不生效</para>
    /// <para>- 当设置为 feishu_doc 或者 bitable 时，doc_url 必传，file 不生效</para>
    /// <para>必填：是</para>
    /// <para>示例值：image</para>
    /// <para>可选值：image、file、feishu_doc、bitable</para>
    /// <para>最大长度：32</para>
    /// <para>最小长度：1</para>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// <para>云文档/多维表格 URL，仅在 type=bitable 或 feishu_doc 时需要传入</para>
    /// <para>必填：否（type 为 bitable/feishu_doc 时必传）</para>
    /// <para>示例值：https://bytedance.larkoffice.com/wiki/PRSdJR8NowW5gxguFGcm9Hpn</para>
    /// <para>最大长度：512</para>
    /// <para>最小长度：0</para>
    /// </summary>
    [JsonPropertyName("doc_url")]
    public string? DocUrl { get; set; }
}
