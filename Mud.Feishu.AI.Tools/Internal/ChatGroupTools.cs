// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.ChatGroup;
using Mud.Feishu.DataModels.ChatGroupMember;
using Mud.Feishu.DataModels.ChatGroupMenu;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// ChatGroup「群管理」只读工具执行器（B1 第四个补域：4 个只读工具）。
/// </summary>
/// <remarks>
/// <para>
/// <b>域边界（去重评审的落地）</b>：本域<b>只补既有工具面缺的那半边</b>——
/// 群列表 / 发言权限名单 / 在群预检 / 群菜单。
/// 既有 im 域已覆盖 <c>get_chat</c>（群详情）、<c>search_chats</c>（关键词搜索）、
/// <c>list_chat_members</c>（成员列表），群公告由 docx 域的
/// <c>get_chat_announcement</c>/<c>set_chat_announcement</c> 覆盖（ChatGroup 家族的公告接口
/// 与 Docx 家族同路径，再策展即重复）⇒ 这些能力本域一律不重复实现。
/// </para>
/// <para>
/// <b>软依赖</b>：三个客户端全部可空注入——宿主未启用群管理 API 时本执行器软缺席。
/// </para>
/// <para>
/// <b>命名口径（三套来源，逐一实测过）</b>：
/// <list type="number">
/// <item><b>工具名派生</b>（常量 / 参数类 / 注册器）：前缀 <c>chatgroup</c> 被规范化为 <c>Chatgroup</c>
/// （只首字母大写）⇒ <c>FeishuToolNames.ChatgroupListChats</c>、<c>ChatgroupListChatsArgs</c>；</item>
/// <item><b>执行器类名派生</b>（DI 入口）：<c>ChatGroupTools</c> ⇒ <c>AddFeishuChatGroupToolsCore()</c>
/// —— 注意它与第 1 条**不同源**（HelpDesk 域改名类后这两者才碰巧一致，容易误以为同源）；</item>
/// <item><b>SDK 接口名</b>另有口径：<c>IFeishuTenantV1ChatGroup*</c>（SDK 侧按专有名词大写）。</item>
/// </list>
/// </para>
/// </remarks>
internal sealed class ChatGroupTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1ChatGroup? chatGroupClient = null,
    Mud.Feishu.IFeishuTenantV1ChatGroupMember? memberClient = null,
    Mud.Feishu.IFeishuTenantV1ChatGroupMenu? menuClient = null)
{
    /// <summary>群描述与菜单名的预览截断长度。</summary>
    private const int DescriptionPreviewLength = 200;

    /// <summary>群列表的默认排序（SDK 默认口径，显式写出来避免"默认值藏在 SDK 里"）。</summary>
    private const string DefaultSortType = "ByCreateTimeAsc";

    /// <summary>客户端缺席时的统一说明。</summary>
    private const string ClientName = "IFeishuTenantV1ChatGroup*（群管理 API）";

    private readonly Mud.Feishu.IFeishuTenantV1ChatGroup? _chatGroupClient = chatGroupClient;
    private readonly Mud.Feishu.IFeishuTenantV1ChatGroupMember? _memberClient = memberClient;
    private readonly Mud.Feishu.IFeishuTenantV1ChatGroupMenu? _menuClient = menuClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>统一分页语义（专题四）：<c>fetch_all</c> 的条目 / 页数预算。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    // ─────────────────────────── chatgroup.list_chats ───────────────────────────

    /// <summary>chatgroup.list_chats：列出机器人所在的群（本域第一跳）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupListChatsTool))]
    public Task<ToolResult> ListChatsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupListChats, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupListChatsArgs.Unpack(arguments);

            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
            async Task<ToolApiOutcome<ApiPageListResult<ChatItemInfo>>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _chatGroupClient!
                    .GetChatGroupPageListAsync(
                        sort_type: args.SortType ?? DefaultSortType,
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectChats, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>群列表投影：标识 + 群名 + 群主 + 外部群标志（**不带** tenant_key 这类组织标识）。</summary>
    private static JsonObject ProjectChats(ApiPageListResult<ChatItemInfo> result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var chat in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["chat_id"] = chat.ChatId,
                ["name"] = chat.Name,
                ["description"] = ToolResultText.Truncate(chat.Description, DescriptionPreviewLength),
                ["owner_id"] = chat.OwnerId,
                ["owner_id_type"] = chat.OwnerIdType,
                ["external"] = chat.External,
                ["chat_status"] = chat.ChatStatus,
            });
        }

        if (items.Count == 0)
        {
            // 空结果哨兵（F-5）：这里"空"通常意味着机器人一个群都没被拉进去，要说清下一步。
            envelope["found"] = false;
            envelope["message"] = "机器人当前不在任何群里。请先在目标群里添加该应用（群设置 → 群机器人），"
                + "之后再调用本工具，chat_id 才会出现；要按名字找群可用 im.search_chats。";
        }

        return envelope;
    }

    // ─────────────────────────── chatgroup.list_moderators ───────────────────────────

    /// <summary>chatgroup.list_moderators：查询发言权限名单（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupListModeratorsTool))]
    public Task<ToolResult> ListModeratorsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupListModerators, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupListModeratorsArgs.Unpack(arguments);

            async Task<ToolApiOutcome<ChatGroupModeratorPageListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _chatGroupClient!
                    .GetChatGroupModeratorPageListByIdAsync(args.ChatId, page_token: token, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectModerators, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>发言权限投影：设置项 + 名单（只给 user_id 与 ID 类型）。</summary>
    private static JsonObject ProjectModerators(ChatGroupModeratorPageListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        envelope["moderation_setting"] = result.ModerationSetting;

        var items = envelope["items"]!.AsArray();
        foreach (var moderator in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["user_id"] = moderator.UserId,
                ["user_id_type"] = moderator.UserIdType,
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "该群没有单独配置发言白名单（moderation_setting 为 all 时表示全员可发言）；"
                + "要确认群主与管理员请用 im.get_chat。";
        }

        return envelope;
    }

    // ─────────────────────────── chatgroup.is_in_chat ───────────────────────────

    /// <summary>chatgroup.is_in_chat：发消息前的在群预检。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupIsInChatTool))]
    public Task<ToolResult> IsInChatAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupIsInChat, _maxResultLength);
        if (Absent(executor.ToolName, _memberClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupIsInChatArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _memberClient!
                .GetMemberInChatByIdAsync(args.ChatId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["is_in_chat"] = result.IsInChat,

                // 把"结论 → 下一步"写在结果里：模型不必再猜不在群该怎么办。
                ["notice"] = result.IsInChat == true
                    ? "当前身份在该群内，可以继续调用 im.send_message 发消息。"
                    : "当前身份**不在**该群：请先把应用添加到群里（群设置 → 群机器人），否则对它的消息调用会失败。",
            });
        });
    }

    // ─────────────────────────── chatgroup.get_menu ───────────────────────────

    /// <summary>chatgroup.get_menu：查询群快捷菜单配置。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupGetMenuTool))]
    public Task<ToolResult> GetMenuAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupGetMenu, _maxResultLength);
        if (Absent(executor.ToolName, _menuClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupGetMenuArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _menuClient!
                .GetMenuByIdAsync(args.ChatId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectMenu);
        });
    }

    /// <summary>菜单投影：一级菜单 + 二级菜单（名称 / 动作类型 / 跳转链接）；无个人信息。</summary>
    private static JsonObject ProjectMenu(ChatGroupMenuResult result)
    {
        var items = new JsonArray();
        foreach (var top in result.MenuTree?.ChatMenuTopLevels ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["chat_menu_top_level_id"] = top.ChatMenuTopLevelId,
                ["name"] = top.ChatMenuItem?.Name,
                ["action_type"] = top.ChatMenuItem?.ActionType,
                ["url"] = top.ChatMenuItem?.RedirectLink?.CommonUrl,
                ["children"] = SecondLevels(top.Childrens),
            });
        }

        var payload = new JsonObject { ["items"] = items };

        if (items.Count == 0)
        {
            payload["found"] = false;
            payload["message"] = "该群没有配置快捷菜单（菜单是可选配置，空即未配置，不是调用失败）。";
        }

        return payload;
    }

    private static JsonArray SecondLevels(ChatMenuSecondLevelInfo[]? children)
    {
        var array = new JsonArray();
        foreach (var child in children ?? [])
        {
            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            array.AddNode(new JsonObject
            {
                ["chat_menu_second_level_id"] = child.ChatMenuSecondLevelId,
                ["name"] = child.ChatMenuItem?.Name,
                ["action_type"] = child.ChatMenuItem?.ActionType,
                ["url"] = child.ChatMenuItem?.RedirectLink?.CommonUrl,
            });
        }

        return array;
    }

    /// <summary>
    /// 软缺席：客户端为空时给出<b>可执行的错误结果</b>而不是抛异常
    /// （异常会逃出执行链抛给宿主，而这里要的是"模型看到一条明确的不可用说明"）。
    /// </summary>
    private static ToolResult? Absent(string toolName, object? client)
        => client is null
            ? ToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName,
                $"未注册 {ClientName} 客户端——群管理能力需宿主启用群管理 API（软缺席，不影响其它域工具）"))
            : null;

    // ─────────────────── 写面：群设置 / 成员 / 管理员 / 置顶 / 菜单删除 ───────────────────

    /// <summary>chatgroup.create_chat：创建群。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupCreateChatTool))]
    public Task<ToolResult> CreateChatAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupCreateChat, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupCreateChatArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/im/v1/chats",
                    ToolDryRun.IdempotencyNote(null),
                    ("name", args.Name?.Length ?? 0),
                    ("user_id_list", args.UserIdList?.Length ?? 0),
                    ("chat_type", args.ChatType?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _chatGroupClient!
                .CreateChatGroupAsync(
                    new CreateChatRequest
                    {
                        Name = args.Name,
                        Description = args.Description,
                        UserIdList = args.UserIdList?.ToList(),
                        ChatType = args.ChatType,
                        GroupMessageType = args.GroupMessageType,
                        EditPermission = args.EditPermission,
                        MembershipApproval = args.MembershipApproval,
                    },
                    user_id_type: args.UserIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = data.ChatId,
                ["owner_id_type"] = data.OwnerIdType,
                ["created"] = true,
                ["notice"] = "chat_id 可再用于 im.get_chat / im.list_chat_members / chatgroup.* 系列；"
                    + "无效成员 ID 由平台的 invalid_user_id_list 体现（本投影只回有效结果）。",
            });
        });
    }

    /// <summary>chatgroup.update_chat：增量更新群设置（未传字段保持原值）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupUpdateChatTool))]
    public Task<ToolResult> UpdateChatAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupUpdateChat, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupUpdateChatArgs.Unpack(arguments);

            var changed = CountSet(args.Name, args.Description, args.EditPermission, args.AddMemberPermission,
                args.ShareCardPermission, args.AtAllPermission, args.GroupMessageType);
            if (changed == 0)
            {
                return Invalid(executor.ToolName,
                    "至少传一个要修改的字段（name / description / edit_permission / add_member_permission / "
                    + "share_card_permission / at_all_permission / group_message_type）——全空等于空更新。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "PUT",
                    "/open-apis/im/v1/chats/{chat_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("changed_fields", changed)));
            }

            var outcome = FeishuApiResultReader.Read(await _chatGroupClient!
                .UpdateChatGroupByIdAsync(
                    args.ChatId,
                    new UpdateChatRequest
                    {
                        Name = args.Name,
                        Description = args.Description,
                        EditPermission = args.EditPermission,
                        AddMemberPermission = args.AddMemberPermission,
                        ShareCardPermission = args.ShareCardPermission,
                        AtAllPermission = args.AtAllPermission,
                        GroupMessageType = args.GroupMessageType,
                    },
                    user_id_type: args.UserIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = data.ChatId ?? args.ChatId,
                ["updated_fields"] = changed,
                ["updated"] = true,
                ["notice"] = "本次按**增量更新**处理：未传的字段保持原值（与互通规则的覆盖式语义不同）。",
            });
        });
    }

    /// <summary>chatgroup.delete_chat：解散群（high-risk-write）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupDeleteChatTool))]
    public Task<ToolResult> DeleteChatAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupDeleteChat, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupDeleteChatArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "DELETE",
                    "/open-apis/im/v1/chats/{chat_id}",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0)));
            }

            var result = await _chatGroupClient!
                .DeleteChatGroupAsync(args.ChatId, cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["deleted"] = true,
                ["notice"] = "解散不可逆：群内历史消息随群一起消失，只能重建（会拿到新 chat_id）。",
            });
        });
    }

    /// <summary>chatgroup.update_moderation：更新群发言权限。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupUpdateModerationTool))]
    public Task<ToolResult> UpdateModerationAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupUpdateModeration, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupUpdateModerationArgs.Unpack(arguments);

            // 参数校验早于 dry_run：moderator_list 模式不给名单等于"谁都不能发言"，
            // 那是把群静音的静默事故，必须在预演之前就拦下。
            var isModeratorList = string.Equals(args.ModerationSetting, "moderator_list", StringComparison.Ordinal);
            if (isModeratorList
                && (args.ModeratorAddedList is not { Length: > 0 } && args.ModeratorRemovedList is not { Length: > 0 }))
            {
                return Invalid(executor.ToolName,
                    "moderation_setting=moderator_list 时必须同时给 moderator_added_list 或 moderator_removed_list"
                    + "——否则该模式下没有可发言成员，等于把全群静音。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "PUT",
                    "/open-apis/im/v1/chats/{chat_id}/moderation",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("moderation_setting", args.ModerationSetting?.Length ?? 0),
                    ("moderator_added", args.ModeratorAddedList?.Length ?? 0),
                    ("moderator_removed", args.ModeratorRemovedList?.Length ?? 0)));
            }

            var result = await _chatGroupClient!
                .UpdateChatModerationAsync(
                    args.ChatId,
                    new UpdateChatModerationRequest
                    {
                        ModerationSetting = args.ModerationSetting,
                        ModeratorAddedList = args.ModeratorAddedList?.ToList() ?? [],
                        ModeratorRemovedList = args.ModeratorRemovedList?.ToList() ?? [],
                    },
                    user_id_type: args.MemberIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["moderation_setting"] = args.ModerationSetting,
                ["updated"] = true,
                ["notice"] = isModeratorList
                    ? "当前为「指定成员可发言」模式：不在名单里的成员无法在群内发言。"
                    : "已切换发言模式；切换到 moderator_list 时必须给名单，否则等于全群静音。",
            });
        });
    }

    /// <summary>chatgroup.add_member：拉成员入群。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupAddMemberTool))]
    public Task<ToolResult> AddMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupAddMember, _maxResultLength);
        if (Absent(executor.ToolName, _memberClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupAddMemberArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/im/v1/chats/{chat_id}/members",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("id_list", args.IdList?.Length ?? 0),
                    ("succeed_type", args.SucceedType ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _memberClient!
                .AddMemberAsync(
                    args.ChatId,
                    new MembersRequest { IdList = args.IdList?.ToList() ?? [] },
                    member_id_type: args.MemberIdType,
                    succeed_type: args.SucceedType ?? 0,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["invalid_id_list"] = StringArray(data.InvalidIdList),
                ["not_existed_id_list"] = StringArray(data.NotExistedIdList),
                ["pending_approval_id_list"] = StringArray(data.PendingApprovalIdList),
                ["added"] = true,
                ["notice"] = "三份清单是平台对**部分失败**的明细：非空说明有成员没进群（类型不匹配 / 不存在 / 待审批），"
                    + "不要当成全员拉入。",
            });
        });
    }

    /// <summary>chatgroup.remove_member：移成员出群（high-risk-write）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupRemoveMemberTool))]
    public Task<ToolResult> RemoveMemberAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupRemoveMember, _maxResultLength);
        if (Absent(executor.ToolName, _memberClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupRemoveMemberArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "DELETE",
                    "/open-apis/im/v1/chats/{chat_id}/members",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("id_list", args.IdList?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _memberClient!
                .RemoveMemberAsync(
                    args.ChatId,
                    new MembersRequest { IdList = args.IdList?.ToList() ?? [] },
                    member_id_type: args.MemberIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["invalid_id_list"] = StringArray(data.InvalidIdList),
                ["removed"] = true,
                ["notice"] = "被移出的成员立即失去群内可见性，且**不会**收到单独通知（只有群成员数变化）。",
            });
        });
    }

    /// <summary>chatgroup.me_join_chat：当前身份加入群。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupMeJoinChatTool))]
    public Task<ToolResult> MeJoinChatAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupMeJoinChat, _maxResultLength);
        if (Absent(executor.ToolName, _memberClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupMeJoinChatArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "PATCH",
                    "/open-apis/im/v1/chats/{chat_id}/members/me_join",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0)));
            }

            var result = await _memberClient!
                .MeJoinChatGroupAsync(args.ChatId, cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["joined"] = true,
                ["notice"] = "加入的是**当前应用/机器人身份**（不是某个用户）；公开群可直接加入，私密群需已在群内或被邀请。",
            });
        });
    }

    /// <summary>chatgroup.add_manager：设置群管理员。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupAddManagerTool))]
    public Task<ToolResult> AddManagerAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupAddManager, _maxResultLength);
        if (Absent(executor.ToolName, _memberClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupAddManagerArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/im/v1/chats/{chat_id}/managers/add_managers",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("manager_ids", args.ManagerIds?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _memberClient!
                .AddManagersAsync(
                    args.ChatId,
                    new GroupManagerRequest { ManagerIds = args.ManagerIds?.ToList() ?? [] },
                    member_id_type: args.MemberIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["chat_managers"] = StringArray(data.ChatManagers),
                ["chat_bot_managers"] = StringArray(data.ChatBotManagers),
                ["added"] = true,
            });
        });
    }

    /// <summary>chatgroup.delete_manager：撤销群管理员（high-risk-write）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupDeleteManagerTool))]
    public Task<ToolResult> DeleteManagerAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupDeleteManager, _maxResultLength);
        if (Absent(executor.ToolName, _memberClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupDeleteManagerArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/im/v1/chats/{chat_id}/managers/delete_managers",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("manager_ids", args.ManagerIds?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _memberClient!
                .DeleteManagersAsync(
                    args.ChatId,
                    new GroupManagerRequest { ManagerIds = args.ManagerIds?.ToList() ?? [] },
                    member_id_type: args.MemberIdType,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["chat_managers"] = StringArray(data.ChatManagers),
                ["chat_bot_managers"] = StringArray(data.ChatBotManagers),
                ["deleted"] = true,
                ["notice"] = "被撤销的成员立即失去群管理能力（改设置 / 踢人 / 置顶等），且不会收到通知。",
            });
        });
    }

    /// <summary>chatgroup.put_top_notice：设置群置顶。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupPutTopNoticeTool))]
    public Task<ToolResult> PutTopNoticeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupPutTopNotice, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupPutTopNoticeArgs.Unpack(arguments);

            // 参数校验早于 dry_run：action_type=1 不给 message_id 是必然失败的请求。
            if (string.Equals(args.ActionType, "1", StringComparison.Ordinal)
                && string.IsNullOrEmpty(args.MessageId))
            {
                return Invalid(executor.ToolName,
                    "action_type=1（消息类型置顶）必须配 message_id——"
                    + "若想置顶群公告请用 action_type=2（不需要 message_id）。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/im/v1/chats/{chat_id}/top_notice/put_top_notice",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("action_type", args.ActionType?.Length ?? 0),
                    ("message_id", args.MessageId?.Length ?? 0)));
            }

            var result = await _chatGroupClient!
                .PutChatGroupTopNoticeAsync(
                    args.ChatId,
                    new ChatTopNoticeRequest
                    {
                        ChatTopNotice = [new ChatTopNotice { ActionType = args.ActionType, MessageId = args.MessageId }],
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["action_type"] = args.ActionType,
                ["message_id"] = args.MessageId,
                ["updated"] = true,
            });
        });
    }

    /// <summary>chatgroup.delete_top_notice：取消群置顶（high-risk-write）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupDeleteTopNoticeTool))]
    public Task<ToolResult> DeleteTopNoticeAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupDeleteTopNotice, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupDeleteTopNoticeArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/im/v1/chats/{chat_id}/top_notice/delete_top_notice",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0)));
            }

            var result = await _chatGroupClient!
                .DeleteChatGroupTopNoticeAsync(args.ChatId, cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["deleted"] = true,
                ["notice"] = "全群可见的置顶已消失；被置顶的消息本身不丢失（仍在群历史里）。",
            });
        });
    }

    /// <summary>chatgroup.delete_menu：删除群菜单一级入口（high-risk-write）。</summary>
    [ToolHandler(typeof(IFeishuTenantChatgroupDeleteMenuTool))]
    public Task<ToolResult> DeleteMenuAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupDeleteMenu, _maxResultLength);
        if (Absent(executor.ToolName, _menuClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupDeleteMenuArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "DELETE",
                    "/open-apis/im/v1/chats/{chat_id}/menu_tree",
                    ToolDryRun.IdempotencyNote(null),
                    ("chat_id", args.ChatId?.Length ?? 0),
                    ("chat_menu_top_level_ids", args.ChatMenuTopLevelIds?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _menuClient!
                .DeleteMenuByIdAsync(
                    args.ChatId,
                    new ChartMenuIdsRequest { ChatMenuTopLevelIds = args.ChatMenuTopLevelIds },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = args.ChatId,
                ["deleted_menu_count"] = args.ChatMenuTopLevelIds?.Length ?? 0,
                ["remaining_menu_count"] = data.MenuTree?.ChatMenuTopLevels?.Length ?? 0,
                ["deleted"] = true,
                ["notice"] = "二级菜单随父级一起消失；新增/修改菜单暂未纳入工具面（请求体是嵌套树结构）。",
            });
        });
    }

    // ─────────────────── 写面内部工具 ───────────────────

    /// <summary>数一下本次实际传了几个字段（dry_run 与回执只报条数，不回原文）。</summary>
    private static int CountSet(params string?[] values)
        => values.Count(static value => !string.IsNullOrEmpty(value));

    /// <summary>ID 清单转 JsonArray（空清单给空数组而不是 null）。</summary>
    private static JsonArray StringArray(IReadOnlyList<string>? values)
    {
        var array = new JsonArray();
        foreach (var value in values ?? [])
        {
            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            array.AddNode(value);
        }

        return array;
    }

    /// <summary>参数非法：结构化错误（不是异常——异常会逃出执行链，模型看不到可执行信息）。</summary>
    private static ToolResult Invalid(string toolName, string message)
        => ToolResult.FromError(FeishuToolBinding.StructuredError(toolName, message));

    /// <summary>
    /// 空数据结果的成功判定（delete / me_join / moderation / top_notice 返回
    /// <see cref="FeishuNullDataApiResult"/>，其 Data 天然为 null，不能用
    /// <c>FeishuApiResultReader.Read</c>——那会把"无载荷"误判为失败）。
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
}
