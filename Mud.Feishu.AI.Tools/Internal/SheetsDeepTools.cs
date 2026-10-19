// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Sheets 深化执行器（6 只读 + 15 写）：条件格式（<c>cond_format_*</c>）/
/// 数据校验（<c>data_validation_*</c>）/ 保护范围（<c>protected_*</c>）/
/// 筛选器（<c>filter_*</c>）/ 浮动图片（<c>float_image_*</c>）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class SheetsDeepTools(
    Mud.Feishu.IFeishuTenantV2SpreadsheetConditionFormat condFormatClient,
    Mud.Feishu.IFeishuTenantV2SpreadsheetDataValidation dataValidationClient,
    Mud.Feishu.IFeishuTenantV2SpreadsheetProtected protectedClient,
    Mud.Feishu.IFeishuTenantV3SpreadsheetFilter filterClient,
    Mud.Feishu.IFeishuTenantV3SpreadsheetFloatImage floatImageClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2SpreadsheetConditionFormat _condFormatClient = condFormatClient
        ?? throw new ArgumentNullException(nameof(condFormatClient));
    private readonly Mud.Feishu.IFeishuTenantV2SpreadsheetDataValidation _dataValidationClient = dataValidationClient
        ?? throw new ArgumentNullException(nameof(dataValidationClient));
    private readonly Mud.Feishu.IFeishuTenantV2SpreadsheetProtected _protectedClient = protectedClient
        ?? throw new ArgumentNullException(nameof(protectedClient));
    private readonly Mud.Feishu.IFeishuTenantV3SpreadsheetFilter _filterClient = filterClient
        ?? throw new ArgumentNullException(nameof(filterClient));
    private readonly Mud.Feishu.IFeishuTenantV3SpreadsheetFloatImage _floatImageClient = floatImageClient
        ?? throw new ArgumentNullException(nameof(floatImageClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>sheets.cond_format_get：查询条件格式规则（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsCondFormatGetTool))]
    public Task<ToolResult> GetConditionFormatsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsCondFormatGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsCondFormatGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _condFormatClient
                .GetConditionFormatsAsync(args.SpreadsheetToken, args.SheetIds, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectConditionFormats);
        });
    }

    /// <summary>sheets.data_validation_get：查询数据校验规则（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsDataValidationGetTool))]
    public Task<ToolResult> GetDataValidationsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsDataValidationGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsDataValidationGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _dataValidationClient
                .GetDataValidationsAsync(args.SpreadsheetToken, args.Range, args.DataValidationType, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectDataValidations);
        });
    }

    /// <summary>sheets.protected_get：查询保护范围（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsProtectedGetTool))]
    public Task<ToolResult> GetProtectedAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsProtectedGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsProtectedGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _protectedClient
                .GetProtectedAsync(args.SpreadsheetToken, args.ProtectIds, memberType: args.MemberType ?? "open_id", cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectProtectedRanges);
        });
    }

    /// <summary>sheets.filter_get：查询工作表筛选器（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFilterGetTool))]
    public Task<ToolResult> GetFilterAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFilterGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFilterGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _filterClient
                .GetFilterAsync(args.SpreadsheetToken, args.SheetId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectSheetFilter);
        });
    }

    /// <summary>sheets.float_image_get：按 ID 获取单个浮动图片（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFloatImageGetTool))]
    public Task<ToolResult> GetFloatImageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFloatImageGet, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFloatImageGetArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _floatImageClient
                .GetFloatImageAsync(args.SpreadsheetToken, args.SheetId, args.FloatImageId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["float_image"] = data.FloatImage is null ? null : ProjectFloatImage(data.FloatImage),
            });
        });
    }

    /// <summary>sheets.float_image_query：查询工作表中的全部浮动图片（只读）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFloatImageQueryTool))]
    public Task<ToolResult> GetFloatImagesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFloatImageQuery, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFloatImageQueryArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _floatImageClient
                .GetFloatImagesAsync(args.SpreadsheetToken, args.SheetId, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data =>
            {
                var envelope = ToolResultJsons.ItemsEnvelope();
                foreach (var image in data.Items ?? [])
                {
                    envelope["items"]!.AsArray().AddNode(ProjectFloatImage(image));
                }

                return envelope;
            });
        });
    }

    /// <summary>sheets.cond_format_create：批量创建条件格式规则。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsCondFormatCreateTool))]
    public Task<ToolResult> CreateConditionFormatsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsCondFormatCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsCondFormatCreateArgs.Unpack(arguments);

            var formats = ParseRequiredArray<SheetConditionFormat>(args.ConditionFormats, "condition_formats");
            var request = new CreateConditionFormatRequest { SheetConditionFormats = formats };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_create",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("condition_formats", formats.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _condFormatClient
                .CreateConditionFormatsAsync(args.SpreadsheetToken, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectConditionFormatResponses);
        });
    }

    /// <summary>sheets.cond_format_update：批量更新（覆盖）条件格式规则（每项须带 cf_id）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsCondFormatUpdateTool))]
    public Task<ToolResult> UpdateConditionFormatsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsCondFormatUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsCondFormatUpdateArgs.Unpack(arguments);

            var formats = ParseRequiredArray<SheetConditionFormatData>(args.ConditionFormats, "condition_formats");
            foreach (var item in formats)
            {
                if (string.IsNullOrWhiteSpace(item.ConditionFormat?.CfId))
                {
                    throw new ArgumentException("condition_formats 每项的 condition_format.cf_id 必填（可先用 sheets.cond_format_get 取回）");
                }
            }

            var request = new UpdateConditionFormatRequest { SheetConditionFormats = formats };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_update",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("condition_formats", formats.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _condFormatClient
                .UpdateConditionFormatsAsync(args.SpreadsheetToken, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectConditionFormatResponses);
        });
    }

    /// <summary>sheets.cond_format_delete：批量删除条件格式规则（不可恢复）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsCondFormatDeleteTool))]
    public Task<ToolResult> DeleteConditionFormatsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsCondFormatDelete, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsCondFormatDeleteArgs.Unpack(arguments);

            var ids = ParseRequiredArray<SheetCfId>(args.SheetCfIds, "sheet_cf_ids");
            foreach (var item in ids)
            {
                if (string.IsNullOrWhiteSpace(item.SheetId) || string.IsNullOrWhiteSpace(item.CfId))
                {
                    throw new ArgumentException("sheet_cf_ids 每项须同时提供 sheet_id 与 cf_id（可先用 sheets.cond_format_get 取回）");
                }
            }

            var request = new DeleteConditionFormatsRequest { SheetCfIds = ids };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_delete",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_cf_ids", ids.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _condFormatClient
                .DeleteConditionFormatsAsync(args.SpreadsheetToken, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectConditionFormatResponses);
        });
    }

    /// <summary>sheets.data_validation_create：为单元格区域创建数据校验（下拉列表）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsDataValidationCreateTool))]
    public Task<ToolResult> CreateDataValidationAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsDataValidationCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsDataValidationCreateArgs.Unpack(arguments);

            if (args.ConditionValues is not { Length: > 0 })
            {
                throw new ArgumentException("condition_values 须为非空字符串数组（下拉选项值列表）");
            }

            var dataValidationType = args.DataValidationType ?? "list";
            var request = new CreateDataValidationRequest
            {
                Range = args.Range,
                DataValidationType = dataValidationType,
                DataValidation = new SheetDataValidation
                {
                    ConditionValues = args.ConditionValues,
                    Options = BuildDataValidationOption(args.MultipleValues, args.HighlightValidData),
                },
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("range", args.Range.Length),
                    ("data_validation_type", dataValidationType.Length), ("condition_values", args.ConditionValues.Length)));
            }

            // CreateDataValidationAsync 返回非泛型 FeishuApiResult?（无 Data 载荷）。
            var result = await _dataValidationClient
                .CreateDataValidationAsync(args.SpreadsheetToken, request, cancellationToken)
                .ConfigureAwait(false);
            RequireResultCode(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["range"] = args.Range,
                ["data_validation_type"] = dataValidationType,
                ["created"] = true,
            });
        });
    }

    /// <summary>sheets.data_validation_update：更新（覆盖）数据校验规则。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsDataValidationUpdateTool))]
    public Task<ToolResult> UpdateDataValidationAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsDataValidationUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsDataValidationUpdateArgs.Unpack(arguments);

            if (args.Ranges is not { Length: > 0 })
            {
                throw new ArgumentException("ranges 须为非空字符串数组（更新的生效区域列表）");
            }

            if (args.ConditionValues is not { Length: > 0 })
            {
                throw new ArgumentException("condition_values 须为非空字符串数组（下拉选项值列表）");
            }

            var dataValidationType = args.DataValidationType ?? "list";
            var request = new UpdateDataValidationRequest
            {
                Ranges = args.Ranges,
                DataValidationType = dataValidationType,
                DataValidation = new SheetDataValidation
                {
                    ConditionValues = args.ConditionValues,
                    Options = BuildDataValidationOption(args.MultipleValues, args.HighlightValidData),
                },
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation/{sheet_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length),
                    ("ranges", args.Ranges.Length), ("condition_values", args.ConditionValues.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _dataValidationClient
                .UpdateDataValidationAsync(args.SpreadsheetToken, args.SheetId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["sheet_id"] = data.SheetId,

                // UpdateDataValidationResult 不带生效区域（官方回包只含校验信息），ranges 取入参回显。
                ["ranges"] = ToJsonArray(args.Ranges),
                ["condition_values"] = ToJsonArray(data.DataValidation?.ConditionValues),
            });
        });
    }

    /// <summary>sheets.data_validation_delete：删除单元格区域上的数据校验规则（不可恢复）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsDataValidationDeleteTool))]
    public Task<ToolResult> DeleteDataValidationAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsDataValidationDelete, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsDataValidationDeleteArgs.Unpack(arguments);

            if (args.Ranges is not { Length: > 0 })
            {
                throw new ArgumentException("ranges 须为非空字符串数组（要删除数据校验的区域列表）");
            }

            var request = new DeleteDataValidationRequest
            {
                DataValidationRanges = [.. args.Ranges.Select(range => new DeleteDataValidationRange { Range = range })],
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("ranges", args.Ranges.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _dataValidationClient
                .DeleteDataValidationAsync(args.SpreadsheetToken, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectDataValidationDeleteResults);
        });
    }

    /// <summary>sheets.protected_create：为工作表/行列区间创建保护范围。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsProtectedCreateTool))]
    public Task<ToolResult> CreateProtectedAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsProtectedCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsProtectedCreateArgs.Unpack(arguments);

            if (args.MajorDimension != "ROWS" && args.MajorDimension != "COLUMNS")
            {
                throw new ArgumentException("major_dimension 必须为 'ROWS'（按行）或 'COLUMNS'（按列）");
            }

            if (args.StartIndex < 0 || args.EndIndex <= args.StartIndex)
            {
                throw new ArgumentException("end_index 须大于 start_index，且 start_index ≥ 0（半开区间，下标从 0 开始）");
            }

            var request = new CreateProtectedRequest
            {
                AddProtectedDimension =
                [
                    new ProtectedDimension
                    {
                        Dimension = new SheetRangeDimension
                        {
                            SheetId = args.SheetId,
                            MajorDimension = args.MajorDimension,
                            StartIndex = args.StartIndex,
                            EndIndex = args.EndIndex,
                        },
                        Users = args.Users,
                        LockInfo = args.LockInfo,
                    },
                ],
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_dimension",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length),
                    ("major_dimension", args.MajorDimension.Length), ("start_index", args.StartIndex), ("end_index", args.EndIndex)));
            }

            var outcome = FeishuApiResultReader.Read(await _protectedClient
                .CreateProtectedAsync(args.SpreadsheetToken, request, user_id_type: args.UserIdType ?? "open_id", cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data =>
            {
                var ids = new JsonArray();
                foreach (var item in data.AddProtectedDimension ?? [])
                {
                    ((IList<JsonNode?>)ids).Add(item.ProtectId);
                }

                return new JsonObject
                {
                    ["protect_ids"] = ids,
                };
            });
        });
    }

    /// <summary>sheets.protected_update：更新保护范围的可编辑成员或锁定信息。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsProtectedUpdateTool))]
    public Task<ToolResult> UpdateProtectedAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsProtectedUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsProtectedUpdateArgs.Unpack(arguments);

            if (args.AddEditors is null && args.RemoveEditors is null && args.LockInfo is null)
            {
                throw new ArgumentException("至少提供一个待更新项（add_editors / remove_editors / lock_info）");
            }

            var request = new UpdateProtectedRequest
            {
                Requests =
                [
                    new SheetProtectedData
                    {
                        ProtectId = args.ProtectId,
                        Editors = BuildEditors(args.AddEditors, args.RemoveEditors, args.EditorMemberType),
                        LockInfo = args.LockInfo,
                    },
                ],
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_update",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("protect_id", args.ProtectId.Length),
                    ("add_editors", args.AddEditors?.Length ?? 0), ("remove_editors", args.RemoveEditors?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _protectedClient
                .UpdateProtectedAsync(args.SpreadsheetToken, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectProtectedReplies);
        });
    }

    /// <summary>sheets.protected_delete：删除保护范围（不可恢复）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsProtectedDeleteTool))]
    public Task<ToolResult> DeleteProtectedAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsProtectedDelete, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsProtectedDeleteArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_del",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("protect_ids", args.ProtectIds.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _protectedClient
                .DeleteProtectedAsync(args.SpreadsheetToken, args.ProtectIds, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["del_protect_ids"] = data.ProtectIds,
                ["deleted"] = true,
            });
        });
    }

    /// <summary>sheets.filter_create：为工作表创建筛选器。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFilterCreateTool))]
    public Task<ToolResult> CreateFilterAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFilterCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFilterCreateArgs.Unpack(arguments);

            var request = new CreateFilterRequest
            {
                Range = args.Range,
                Col = args.Col,
                Condition = BuildFilterCondition(args.FilterType, args.CompareType, args.Expected),
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length),
                    ("range", args.Range.Length), ("col", args.Col.Length), ("filter_type", args.FilterType.Length)));
            }

            // CreateFilterAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _filterClient
                .CreateFilterAsync(args.SpreadsheetToken, args.SheetId, request, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["sheet_id"] = args.SheetId,
                ["col"] = args.Col,
                ["created"] = true,
            });
        });
    }

    /// <summary>sheets.filter_update：更新（覆盖）筛选器的某列筛选条件（filter_type 传 clear 清除）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFilterUpdateTool))]
    public Task<ToolResult> UpdateFilterAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFilterUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFilterUpdateArgs.Unpack(arguments);

            var request = new UpdateFilterRequest
            {
                Col = args.Col,
                Condition = BuildFilterCondition(args.FilterType, args.CompareType, args.Expected),
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length),
                    ("col", args.Col.Length), ("filter_type", args.FilterType.Length)));
            }

            // UpdateFilterAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _filterClient
                .UpdateFilterAsync(args.SpreadsheetToken, args.SheetId, request, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["sheet_id"] = args.SheetId,
                ["col"] = args.Col,
                ["updated"] = true,
            });
        });
    }

    /// <summary>sheets.filter_delete：删除工作表的筛选器。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFilterDeleteTool))]
    public Task<ToolResult> DeleteFilterAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFilterDelete, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFilterDeleteArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length)));
            }

            // DeleteFilterAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _filterClient
                .DeleteFilterAsync(args.SpreadsheetToken, args.SheetId, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["sheet_id"] = args.SheetId,
                ["deleted"] = true,
            });
        });
    }

    /// <summary>sheets.float_image_create：在工作表中创建浮动图片。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFloatImageCreateTool))]
    public Task<ToolResult> CreateFloatImageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFloatImageCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFloatImageCreateArgs.Unpack(arguments);

            var request = new CreateFloatImageRequest
            {
                FloatImageToken = args.FloatImageToken,
                Range = args.Range,
                FloatImageId = args.FloatImageId,
                Width = ParsePixelValue(args.Width, "width"),
                Height = ParsePixelValue(args.Height, "height"),
                OffsetX = ParsePixelValue(args.OffsetX, "offset_x"),
                OffsetY = ParsePixelValue(args.OffsetY, "offset_y"),
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length),
                    ("float_image_token", args.FloatImageToken.Length), ("range", args.Range.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _floatImageClient
                .CreateFloatImageAsync(args.SpreadsheetToken, args.SheetId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["float_image"] = data.FloatImage is null ? null : ProjectFloatImage(data.FloatImage),
            });
        });
    }

    /// <summary>sheets.float_image_update：更新浮动图片（未提供的字段保持不变）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFloatImageUpdateTool))]
    public Task<ToolResult> UpdateFloatImageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFloatImageUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFloatImageUpdateArgs.Unpack(arguments);

            if (args.Range is null && args.Width is null && args.Height is null
                && args.OffsetX is null && args.OffsetY is null)
            {
                throw new ArgumentException("至少提供一个待更新字段（range / width / height / offset_x / offset_y）");
            }

            var request = new UpdateFloatImageRequest
            {
                Range = args.Range,
                Width = ParsePixelValue(args.Width, "width"),
                Height = ParsePixelValue(args.Height, "height"),
                OffsetX = ParsePixelValue(args.OffsetX, "offset_x"),
                OffsetY = ParsePixelValue(args.OffsetY, "offset_y"),
            };

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PATCH", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length),
                    ("float_image_id", args.FloatImageId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _floatImageClient
                .UpdateFloatImageAsync(args.SpreadsheetToken, args.SheetId, args.FloatImageId, request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["float_image"] = data.FloatImage is null ? null : ProjectFloatImage(data.FloatImage),
            });
        });
    }

    /// <summary>sheets.float_image_delete：删除工作表中的浮动图片（不可恢复）。</summary>
    [ToolHandler(typeof(IFeishuTenantSheetsFloatImageDeleteTool))]
    public Task<ToolResult> DeleteFloatImageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SheetsFloatImageDelete, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SheetsFloatImageDeleteArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("spreadsheet_token", args.SpreadsheetToken.Length), ("sheet_id", args.SheetId.Length),
                    ("float_image_id", args.FloatImageId.Length)));
            }

            // DeleteFloatImageAsync 返回 FeishuNullDataApiResult?（无 Data 载荷）。
            var result = await _floatImageClient
                .DeleteFloatImageAsync(args.SpreadsheetToken, args.SheetId, args.FloatImageId, cancellationToken)
                .ConfigureAwait(false);
            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["sheet_id"] = args.SheetId,
                ["float_image_id"] = args.FloatImageId,
                ["deleted"] = true,
            });
        });
    }

    /// <summary>cond_format_get 投影：items（sheet_id/cf_id/rule_type/ranges）。</summary>
    private static JsonObject ProjectConditionFormats(GetConditionFormatsResult data)
    {
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var item in data.SheetConditionFormats ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["sheet_id"] = item.SheetId,
                ["cf_id"] = item.ConditionFormat?.CfId,
                ["rule_type"] = item.ConditionFormat?.RuleType,
                ["ranges"] = ToJsonArray(item.ConditionFormat?.Ranges),
            });
        }

        return envelope;
    }

    /// <summary>cond_format_create / update / delete 投影：items（sheet_id/cf_id/res_code/res_msg）。</summary>
    private static JsonObject ProjectConditionFormatResponses(ConditionFormatOpsResult data)
    {
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var item in data.Responses ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["sheet_id"] = item.SheetId,
                ["cf_id"] = item.CfId,
                ["res_code"] = item.ResCode,
                ["res_msg"] = item.ResMsg,
            });
        }

        return envelope;
    }

    /// <summary>data_validation_get 投影：sheet_id + items（ranges/data_validation_type/condition_values）。</summary>
    private static JsonObject ProjectDataValidations(GetDataValidationsResult data)
    {
        var envelope = ToolResultJsons.ItemsEnvelope();
        envelope["sheet_id"] = data.SheetId;
        foreach (var item in data.DataValidations ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["ranges"] = ToJsonArray(item.Ranges),
                ["data_validation_type"] = item.DataValidationType,
                ["condition_values"] = ToJsonArray(item.ConditionValues),
            });
        }

        return envelope;
    }

    /// <summary>data_validation_delete 投影：items（range/success/updated_cells）。</summary>
    private static JsonObject ProjectDataValidationDeleteResults(DeleteDataValidationResult data)
    {
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var item in data.RangeResults ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["range"] = item.Range,
                ["success"] = item.Success,
                ["updated_cells"] = item.UpdatedCells,
            });
        }

        return envelope;
    }

    /// <summary>protected_get 投影：items（sheet_id/protect_id/dimension/lock_info）。</summary>
    private static JsonObject ProjectProtectedRanges(GetProtectedResult data)
    {
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var item in data.ProtectedRanges ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["sheet_id"] = item.SheetId,
                ["protect_id"] = item.ProtectId,
                ["dimension"] = ProjectDimension(item.Dimension),
                ["lock_info"] = item.LockInfo,
            });
        }

        return envelope;
    }

    /// <summary>protected_update 投影：items（sheet_id/protect_id/lock_info）。</summary>
    private static JsonObject ProjectProtectedReplies(UpdateProtectedResult data)
    {
        var envelope = ToolResultJsons.ItemsEnvelope();
        foreach (var item in data.Replies ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["sheet_id"] = item.SheetId,
                ["protect_id"] = item.ProtectId,
                ["lock_info"] = item.LockInfo,
            });
        }

        return envelope;
    }

    /// <summary>filter_get 投影：range/filtered_out_rows/filter_infos。</summary>
    private static JsonObject ProjectSheetFilter(GetFilterResult data)
    {
        var info = data.SheetFilterInfo;
        return new JsonObject
        {
            ["range"] = info?.Range,
            ["filtered_out_rows"] = ToJsonArray(info?.FilteredOutRows),
            ["filter_infos"] = ProjectFilterInfos(info?.FilterInfos),
        };
    }

    /// <summary>filter_get 投影：筛选条件列表（col + conditions）。</summary>
    private static JsonArray ProjectFilterInfos(SheetFilterInfo[]? infos)
    {
        var array = new JsonArray();
        foreach (var info in infos ?? [])
        {
            var conditions = new JsonArray();
            foreach (var condition in info.Conditions ?? [])
            {
                conditions.AddNode(new JsonObject
                {
                    ["filter_type"] = condition.FilterType,
                    ["compare_type"] = condition.CompareType,
                    ["expected"] = ToJsonArray(condition.Expected),
                });
            }

            array.AddNode(new JsonObject
            {
                ["col"] = info.Col,
                ["conditions"] = conditions,
            });
        }

        return array;
    }

    /// <summary>保护范围维度投影（sheet_id/major_dimension/start_index/end_index）。</summary>
    private static JsonNode? ProjectDimension(SheetRangeDimension? dimension)
        => dimension is null
            ? null
            : new JsonObject
            {
                ["sheet_id"] = dimension.SheetId,
                ["major_dimension"] = dimension.MajorDimension,
                ["start_index"] = dimension.StartIndex,
                ["end_index"] = dimension.EndIndex,
            };

    /// <summary>浮动图片投影（float_image_id/float_image_token/range/width/height/offset_x/offset_y）。</summary>
    private static JsonObject ProjectFloatImage(SheetFloatImageData image)
        => new()
        {
            ["float_image_id"] = image.FloatImageId,
            ["float_image_token"] = image.FloatImageToken,
            ["range"] = image.Range,
            ["width"] = image.Width,
            ["height"] = image.Height,
            ["offset_x"] = image.OffsetX,
            ["offset_y"] = image.OffsetY,
        };

    /// <summary>data_validation_create / update 的选项构造；两项均未提供时省略 options。</summary>
    private static SheetDataValidationOption? BuildDataValidationOption(bool? multipleValues, bool? highlightValidData)
        => multipleValues is null && highlightValidData is null
            ? null
            : new SheetDataValidationOption
            {
                MultipleValues = multipleValues,
                HighlightValidData = highlightValidData,
            };

    /// <summary>protected_update 的可编辑成员构造；member_type 官方枚举为 openid/userid/unionid。</summary>
    private static SheetEditors? BuildEditors(string[]? addEditors, string[]? removeEditors, string? memberType)
    {
        if (addEditors is null && removeEditors is null)
        {
            return null;
        }

        var type = memberType ?? "openid";
        return new SheetEditors
        {
            AddEditors = addEditors?.Select(id => new SheetEditor { MemberType = type, MemberId = id }).ToArray(),
            DelEditors = removeEditors?.Select(id => new SheetEditor { MemberType = type, MemberId = id }).ToArray(),
        };
    }

    /// <summary>filter_create / update 的条件构造（expected 缺省为空数组）。</summary>
    private static SheetFilterCondition BuildFilterCondition(string filterType, string? compareType, string[]? expected)
        => new()
        {
            FilterType = filterType,
            CompareType = compareType,
            Expected = expected ?? [],
        };

    /// <summary>像素数值字符串 → float（空白 ⇒ null；非法数值 ⇒ 带参数名的结构化错误）。</summary>
    private static float? ParsePixelValue(string? raw, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
        {
            throw new ArgumentException($"{parameterName} 须为数值字符串（像素，如 \"320\"），实际为 '{raw}'");
        }

        return value;
    }

    /// <summary>必填 JSON 数组参数：空串/非数组/空数组均转为结构化错误。</summary>
    private static T[] ParseRequiredArray<T>(string json, string parameterName) where T : class
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException($"{parameterName} 不能为空（须为 JSON 数组字符串）");
        }

        T[]? items;
        try
        {
            // ⚠️ 必须走 FeishuJsonAot（AOT-3）：泛型 JsonSerializer.Deserialize<T> 重载带
            // RequiresUnreferencedCode/RequiresDynamicCode 标注，会破坏 AOT 严格模式 0 诊断门禁。
            // DTO 全部字段带 [JsonPropertyName]，故与解析器链的命名策略无关。
            items = Mud.Feishu.Abstractions.Utilities.FeishuJsonAot
                .Deserialize<T[]>(json, FeishuJsonDefaults.DeserializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"{parameterName} 解析失败：{ex.Message}");
        }

        if (items is null)
        {
            throw new ArgumentException($"{parameterName} 须为 JSON 数组");
        }

        if (items.Length == 0)
        {
            throw new ArgumentException($"{parameterName} 须为非空 JSON 数组");
        }

        return items;
    }

    /// <summary>string 数组 → JsonArray（缺省空数组）。</summary>
    private static JsonArray ToJsonArray(string[]? values)
    {
        var array = new JsonArray();
        foreach (var value in values ?? [])
        {
            ((IList<JsonNode?>)array).Add(value);
        }

        return array;
    }

    /// <summary>int 数组 → JsonArray（缺省空数组）。</summary>
    private static JsonArray ToJsonArray(int[]? values)
    {
        var array = new JsonArray();
        foreach (var value in values ?? [])
        {
            ((IList<JsonNode?>)array).Add(value);
        }

        return array;
    }

    /// <summary>
    /// 解包 <c>FeishuNullDataApiResult</c>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>，
    /// T 不可推断且 Data 天然为 null）。
    /// </summary>
    private static void RequireNullDataSuccess(string toolName, FeishuNullDataApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            throw new ArgumentException(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }
    }

    /// <summary>非泛型 <c>FeishuApiResult</c> 版本（data_validation_create 的返回形态）。</summary>
    private static void RequireResultCode(string toolName, FeishuApiResult? result)
    {
        if (result is null)
        {
            throw new ArgumentException("飞书接口无响应（result 为空）");
        }

        if (result.Code != 0)
        {
            throw new ArgumentException(
                FeishuToolBinding.StructuredError(
                    toolName,
                    result.Code,
                    $"飞书接口返回错误 code={result.Code.ToString(CultureInfo.InvariantCulture)}, "
                    + $"msg={result.Msg ?? "(无错误信息)"}"));
        }
    }
}
