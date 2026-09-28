// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;
using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Bitable 三工具执行器（<c>bitable.list_tables</c> / <c>bitable.list_fields</c> / <c>bitable.query_records</c>）：
/// 参数映射 → 强类型 Tenant 接口 → 解包 → 白名单投影 → 截断。
/// </summary>
internal sealed class BitableTools(
    Mud.Feishu.IFeishuTenantV1BitableAppTable appTableClient,
    Mud.Feishu.IFeishuTenantV1BitableField fieldClient,
    Mud.Feishu.IFeishuTenantV1BitableRecord recordClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1BitableAppTable _appTableClient = appTableClient
        ?? throw new ArgumentNullException(nameof(appTableClient));
    private readonly Mud.Feishu.IFeishuTenantV1BitableField _fieldClient = fieldClient
        ?? throw new ArgumentNullException(nameof(fieldClient));
    private readonly Mud.Feishu.IFeishuTenantV1BitableRecord _recordClient = recordClient
        ?? throw new ArgumentNullException(nameof(recordClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>bitable.list_tables：列出数据表（白名单 table_id/name/revision）。</summary>
    public async Task<FeishuToolResult> ListTablesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var appToken = ToolArgs.RequireString(arguments, "app_token");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            var outcome = FeishuApiResultReader.Read(await _appTableClient
                .GetAppTablePageListAsync(appToken, PageSizes.BitableTables, pageToken, cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableListTables, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
            var envelope = PageEnvelope(data.HasMore, data.PageToken);
            foreach (var table in data.Items ?? [])
            {
                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["table_id"] = table.TableId,
                    ["name"] = table.Name,
                    ["revision"] = table.Revision,
                });
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableListTables, ex.Message));
        }
    }

    /// <summary>bitable.list_fields：列出字段定义（白名单 field_id/name/type/is_primary/ui_type）。</summary>
    /// <remarks>源码出参 <c>property</c> 为复杂嵌套对象，AOT 安全投影不含反射序列化，故不回填（Phase 2 评估源生成上下文引用）。</remarks>
    public async Task<FeishuToolResult> ListFieldsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var appToken = ToolArgs.RequireString(arguments, "app_token");
            var tableId = ToolArgs.RequireString(arguments, "table_id");
            var viewId = ToolArgs.OptionalString(arguments, "view_id");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            var outcome = FeishuApiResultReader.Read(await _fieldClient
                .GetFieldsPageListAsync(appToken, tableId, viewId, text_field_as_array: null, PageSizes.BitableFields, pageToken, cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableListFields, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
            var envelope = PageEnvelope(data.HasMore, data.PageToken);
            foreach (var field in data.Items ?? [])
            {
                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["field_id"] = field.FieldId,
                    ["name"] = field.FieldName,
                    ["type"] = field.Type,
                    ["is_primary"] = field.IsPrimary,
                    ["ui_type"] = field.UiType,
                });
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableListFields, ex.Message));
        }
    }

    /// <summary>bitable.query_records：查询记录（filter/sort 简化文法 → 官方过滤/排序结构）。</summary>
    public async Task<FeishuToolResult> QueryRecordsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var appToken = ToolArgs.RequireString(arguments, "app_token");
            var tableId = ToolArgs.RequireString(arguments, "table_id");
            var viewId = ToolArgs.OptionalString(arguments, "view_id");
            var fieldNames = ToolArgs.OptionalStringArray(arguments, "field_names");
            var filter = ToolArgs.OptionalString(arguments, "filter");
            var sort = ToolArgs.OptionalStringArray(arguments, "sort");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            // filter 简化文法（§3.3.3）：解析失败回填「filter 语法不支持」结构化错误。
            if (!BitableFilterParser.TryParse(filter, out var parsedFilter, out var filterError))
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableQueryRecords, filterError!));
            }

            // sort 简化文法（P1D-1b 批次 A）：字段:asc|desc，≤3 个。
            if (!BitableSortParser.TryParse(sort, out var parsedSort, out var sortError))
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableQueryRecords, sortError!));
            }

            var request = new QueryRecordsRequest
            {
                ViewId = viewId,
                FieldNames = fieldNames,
                Filter = parsedFilter,
                Sorts = parsedSort,
            };

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .QueryRecordsPageListAsync(appToken, tableId, request, PageSizes.BitableRecords, pageToken, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableQueryRecords, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
            var envelope = PageEnvelope(data.HasMore, data.PageToken);
            if (data.Total.HasValue)
            {
                envelope["total"] = data.Total.Value;
            }

            foreach (var record in data.Items ?? [])
            {
                var fields = new JsonObject();
                foreach (var pair in record.Fields ?? new Dictionary<string, object?>())
                {
                    // field_names 列过滤在绑定层落地（缺省返回全部字段）。
                    if (fieldNames is { Length: > 0 }
                        && !fieldNames.Contains(pair.Key, StringComparer.Ordinal))
                    {
                        continue;
                    }

                    fields[pair.Key] = ToolResultText.ToJsonNode(pair.Value);
                }

                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["record_id"] = record.RecordId,
                    ["fields"] = fields,
                });
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableQueryRecords, ex.Message));
        }
    }

    /// <summary>bitable.get_records_by_ids：按 ID 批量取记录（官方上限 100 条/请求；白名单 record_id/fields）。</summary>
    public async Task<FeishuToolResult> GetRecordsByIdsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var appToken = ToolArgs.RequireString(arguments, "app_token");
            var tableId = ToolArgs.RequireString(arguments, "table_id");
            var recordIds = ToolArgs.OptionalStringArray(arguments, "record_ids");

            if (recordIds is null || recordIds.Length == 0)
            {
                throw new ArgumentException("缺少必填参数 record_ids");
            }

            if (recordIds.Length > PageSizes.BitableRecordsByIds)
            {
                throw new ArgumentException(
                    $"record_ids 最多 {PageSizes.BitableRecordsByIds.ToString(CultureInfo.InvariantCulture)} 条，实际 {recordIds.Length.ToString(CultureInfo.InvariantCulture)} 条");
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .GetRecordsAsync(appToken, tableId, new GetRecordsRequest { RecordIds = recordIds }, cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableGetRecordsByIds, outcome.Code, outcome.ErrorText!));
            }

            var data = outcome.Data!;
            var envelope = new JsonObject { ["items"] = new JsonArray() };
            foreach (var record in data.Records ?? [])
            {
                var fields = new JsonObject();
                foreach (var pair in record.Fields ?? new Dictionary<string, object?>())
                {
                    fields[pair.Key] = ToolResultText.ToJsonNode(pair.Value);
                }

                envelope["items"]!.AsArray().AddNode(new JsonObject
                {
                    ["record_id"] = record.RecordId,
                    ["fields"] = fields,
                });
            }

            if (data.AbsentRecordIds is { Length: > 0 })
            {
                envelope["absent_record_ids"] = new JsonArray();
                foreach (var absent in data.AbsentRecordIds)
                {
                    ((IList<JsonNode?>)envelope["absent_record_ids"]!.AsArray()).Add(absent);
                }
            }

            return FeishuToolResult.FromText(ToolResultText.TruncateJson(envelope.ToJsonString(), _maxResultLength));
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableGetRecordsByIds, ex.Message));
        }
    }

    private static JsonObject PageEnvelope(bool hasMore, string? pageToken)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = hasMore,
        };
        if (!string.IsNullOrEmpty(pageToken))
        {
            envelope["page_token"] = pageToken;
        }

        return envelope;
    }
}
