// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Docx 单工具执行器（<c>docx.get_raw_content</c>）：正文为纯文本，不做字段投影，
/// 仅按 <see cref="FeishuAgentOptions.MaxToolResultLength"/> 截断并标记（§3.3.3）。
/// </summary>
internal sealed class DocxTools(Mud.Feishu.IFeishuTenantV1Docx docxClient, IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1Docx _docxClient = docxClient
        ?? throw new ArgumentNullException(nameof(docxClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>docx.get_raw_content：读取文档纯文本正文。</summary>
    public async Task<string> GetRawContentAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var documentId = ToolArgs.RequireString(arguments, "document_id");
            var lang = ToolArgs.OptionalString(arguments, "lang");

            var outcome = FeishuApiResultReader.Read(await _docxClient
                .GetDocumentRawContentAsync(
                    documentId,
                    ParseLang(lang),
                    cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolBinding.StructuredError(FeishuToolNames.DocxGetRawContent, outcome.ErrorText!);
            }

            return ToolResultText.Truncate(outcome.Data!.Content ?? string.Empty, _maxResultLength);
        }
        catch (ArgumentException ex)
        {
            return FeishuToolBinding.StructuredError(FeishuToolNames.DocxGetRawContent, ex.Message);
        }
    }

    private static int? ParseLang(string? lang)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            return 0; // 默认中文（§3.3.3：lang 默认 0）。
        }

        if (!int.TryParse(lang, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            throw new ArgumentException($"lang 需为整数（0=中文），实际: {lang}");
        }

        return value;
    }
}
