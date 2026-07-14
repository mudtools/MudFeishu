// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.DataModels.AI;

/// <summary>
/// 提取文件中的合同字段请求体
/// </summary>
[FormContent]
[HttpJsonSerializable(SerializerClassName = "AI")]
public partial class ContractFileUploadRequest
{
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public ContractFileUploadRequest()
    {
    }

    /// <summary>
    /// 带文件全路径名称参数的构造函数
    /// </summary>
    /// <param name="fileName">文件全路径名称</param>
    public ContractFileUploadRequest(string? fileName)
    {
        FileName = fileName;
    }

    /// <summary>
    /// 文件全路径名称。
    /// </summary>
    [FilePath]
    [JsonPropertyName("file")]
    public string? FileName { get; set; }

    /// <summary>
    /// <para>pdf页数限制，太长会导致latency增加，最大允许100页</para>
    /// <para>必填：是</para>
    /// <para>示例值：15</para>
    /// </summary>
    [JsonPropertyName("pdf_page_limit")]
    public int PdfPageLimit { get; set; }

    /// <summary>
    /// <para>ocr 参数，当前支持force, pdf, unused三种格式</para>
    /// <para>必填：是</para>
    /// <para>示例值：auto</para>
    /// <para>可选值：<list type="bullet">
    /// <item>force：pdf类型文件直接走OCR解析</item>
    /// <item>auto：pdf类型文件先走本地解析，无法解析（扫描/图片版）再走OCR</item>
    /// <item>unused：不调用OCR，扫描/图片PDF返回不可解析信息</item>
    /// </list></para>
    /// </summary>
    [JsonPropertyName("ocr_mode")]
    public string OcrMode { get; set; } = string.Empty;
}
