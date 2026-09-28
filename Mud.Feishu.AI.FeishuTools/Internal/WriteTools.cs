// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.DataModels.Approval;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.AI.FeishuTools.Channels;
using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// IM 写工具执行器（<c>im.send_message</c>）：模型扁平参数 → <see cref="SendMessageRequest"/>
/// （msg_type 固定 text、content 经 JsonNode 转义）→ <c>SendMessageAsync</c> → 解包投影。
/// </summary>
/// <remarks>执行骨架（catch/回填）由 <see cref="ToolExecutor"/> 承担（WP3）；写工具载荷小、无截断语义。</remarks>
internal sealed class MessageWriteTools(Mud.Feishu.IFeishuTenantV1Message messageClient)
{
    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));

    /// <summary>im.send_message：发送文本消息（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 / F-1）：<c>idempotency_key</c> → <see cref="SendMessageRequest.Uuid"/>（平台侧 1 小时窗口去重）。</remarks>
    [FeishuToolHandler(typeof(IFeishuImSendMessageTool))]
    public Task<FeishuToolResult> SendMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImSendMessage);
        return executor.RunAsync(async () =>
        {
            var args = ImSendMessageArgs.Unpack(arguments);
            var receiveIdType = args.ReceiveIdType ?? "chat_id";
            if (!EditMessageChannel.AllowedReceiveIdTypes.Contains(receiveIdType, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"receive_id_type 仅支持 {string.Join("/", EditMessageChannel.AllowedReceiveIdTypes)}，实际: {receiveIdType}");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/im/v1/messages",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("receive_id", args.ReceiveId.Length), ("text", args.Text.Length), ("receive_id_type", receiveIdType.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .SendMessageAsync(
                    new SendMessageRequest
                    {
                        ReceiveId = args.ReceiveId,
                        MsgType = "text",
                        Content = new JsonObject { ["text"] = args.Text }.ToJsonString(),
                        Uuid = args.IdempotencyKey,
                    },
                    receiveIdType,
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["message_id"] = data.MessageId,
            });
        });
    }
}

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
    [FeishuToolHandler(typeof(IFeishuBitableAddRecordTool))]
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
    private static JsonElement ParseFieldsObject(string fieldsJson)
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

        return JsonDocument.Parse(fieldsJson).RootElement.Clone();
    }
}

/// <summary>
/// Approval 写工具执行器（<c>approval.create_instance</c>）：模型扁平参数 →
/// <see cref="CreateInstanceRequest"/>（approval_code/user_id 为 body 字段）→
/// <c>CreateInstanceAsync</c> → 解包投影。
/// </summary>
internal sealed class ApprovalWriteTools(Mud.Feishu.IFeishuTenantV4Approval approvalClient)
{
    private readonly Mud.Feishu.IFeishuTenantV4Approval _approvalClient = approvalClient
        ?? throw new ArgumentNullException(nameof(approvalClient));

    /// <summary>approval.create_instance：发起审批实例（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 / F-1）：<c>idempotency_key</c> → <see cref="CreateInstanceRequest.Uuid"/>（冲突返回 60012）。</remarks>
    [FeishuToolHandler(typeof(IFeishuApprovalCreateInstanceTool))]
    public Task<FeishuToolResult> CreateInstanceAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApprovalCreateInstance);
        return executor.RunAsync(async () =>
        {
            var args = ApprovalCreateInstanceArgs.Unpack(arguments);

            // AT-F13③：form 是裸 JSON 字符串参数，此前**无任何形状校验**——非 JSON / 非数组
            // 会被原样下发，飞书侧返回一个语焉不详的 code，模型只能盲试。此处与 fields 同级校验。
            ValidateFormArray(args.Form);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/approval/v4/instances",
                    ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("approval_code", args.ApprovalCode.Length), ("form", args.Form.Length), ("user_id", args.UserId?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _approvalClient
                .CreateInstanceAsync(
                    new CreateInstanceRequest
                    {
                        ApprovalCode = args.ApprovalCode,
                        Form = args.Form,
                        UserId = args.UserId,
                        Uuid = args.IdempotencyKey,
                    },
                    cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApiUntruncated(outcome, data => new JsonObject
            {
                ["instance_code"] = data.InstanceCode,
            });
        });
    }

    /// <summary>form 参数 → JSON 数组校验（非数组/非法 JSON 转结构化错误，附修复指引）。</summary>
    /// <remarks>与 <see cref="BitableWriteTools.ParseFieldsObject"/> 同一体例：裸 JSON 参数不得静默下发（校验逻辑，非骨架）。</remarks>
    private static void ValidateFormArray(string formJson)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(formJson);
        }
        catch (JsonException ex)
        {
            throw new ArgumentException(
                $"form 不是合法 JSON: {ex.Message}——form 须为 JSON 数组字符串，元素形如 "
                + "{\"id\":\"控件ID\",\"type\":\"控件类型\",\"value\":控件值}（控件ID来自审批定义的表单结构）");
        }

        if (node is not JsonArray)
        {
            throw new ArgumentException(
                "form 须为 JSON 数组字符串（如 [{\"id\":\"widget1\",\"type\":\"input\",\"value\":\"内容\"}]），"
                + "实际为" + (node is null ? "空值/null" : node.GetValueKind().ToString()));
        }
    }
}
