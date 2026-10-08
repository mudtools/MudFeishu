// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.AI.Tools.Tools;
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
/// Bitable 写工具执行器（<c>bitable.add_record</c>）：模型 JSON 字符串 →
/// <see cref="RecordOpsRequest"/>（<c>Fields</c> 走 <see cref="JsonElement"/>——STJ 内建转换器
/// 可源生成序列化，无反射）→ <c>AddRecordAsync</c> → 解包投影。
/// </summary>
internal sealed class BitableWriteTools(Mud.Feishu.IFeishuTenantV1BitableRecord recordClient)
{
    private readonly Mud.Feishu.IFeishuTenantV1BitableRecord _recordClient = recordClient
        ?? throw new ArgumentNullException(nameof(recordClient));

    /// <summary>bitable.add_record：新增记录（fields 为「字段名 → 值」JSON 对象字符串；<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 / F-1）：<c>idempotency_key</c> → 透传查询参数 <c>client_token</c>（重复请求返回原记录）。</remarks>
    [FeishuToolHandler(typeof(IFeishuTenantBitableAddRecordTool))]
    public Task<FeishuToolResult> AddRecordAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableAddRecord);
        return executor.RunAsync(async () =>
        {
            var args = BitableAddRecordArgs.Unpack(arguments);

            // 参数合法性校验先于 dry_run 判定：预演的价值在于"能提前发现的问题都提前发现"，
            // 若预演放过了非法 fields，模型会误以为参数没问题而在真实下发时才失败。
            var fields = ParseFieldsObject(args.Fields);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST",
                    "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("fields", args.Fields.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .AddRecordAsync(args.AppToken, args.TableId, new RecordOpsRequest { Fields = fields }, client_token: args.IdempotencyKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["record_id"] = data.Record?.RecordId,
            });
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
}

/// <summary>
/// Bitable 写工具执行器（<c>bitable.update_record</c> / <c>bitable.delete_record</c>，WP2/R5）。
/// 与 <see cref="BitableWriteTools"/>（<c>add_record</c>）职责不同，故分列两个类型
/// （R3-20：原名 <c>BitableWriteTools2</c> 无语义，改按职责命名）。
/// </summary>
internal sealed class BitableWriteRecordOps(Mud.Feishu.IFeishuTenantV1BitableRecord recordClient)
{
    private readonly Mud.Feishu.IFeishuTenantV1BitableRecord _recordClient = recordClient
        ?? throw new ArgumentNullException(nameof(recordClient));

    /// <summary>bitable.update_record：更新记录（fields 为「字段名 → 值」JSON 对象字符串）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantBitableUpdateRecordTool))]
    public Task<FeishuToolResult> UpdateRecordAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableUpdateRecord);
        return executor.RunAsync(async () =>
        {
            var args = BitableUpdateRecordArgs.Unpack(arguments);

            var fields = BitableWriteTools.ParseFieldsObject(args.Fields);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "PUT", $"/open-apis/bitable/v1/apps/{args.AppToken}/tables/{args.TableId}/records/{args.RecordId}",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("record_id", args.RecordId.Length), ("fields", args.Fields.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .UpdateRecordAsync(args.AppToken, args.TableId, args.RecordId, new RecordOpsRequest { Fields = fields }, client_token: args.IdempotencyKey, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["record_id"] = data.Record?.RecordId,
            });
        });
    }

    /// <summary>bitable.delete_record：删除记录（不可恢复，high-risk-write）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantBitableDeleteRecordTool))]
    public Task<FeishuToolResult> DeleteRecordAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.BitableDeleteRecord);
        return executor.RunAsync(async () =>
        {
            var args = BitableDeleteRecordArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "DELETE", $"/open-apis/bitable/v1/apps/{args.AppToken}/tables/{args.TableId}/records/{args.RecordId}",
                    ToolDryRun.IdempotencyNote(null),
                    ("app_token", args.AppToken.Length), ("table_id", args.TableId.Length), ("record_id", args.RecordId.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .DeleteRecordAsync(args.AppToken, args.TableId, args.RecordId, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["deleted"] = data.Deleted,
                ["record_id"] = data.RecordId,
            });
        });
    }
}
