// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.Abstractions.Utilities;
using Mud.Feishu.DataModels.Approval;
using Mud.Feishu.DataModels.ApprovalQuery;
using Mud.Feishu.DataModels.ApprovalTask;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels.Docx;
using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.DataModels.Drive.Folder;
using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Bitable 写工具执行器（<c>bitable.add_record</c> / <c>bitable.record_batch_create</c>）：模型 JSON 字符串 →
/// <see cref="RecordOpsRequest"/>（<c>Fields</c> 走 <see cref="JsonElement"/>——STJ 内建转换器
/// 可源生成序列化，无反射）→ <c>AddRecordAsync</c> → 解包投影。
/// </summary>
internal sealed class BitableWriteTools(
    Mud.Feishu.IFeishuTenantV1BitableRecord recordClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1BitableRecord _recordClient = recordClient
        ?? throw new ArgumentNullException(nameof(recordClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>bitable.add_record：新增记录（fields 为「字段名 → 值」JSON 对象字符串；<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 / F-1）：<c>idempotency_key</c> → 透传查询参数 <c>client_token</c>（重复请求返回原记录）。</remarks>
    [ToolHandler(typeof(IFeishuTenantBitableAddRecordTool))]
    public Task<ToolResult> AddRecordAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableAddRecord, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableAddRecordArgs.Unpack(arguments);

            // 参数合法性校验先于 dry_run 判定：预演的价值在于"能提前发现的问题都提前发现"，
            // 若预演放过了非法 fields，模型会误以为参数没问题而在真实下发时才失败。
            var fields = ParseFieldsObject(args.Fields);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("fields", args.Fields.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .AddRecordAsync(args.AppToken, args.TableId, new RecordOpsRequest { Fields = fields }, client_token: args.IdempotencyKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["record_id"] = data.Record?.RecordId,
            });
        });
    }

    /// <summary>bitable.record_batch_create：批量新增记录（单次上限 1000 条；<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 / F-1）：<c>idempotency_key</c> → 透传查询参数 <c>client_token</c>。</remarks>
    [ToolHandler(typeof(IFeishuTenantBitableRecordBatchCreateTool))]
    public Task<ToolResult> AddRecordsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRecordBatchCreate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRecordBatchCreateArgs.Unpack(arguments);

            var records = ParseRecordArray<AppTableRecordData>(args.Records, "records");

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/batch_create",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("records", records.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .AddRecordsAsync(args.AppToken, args.TableId, new AddRecordsRequest { Records = records }, client_token: args.IdempotencyKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectBatchRecords);
        });
    }

    /// <summary>fields 参数 → JSON 对象 <see cref="JsonElement"/>（非对象/非法 JSON 转结构化错误）。</summary>
    /// <remarks>
    /// 此处 <c>catch (JsonException)</c> 是<b>校验逻辑</b>而非执行骨架（把平台无语义的解析失败
    /// 翻译成带修复指引的 <see cref="ArgumentException"/>），不在 WP3 骨架收敛范围。
    /// </remarks>
    internal static JsonElement ParseFieldsObject(string fieldsJson)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(fieldsJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"fields 不是合法 JSON: {ex.Message}");
        }

        if (node is not JsonObject)
        {
            throw new ArgumentException("fields 须为 JSON 对象（{\"字段名\": 值}）");
        }

        // WP4（W5 修复）：使用 using 归还池化缓冲（CA2000），Clone 后 JsonDocument 可安全释放。
        using var document = JsonDocument.Parse(fieldsJson);
        return document.RootElement.Clone();
    }

    /// <summary>批量记录单次上限（官方 batch_create / batch_update 均为 1000 条）。</summary>
    internal const int MaxRecordsPerBatch = 1000;

    /// <summary>records 参数 → 强类型记录数组（非法 JSON / 空数组 / 超量转带修复指引的结构化错误）。</summary>
    /// <remarks>
    /// 解析走 <c>FeishuJsonAot</c>（AOT-3）：泛型 <c>JsonSerializer.Deserialize&lt;T&gt;(string, options)</c>
    /// 重载带 RequiresUnreferencedCode/RequiresDynamicCode 注解，在 net8+ 产生 IL2026/IL3050。
    /// DTO 全部字段带 <c>[JsonPropertyName]</c>，与解析器链的命名策略无关。
    /// </remarks>
    internal static T[] ParseRecordArray<T>(string recordsJson, string parameterName) where T : class
    {
        T[]? items;
        try
        {
            items = FeishuJsonAot.Deserialize<T[]>(recordsJson, FeishuJsonDefaults.DeserializerOptions);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException($"{parameterName} 不是合法 JSON: {ex.Message}");
        }

        if (items is not { Length: > 0 })
        {
            throw new ArgumentException(
                $"{parameterName} 须为非空 JSON 数组（每项形如 {{\"fields\":{{\"字段名\":\"值\"}}}}），最多 {MaxRecordsPerBatch} 条");
        }

        if (items.Length > MaxRecordsPerBatch)
        {
            throw new ArgumentException(
                $"{parameterName} 单次最多 {MaxRecordsPerBatch} 条，实际 {items.Length} 条；请分批调用");
        }

        return items;
    }

    /// <summary>record_batch_* 投影：record_ids（成功记录 ID 列表）+ count。</summary>
    internal static JsonObject ProjectBatchRecords(RecordsOpsResult data)
    {
        var ids = new JsonArray();
        foreach (var record in data.Records ?? [])
        {
            if (!string.IsNullOrEmpty(record.RecordId))
            {
                // AddNode（而非泛型 Add<T>）：泛型 Add<T>(T) 带 RequiresUnreferencedCode/RequiresDynamicCode
                // 注解（非 JsonNode 的 T 走反射 JsonValue.Create），IL2026/IL3050 必须为 0。
                ((IList<JsonNode?>)ids).Add(record.RecordId);
            }
        }

        return new JsonObject
        {
            ["record_ids"] = ids,
            ["count"] = ids.Count,
        };
    }
}

