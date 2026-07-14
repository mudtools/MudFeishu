// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.Drive.Media;

/// <summary>
/// 上传素材请求体
/// <para>将文件、图片、视频等素材上传到指定云文档中。素材将显示在对应云文档中，在云空间中不会显示。</para>
/// <para>## 使用限制</para>
/// <para>- 素材大小不得超过 20 MB。要上传大于 20 MB 的文件，你需使用分片上传素材相关接口。详情参考[素材概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/media/introduction)。</para>
/// <para>- 该接口调用频率上限为 5 QPS，10000 次/天。</para>
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "Drive")]
public partial class MediasUploadAllRequest
{

    /// <summary>
    /// <para>要上传的素材的名称</para>
    /// <para>必填：是</para>
    /// <para>示例值：demo.jpeg</para>
    /// <para>最大长度：250</para>
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// <para>上传点的类型。你可根据上传的素材类型与云文档类型确定上传点类型。例如，要将一张图片插入到新版文档（文件类型为 `docx`）中，需指定上传点为 `docx_image `；要将一个附件上传到新版文档中，需指定上传点为 `docx_file`。</para>
    /// <para>必填：是</para>
    /// <para>示例值：docx_image</para>
    /// <para>可选值：<list type="bullet">
    /// <item>doc_image：旧版文档图片</item>
    /// <item>docx_image：新版文档图片</item>
    /// <item>sheet_image：电子表格图片</item>
    /// <item>doc_file：旧版文档文件</item>
    /// <item>docx_file：新版文档文件</item>
    /// <item>sheet_file：电子表格文件</item>
    /// <item>vc_virtual_background：vc 虚拟背景（灰度中，暂未开放）</item>
    /// <item>bitable_image：多维表格图片</item>
    /// <item>bitable_file：多维表格文件</item>
    /// <item>moments：同事圈（灰度中，暂未开放）</item>
    /// <item>ccm_import_open：云文档导入文件</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("parent_type")]
    public string ParentType { get; set; } = string.Empty;

    /// <summary>
    /// <para>上传点的 token，即要上传的云文档的 token，用于指定素材将要上传到的云文档或位置。参考[素材概述](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/media/introduction)了解上传点类型与上传点 token 的对应关系</para>
    /// <para>必填：是</para>
    /// </summary>
    [JsonPropertyName("parent_node")]
    public string ParentNode { get; set; } = string.Empty;

    /// <summary>
    /// <para>文件的大小，单位为字节</para>
    /// <para>必填：是</para>
    /// <para>示例值：1024</para>
    /// <para>最大值：20971520</para>
    /// </summary>
    [JsonPropertyName("size")]
    public int Size { get; set; }

    /// <summary>
    /// <para>文件的 Adler-32 校验和</para>
    /// <para>必填：否</para>
    /// <para>示例值：3248270248</para>
    /// </summary>
    [JsonPropertyName("checksum")]
    public string? Checksum { get; set; }

    /// <summary>
    /// <para>以下场景的上传点需通过该参数传入素材所在云文档的 token。extra 参数的格式为`"{\"drive_route_token\":\"素材所在云文档的 token\"}"`。详情参考[素材概述-extra 参数说明](https://open.feishu.cn/document/uAjLw4CM/ukTMukTMukTM/reference/drive-v1/media/introduction#3b8635d3)。</para>
    /// <para>必填：否</para>
    /// <para>示例值：{\"drive_route_token\":\"doxcnXgNGAtaAraIRVeCfmabcef\"}</para>
    /// </summary>
    [JsonPropertyName("extra")]
    public string? Extra { get; set; }

    /// <summary>
    /// 需要上传的文件的本地路径，必须是绝对路径。上传前会先检查该路径下是否存在该文件，如果不存在则会抛出异常。
    /// </summary>
    [FilePath]
    [JsonPropertyName("file")]
    public string? FilePath { get; set; }
}
