// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Sheets 双工具执行器（<c>sheets.list_sheets</c> / <c>sheets.get_range_values</c>）：
/// 工作表白名单 sheet_id/title/index；区域值网格经 <see cref="ToolResultText.ToJsonNode"/> 逐格投影后截断。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
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
    public Task<FeishuToolResult> ListSheetsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsListSheets, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsListSheetsArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _spreadsheetsClient
                .GetSpreadsheetSheetsByTokenAsync(args.SpreadsheetToken, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectSheets);
        });
    }

    /// <summary>sheets.get_range_values：读取单元格区域数据。</summary>
    public Task<FeishuToolResult> GetRangeValuesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsGetRangeValues, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsGetRangeValuesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _spreadsheetDataClient
                .GetRangeDataAsync(
                    args.SpreadsheetToken,
                    args.Range,
                    args.ValueRenderOption,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectRangeValues);
        });
    }

    /// <summary>list_sheets 投影：items（sheet_id/title/index）。</summary>
    private static JsonObject ProjectSheets(GetSpreadsheetSheetsResult data)
    {
        var envelope = new JsonObject { ["items"] = new JsonArray() };
        foreach (var sheet in data.Sheets ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["sheet_id"] = sheet.SheetId,
                ["title"] = sheet.Title,
                ["index"] = sheet.Index,
            });
        }

        return envelope;
    }

    /// <summary>get_range_values 投影：range + values 网格（逐格 ToJsonNode）。</summary>
    private static JsonObject ProjectRangeValues(GetRangeDataResult data)
    {
        var valueRange = data.ValueRange;
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

        return new JsonObject
        {
            ["range"] = valueRange?.Range,
            ["values"] = values,
        };
    }
}