/// <summary>
/// Bitable 写工具执行器（<c>bitable.update_record</c> / <c>bitable.delete_record</c> / <c>bitable.record_batch_update</c>，WP2/R5）。
/// 与 <see cref="BitableWriteTools"/>（<c>add_record</c>/<c>record_batch_create</c>）职责不同，故分列两个类型
/// （R3-20：原名 <c>BitableWriteTools2</c> 无语义，改按职责命名）。
/// </summary>
internal sealed class BitableWriteRecordOps(
    Mud.Feishu.IFeishuTenantV1BitableRecord recordClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV1BitableRecord _recordClient = recordClient
        ?? throw new ArgumentNullException(nameof(recordClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>bitable.update_record：更新记录（fields 为「字段名 → 值」JSON 对象字符串）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableUpdateRecordTool))]
    public Task<ToolResult> UpdateRecordAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableUpdateRecord, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableUpdateRecordArgs.Unpack(arguments);

            var fields = BitableWriteTools.ParseFieldsObject(args.Fields);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", $"/open-apis/bitable/v1/apps/{args.AppToken}/tables/{args.TableId}/records/{args.RecordId}",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("record_id", args.RecordId.Length), ("fields", args.Fields.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .UpdateRecordAsync(args.AppToken, args.TableId, args.RecordId, new RecordOpsRequest { Fields = fields }, client_token: args.IdempotencyKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["record_id"] = data.Record?.RecordId,
            });
        });
    }

    /// <summary>bitable.record_batch_update：批量更新记录（单次上限 1000 条）。</summary>
    /// <remarks>官方批量更新接口无幂等键参数（<c>client_token</c> 仅 batch_create 支持），故本工具不提供 <c>idempotency_key</c>。</remarks>
    [ToolHandler(typeof(IFeishuTenantBitableRecordBatchUpdateTool))]
    public Task<ToolResult> UpdateRecordsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableRecordBatchUpdate, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableRecordBatchUpdateArgs.Unpack(arguments);

            var records = BitableWriteTools.ParseRecordArray<AppTableRecordDetailData>(args.Records, "records");
            foreach (var record in records)
            {
                if (string.IsNullOrWhiteSpace(record.RecordId))
                {
                    // 逐项校验先于 dry_run：缺 record_id 的项无法定位目标记录，预演也不该放过。
                    throw new ArgumentException("records 每项须携带非空 record_id（形如 recXxx）");
                }
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/batch_update",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("records", records.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .UpdateRecordsAsync(args.AppToken, args.TableId, new UpdateRecordsRequest { Records = records }, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, BitableWriteTools.ProjectBatchRecords);
        });
    }

    /// <summary>bitable.delete_record：删除记录（不可恢复，high-risk-write）。</summary>
    [ToolHandler(typeof(IFeishuTenantBitableDeleteRecordTool))]
    public Task<ToolResult> DeleteRecordAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableDeleteRecord, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = BitableDeleteRecordArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", $"/open-apis/bitable/v1/apps/{args.AppToken}/tables/{args.TableId}/records/{args.RecordId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("record_id", args.RecordId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .DeleteRecordAsync(args.AppToken, args.TableId, args.RecordId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, data => new JsonObject
            {
                ["deleted"] = data.Deleted,
                ["record_id"] = data.RecordId,
            });
        });
    }
}
