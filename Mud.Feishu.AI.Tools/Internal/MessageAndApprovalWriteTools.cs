// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Feishu.DataModels.Approval;
using Mud.Feishu.DataModels.ApprovalQuery;
using Mud.Feishu.DataModels.ApprovalTask;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Internal;

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
    [FeishuToolHandler(typeof(IFeishuTenantImSendMessageTool))]
    public Task<FeishuToolResult> SendMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImSendMessage);
        return executor.RunAsync(async () =>
        {
            var args = ImSendMessageArgs.Unpack(arguments);
            var receiveIdType = args.ReceiveIdType ?? "chat_id";
            if (!ReceiveIdTypes.Allowed.Contains(receiveIdType, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"receive_id_type 仅支持 {string.Join("/", ReceiveIdTypes.Allowed)}，实际: {receiveIdType}");
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

    /// <summary>
    /// im.send_card：结构化描述 → <c>CardDsl</c> 编译 → <c>interactive</c> 卡片消息。
    /// </summary>
    /// <remarks>
    /// <b>模型全程零 JSON 字符串</b>（F-6 的目的）：入参是 DSL 文本，出参是编译好的卡片 JSON。
    /// 与 <c>im.send_message</c> 复用同一条 <c>SendMessageAsync</c> 发送路径（A-12）。
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuTenantImSendCardTool))]
    public Task<FeishuToolResult> SendCardAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ImSendCard);
        return executor.RunAsync(async () =>
        {
            var args = ImSendCardArgs.Unpack(arguments);
            var receiveIdType = args.ReceiveIdType ?? "chat_id";
            if (!ReceiveIdTypes.Allowed.Contains(receiveIdType, StringComparer.Ordinal))
            {
                throw new ArgumentException(
                    $"receive_id_type 仅支持 {string.Join("/", ReceiveIdTypes.Allowed)}，实际: {receiveIdType}");
            }

            // 编译期：DSL 非法组合在此报错并附合法组合（DoD）；不进入下游。
            var card = CardDsl.Compile(args.Title, args.Body, args.Buttons);
            var content = card.ToJsonString();

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/im/v1/messages",
                    "卡片结构（由 body/buttons 编译，非模型手写）：" + content
                    + ToolDryRun.IdempotencyNote(args.IdempotencyKey),
                    ("receive_id", args.ReceiveId.Length),
                    ("content", content.Length),
                    ("receive_id_type", receiveIdType.Length)));
            }

            var outcome = FeishuApiResultReader.Read(await _messageClient
                .SendMessageAsync(
                    new SendMessageRequest
                    {
                        ReceiveId = args.ReceiveId,
                        MsgType = "interactive",
                        Content = content,
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
/// Approval 工具执行器（<c>approval.create_instance</c> / <c>approval.list_pending_tasks</c> / <c>approval.approve_task</c> / <c>approval.get_instance</c>，WP4 动作面 + WP5 只读实例详情）：
/// 模型扁平参数 → 强类型请求 → SDK 调用 → 解包投影。
/// </summary>
/// <remarks>
/// <para>
/// <b>多客户端注入</b>：<c>create_instance</c> 走 <c>IFeishuTenantV4Approval</c>，
/// <c>list_pending_tasks</c> 走 <c>IFeishuTenantV4ApprovalQuery</c>，
/// <c>approve_task</c> 走 <c>IFeishuTenantV4ApprovalTask</c>，
/// <c>get_instance</c> 走 <c>IFeishuUserV4ApprovalInstance</c>（用户身份——WP5 新增）。
/// </para>
/// <para>
/// 写工具（<c>approve_task</c>）的软缺席语义：若 DI 容器缺少 <c>IFeishuTenantV4ApprovalTask</c>，
/// 执行器解析为 <c>null</c>，工具不进入注册表（装配期 fail-fast）。
/// </para>
/// </remarks>
internal sealed class ApprovalWriteTools(
    Mud.Feishu.IFeishuTenantV4Approval approvalClient,
    Mud.Feishu.IFeishuTenantV4ApprovalQuery? approvalQueryClient,
    Mud.Feishu.IFeishuTenantV4ApprovalTask? approvalTaskClient,
    Mud.Feishu.IFeishuUserV4ApprovalInstance? approvalInstanceUserClient,
    IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV4Approval _approvalClient = approvalClient
        ?? throw new ArgumentNullException(nameof(approvalClient));
    private readonly Mud.Feishu.IFeishuTenantV4ApprovalQuery? _approvalQueryClient = approvalQueryClient;
    private readonly Mud.Feishu.IFeishuTenantV4ApprovalTask? _approvalTaskClient = approvalTaskClient;
    private readonly Mud.Feishu.IFeishuUserV4ApprovalInstance? _approvalInstanceUserClient = approvalInstanceUserClient;
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;
    private readonly int _maxAutoFetchItems = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxAutoFetchPages;

    /// <summary>审批表单 JSON 的预览截断长度（get_instance 的 <c>form_preview</c> 落点；字面量常数，I16 纪律）。</summary>
    private const int FormPreviewLength = 500;

    /// <summary>approval.create_instance：发起审批实例（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>幂等键（T4-1 / F-1）：<c>idempotency_key</c> → <see cref="CreateInstanceRequest.Uuid"/>（冲突返回 60012）。</remarks>
    [FeishuToolHandler(typeof(IFeishuTenantApprovalCreateInstanceTool))]
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

    /// <summary>approval.list_pending_tasks：查询审批待办任务列表（分页，白名单 task_id/instance_code/approval_name/title/status）。</summary>
    /// <remarks>查询客户端缺席（宿主未注册 <c>IFeishuTenantV4ApprovalQuery</c>）→ 结构化错误。</remarks>
    [FeishuToolHandler(typeof(IFeishuTenantApprovalListPendingTasksTool))]
    public Task<FeishuToolResult> ListPendingTasksAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApprovalListPendingTasks, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            if (_approvalQueryClient is null)
            {
                throw new ArgumentException(
                    "approval.list_pending_tasks 需要 IFeishuTenantV4ApprovalQuery——宿主须启用 AddApprovalApi 的查询侧客户端");
            }

            var args = ApprovalListPendingTasksArgs.Unpack(arguments);

            var queryRequest = new ApprovalInstancesTaskQueryRequest
            {
                UserId = args.UserId,
                ApprovalCode = args.ApprovalCode,
                TaskStatus = "PENDING",
            };

            // B1：fetch_all=true 时循环翻页（唯一翻页实现 ToolPagination）。
            if (args.FetchAll == true)
            {
                var maxItems = ToolPagination.ResolveMaxItems(args.MaxItems, _maxAutoFetchItems);
                var aggregated = await ToolPagination.AggregateOutcomesAsync<ApprovalInstancesTaskQueryResult>(
                    async (token, ct) => FeishuApiResultReader.Read(await _approvalQueryClient
                        .GetTasksPageListAsync(
                            queryRequest,
                            page_size: PageSizes.ApprovalTasks,
                            page_token: token,
                            cancellationToken: ct)
                        .ConfigureAwait(false)),
                    page => (page.HasMore, page.PageToken),
                    page => ProjectPendingTasks(page)["items"]!.AsArray(),
                    maxItems,
                    ToolPagination.ResolveMaxPages(_maxAutoFetchPages),
                    cancellationToken).ConfigureAwait(false);
                return executor.FromPagedResult(aggregated, maxItems);
            }

            var outcome = FeishuApiResultReader.Read(await _approvalQueryClient
                .GetTasksPageListAsync(
                    queryRequest,
                    page_size: PageSizes.ApprovalTasks,
                    page_token: args.PageToken,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectPendingTasks);
        });
    }

    /// <summary>approval.approve_task：同意审批任务（<c>dry_run=true</c> 时只预演）。</summary>
    /// <remarks>无幂等键（飞书审批同意操作天然不可重复——同意后任务状态即变）。</remarks>
    [FeishuToolHandler(typeof(IFeishuTenantApprovalApproveTaskTool))]
    public Task<FeishuToolResult> ApproveTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApprovalApproveTask);
        return executor.RunAsync(async () =>
        {
            if (_approvalTaskClient is null)
            {
                throw new ArgumentException(
                    "approval.approve_task 需要 IFeishuTenantV4ApprovalTask——宿主须启用 AddApprovalApi 的任务侧客户端");
            }

            var args = ApprovalApproveTaskArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/approval/v4/tasks/approve",
                    ToolDryRun.IdempotencyNote(null),
                    ("approval_code", args.ApprovalCode.Length),
                    ("instance_code", args.InstanceCode.Length),
                    ("task_id", args.TaskId.Length),
                    ("user_id", args.UserId.Length),
                    ("comment", args.Comment?.Length ?? 0)));
            }

            var nullDataResult = await _approvalTaskClient
                .AgreeApprovalAsync(
                    new AgreeApprovalTasksRequest
                    {
                        ApprovalCode = args.ApprovalCode,
                        InstanceCode = args.InstanceCode,
                        TaskId = args.TaskId,
                        UserId = args.UserId,
                        Comment = args.Comment,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            // FeishuNullDataApiResult 不走 FeishuApiResultReader.Read<T>（T 不可推断），
            // 手动解包：Code != 0 → 失败，否则成功（data={} 无业务字段）。
            if (nullDataResult is null)
            {
                throw new ArgumentException("飞书接口无响应（result 为空）");
            }

            if (nullDataResult.Code != 0)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                    executor.ToolName, nullDataResult.Code,
                    $"飞书接口返回错误 code={nullDataResult.Code.ToString(CultureInfo.InvariantCulture)}, msg={nullDataResult.Msg ?? "(无错误信息)"}"));
            }

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["approved"] = true,
                ["task_id"] = args.TaskId,
            });
        });
    }

    /// <summary>
    /// approval.reject_task：拒绝审批任务（R5 / F-11 —— 补齐"只能同意"这一硬缺陷）。
    /// </summary>
    [FeishuToolHandler(typeof(IFeishuTenantApprovalRejectTaskTool))]
    public Task<FeishuToolResult> RejectTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApprovalRejectTask);
        return executor.RunAsync(async () =>
        {
            var client = RequireApprovalTaskClient(executor.ToolName);
            var args = ApprovalRejectTaskArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/approval/v4/tasks/reject",
                    ToolDryRun.IdempotencyNote(null),
                    ("approval_code", args.ApprovalCode.Length),
                    ("instance_code", args.InstanceCode.Length),
                    ("task_id", args.TaskId.Length),
                    ("user_id", args.UserId.Length),
                    ("comment", args.Comment?.Length ?? 0)));
            }

            var nullDataResult = await client
                .RejectApprovalAsync(
                    new RejectApprovalTaskRequest
                    {
                        ApprovalCode = args.ApprovalCode,
                        InstanceCode = args.InstanceCode,
                        TaskId = args.TaskId,
                        UserId = args.UserId,
                        Comment = args.Comment,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, nullDataResult);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["rejected"] = true,
                ["task_id"] = args.TaskId,
            });
        });
    }

    /// <summary>
    /// approval.transfer_task：转交审批任务（与 reject 同批补齐）。
    /// </summary>
    /// <remarks>
    /// <b>转交给自己必须提前拒绝</b>：平台会接受但语义上是空操作（流程不前进），
    /// 模型若不察觉会误以为转交成功。这是"构建通过、调用成功、但结果无意义"的一类。
    /// </remarks>
    [FeishuToolHandler(typeof(IFeishuTenantApprovalTransferTaskTool))]
    public Task<FeishuToolResult> TransferTaskAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApprovalTransferTask);
        return executor.RunAsync(async () =>
        {
            var client = RequireApprovalTaskClient(executor.ToolName);
            var args = ApprovalTransferTaskArgs.Unpack(arguments);

            if (string.Equals(args.TransferUserId, args.UserId, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"transfer_user_id 与 user_id 相同（'{args.UserId}'）—— 转交给自己不会推进审批流程，"
                    + "请指定另一位审批人");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return FeishuToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName, "POST", "/open-apis/approval/v4/tasks/transfer",
                    ToolDryRun.IdempotencyNote(null),
                    ("approval_code", args.ApprovalCode.Length),
                    ("instance_code", args.InstanceCode.Length),
                    ("task_id", args.TaskId.Length),
                    ("user_id", args.UserId.Length),
                    ("transfer_user_id", args.TransferUserId.Length),
                    ("comment", args.Comment?.Length ?? 0)));
            }

            var nullDataResult = await client
                .TransferApprovalAsync(
                    new TransferApprovalTasksRequest
                    {
                        ApprovalCode = args.ApprovalCode,
                        InstanceCode = args.InstanceCode,
                        TaskId = args.TaskId,
                        UserId = args.UserId,
                        TransferUserId = args.TransferUserId,
                        Comment = args.Comment,
                    },
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, nullDataResult);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["transferred"] = true,
                ["task_id"] = args.TaskId,
                ["transfer_user_id"] = args.TransferUserId,
            });
        });
    }

    /// <summary>
    /// 取审批任务侧客户端；未注册时给出<b>可执行</b>的提示（宿主该启用什么）。
    /// </summary>
    private IFeishuTenantV4ApprovalTask RequireApprovalTaskClient(string toolName)
        => _approvalTaskClient
            ?? throw new ArgumentException(
                $"{toolName} 需要 IFeishuTenantV4ApprovalTask——宿主须启用 AddApprovalApi 的任务侧客户端");

    /// <summary>
    /// 解包 <c>FeishuNullDataApiResult</c>（不走 <c>FeishuApiResultReader.Read&lt;T&gt;</c>，T 不可推断）。
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

    /// <summary>approval.get_instance：获取审批实例详情（白名单 instance_code/status/form/auditors，user-only）。</summary>
    /// <remarks>用户身份工具——宿主须在 AllowedIdentities 放行 user。客户端缺席时返回结构化错误。</remarks>
    [FeishuToolHandler(typeof(IFeishuUserApprovalGetInstanceTool))]
    public Task<FeishuToolResult> GetInstanceAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ApprovalGetInstance, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            if (_approvalInstanceUserClient is null)
            {
                throw new ArgumentException(
                    "approval.get_instance 需要 IFeishuUserV4ApprovalInstance（用户令牌）——宿主须启用 AddApprovalApi 的实例侧客户端并在 AllowedIdentities 放行 user");
            }

            var args = ApprovalGetInstanceArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _approvalInstanceUserClient
                .GetInstanceDetailAsync(
                    args.InstanceCode,
                    locale: args.Locale,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectInstanceDetail);
        });
    }

    /// <summary>list_pending_tasks 投影：items（task_id/instance_code/approval_name/title/status）+ 翻页契约。</summary>
    private static JsonObject ProjectPendingTasks(ApprovalInstancesTaskQueryResult data)
    {
        var envelope = new JsonObject
        {
            ["items"] = new JsonArray(),
            ["has_more"] = data.HasMore,
        };
        if (!string.IsNullOrEmpty(data.PageToken))
        {
            envelope["page_token"] = data.PageToken;
        }

        foreach (var item in data.TaskLists ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["task_id"] = item.Task?.TaskId,
                ["instance_code"] = item.Instance?.Code,
                ["approval_code"] = item.Approval?.Code,
                ["approval_name"] = item.Approval?.Name,
                ["title"] = item.Task?.Title ?? item.Instance?.Title,
                ["status"] = item.Task?.Status,
                ["start_time"] = item.Task?.StartTime,
            });
        }

        return envelope;
    }

    /// <summary>get_instance 投影：instance_code/status/form（截断预览）/auditors。</summary>
    private static JsonObject ProjectInstanceDetail(InstanceDetailResult data)
    {
        var envelope = new JsonObject
        {
            ["instance_code"] = data.InstanceCode,
            ["definition_name"] = data.DefinitionName,
            ["status"] = data.Status,
            ["start_time"] = data.StartTime,
            ["end_time"] = data.EndTime,
            ["user_id"] = data.UserId,
            ["serial_number"] = data.SerialNumber,
            ["reverted"] = data.Reverted,
        };

        // form 是 JSON 字符串，可能很长——只取前 500 字符做预览。
        // 局部化后再判空（R2-08）：`data.Form` 是**另一个对象的属性**，此处前后虽无调用，
        // 但把可空性判断与取值绑定到同一局部变量可让编译器完全接管（消除 CS8602，不依赖流分析对属性的建模）。
        if (data.Form is { Length: > 0 } form)
        {
            envelope["form_preview"] = form.Length > FormPreviewLength
                ? form[..FormPreviewLength] + "…[truncated]"
                : form;
        }

        // 审批任务列表（白名单 task_id/user_id/status/node_name）
        if (data.Tasks is { Length: > 0 })
        {
            var tasks = new JsonArray();
            foreach (var task in data.Tasks)
            {
                tasks.AddNode(new JsonObject
                {
                    ["task_id"] = task.Id,
                    ["user_id"] = task.UserId,
                    ["status"] = task.Status,
                    ["node_name"] = task.NodeName,
                    ["type"] = task.Type,
                });
            }
            envelope["tasks"] = tasks;
        }

        return envelope;
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
