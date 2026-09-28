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
internal sealed class MessageWriteTools(Mud.Feishu.IFeishuTenantV1Message messageClient)
{
    private readonly Mud.Feishu.IFeishuTenantV1Message _messageClient = messageClient
        ?? throw new ArgumentNullException(nameof(messageClient));

    /// <summary>im.send_message：发送文本消息（<c>dry_run=true</c> 时只预演）。</summary>
    public async Task<FeishuToolResult> SendMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var receiveId = ToolArgs.RequireString(arguments, "receive_id");
            var text = ToolArgs.RequireString(arguments, "text");
            var receiveIdType = ToolArgs.OptionalString(arguments, "receive_id_type") ?? "chat_id";
            if (!EditMessageChannel.AllowedReceiveIdTypes.Contains(receiveIdType, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"receive_id_type 仅支持 {string.Join("/", EditMessageChannel.AllowedReceiveIdTypes)}，实际: {receiveIdType}");
            }

            if (ToolDryRun.IsRequested(arguments))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    FeishuToolNames.ImSendMessage, "POST", "/open-apis/im/v1/messages",
                    ("receive_id", receiveId.Length), ("text", text.Length), ("receive_id_type", receiveIdType.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .SendMessageAsync(
                    new SendMessageRequest
                    {
                        ReceiveId = receiveId,
                        MsgType = "text",
                        Content = new JsonObject { ["text"] = text }.ToJsonString(),
                    },
                    receiveIdType,
                    cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ImSendMessage, outcome.Code, outcome.ErrorText!));
            }

            return FeishuToolResult.FromText(new JsonObject {
                ["message_id"] = outcome.Data!.MessageId,
            }.ToJsonString());
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ImSendMessage, ex.Message));
        }
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
    public async Task<FeishuToolResult> AddRecordAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var appToken = ToolArgs.RequireString(arguments, "app_token");
            var tableId = ToolArgs.RequireString(arguments, "table_id");
            var fieldsJson = ToolArgs.RequireString(arguments, "fields");

            // 参数合法性校验先于 dry_run 判定：预演的价值在于"能提前发现的问题都提前发现"，
            // 若预演放过了非法 fields，模型会误以为参数没问题而在真实下发时才失败。
            var fields = ParseFieldsObject(fieldsJson);

            if (ToolDryRun.IsRequested(arguments))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    FeishuToolNames.BitableAddRecord, "POST",
                    "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records",
                    ("app_token", appToken.Length), ("table_id", tableId.Length), ("fields", fieldsJson.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _recordClient
                .AddRecordAsync(appToken, tableId, new RecordOpsRequest { Fields = fields }, cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableAddRecord, outcome.Code, outcome.ErrorText!));
            }

            return FeishuToolResult.FromText(new JsonObject {
                ["record_id"] = outcome.Data!.Record?.RecordId,
            }.ToJsonString());
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.BitableAddRecord, ex.Message));
        }
    }

    /// <summary>fields 参数 → JSON 对象 <see cref="JsonElement"/>（非对象/非法 JSON 转结构化错误）。</summary>
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
    public async Task<FeishuToolResult> CreateInstanceAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var approvalCode = ToolArgs.RequireString(arguments, "approval_code");
            var form = ToolArgs.RequireString(arguments, "form");
            var userId = ToolArgs.OptionalString(arguments, "user_id");

            // AT-F13③：form 是裸 JSON 字符串参数，此前**无任何形状校验**——非 JSON / 非数组
            // 会被原样下发，飞书侧返回一个语焉不详的 code，模型只能盲试。此处与 fields 同级校验。
            ValidateFormArray(form);

            if (ToolDryRun.IsRequested(arguments))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    FeishuToolNames.ApprovalCreateInstance, "POST", "/open-apis/approval/v4/instances",
                    ("approval_code", approvalCode.Length), ("form", form.Length), ("user_id", userId?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _approvalClient
                .CreateInstanceAsync(
                    new CreateInstanceRequest
                    {
                        ApprovalCode = approvalCode,
                        Form = form,
                        UserId = userId,
                    },
                    cancellationToken)
                .ConfigureAwait(false));
            if (!outcome.Ok)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ApprovalCreateInstance, outcome.Code, outcome.ErrorText!));
            }

            return FeishuToolResult.FromText(new JsonObject {
                ["instance_code"] = outcome.Data!.InstanceCode,
            }.ToJsonString());
        }
        catch (ArgumentException ex)
        {
            return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(FeishuToolNames.ApprovalCreateInstance, ex.Message));
        }
    }

    /// <summary>form 参数 → JSON 数组校验（非数组/非法 JSON 转结构化错误，附修复指引）。</summary>
    /// <remarks>与 <see cref="BitableWriteTools.ParseFieldsObject"/> 同一体例：裸 JSON 参数不得静默下发。</remarks>
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
