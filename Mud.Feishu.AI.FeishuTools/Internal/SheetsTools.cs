// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Sheets 双工具执行器（<c>sheets.list_sheets</c> / <c>sheets.get_range_values</c>）：
/// 工作表白名单 sheet_id/title/index；区域值网格经 <see cref="ToolResultText.ToJsonNode"/> 逐格投影后截断。
/// </summary>
internal sealed class SheetsTools(
    Mud.Feishu.IFeishuTenantV3Spreadsheets spreadsheetsClient,
    Mud.Feishu.IFeishuTenantV3SpreadsheetData spreadsheetDataClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV3Spreadsheets _spreadsheetsClient = spreadsheetsClient
        ?? throw new ArgumentNullException(nameof(spreadsheetsClient));
    private readonly Mud.Feishu.IFeishuTenantV3SpreadsheetData _spreadsheetDataClient = spreadsheetDataClient
        ?? throw new ArgumentNullException(nameof(spreadsheetDataClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>sheets.list_sheets：列出工作表（白名单 sheet_id/title/index）。</summary>
    public async Task<string> ListSheetsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var spreadsheetToken = ToolArgs.RequireString(arguments, "spreadsheet_token");

            var outcome = FeishuApiResultReader.Read(await _spreadsheetsClient
                .GetSpreadsheetSheetsByTokenAsync(spreadsheetToken, cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolBinding.StructuredError(FeishuToolNames.SheetsListSheets, outcome.Code, outcome.ErrorText!);
            }

            var envelope = new JsonObject { ["items"] = new JsonArray() };
            foreach (var sheet in outcome.Data!.Sheets ?? [])
            {
                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["sheet_id"] = sheet.SheetId,
                    ["title"] = sheet.Title,
                    ["index"] = sheet.Index,
                });
            }

            return ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength);
        }
        catch (ArgumentException ex)
        {
            return FeishuToolBinding.StructuredError(FeishuToolNames.SheetsListSheets, ex.Message);
        }
    }

    /// <summary>sheets.get_range_values：读取单元格区域数据。</summary>
    public async Task<string> GetRangeValuesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var spreadsheetToken = ToolArgs.RequireString(arguments, "spreadsheet_token");
            var range = ToolArgs.RequireString(arguments, "range");
            var valueRenderOption = ToolArgs.OptionalString(arguments, "value_render_option");

            var outcome = FeishuApiResultReader.Read(await _spreadsheetDataClient
                .GetRangeDataAsync(
                    spreadsheetToken,
                    range,
                    valueRenderOption,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolBinding.StructuredError(FeishuToolNames.SheetsGetRangeValues, outcome.Code, outcome.ErrorText!);
            }

            var valueRange = outcome.Data!.ValueRange;
            var values = new JsonArray();
            foreach (var row in valueRange?.Values ?? [])
            {
                var rowArray = new JsonArray();
                foreach (var cell in row ?? [])
                {
                    rowArray.AddNode(ToolResultText.ToJsonNode(cell));
                }

                values.AddNode(rowArray);
            }

            var envelope = new JsonObject
            {
                ["range"] = valueRange?.Range,
                ["values"] = values,
            };
            return ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength);
        }
        catch (ArgumentException ex)
        {
            return FeishuToolBinding.StructuredError(FeishuToolNames.SheetsGetRangeValues, ex.Message);
        }
    }
}
