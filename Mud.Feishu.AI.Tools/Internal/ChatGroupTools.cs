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
    [FeishuToolHandler(typeof(IFeishuTenantChatgroupListChatsTool))]
    public Task<FeishuToolResult> ListChatsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
            async Task<FeishuApiOutcome<ApiPageListResult<ChatItemInfo>>> FetchPageAsync(string? token, CancellationToken ct)
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
    [FeishuToolHandler(typeof(IFeishuTenantChatgroupListModeratorsTool))]
    public Task<FeishuToolResult> ListModeratorsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.ChatgroupListModerators, _maxResultLength);
        if (Absent(executor.ToolName, _chatGroupClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = ChatgroupListModeratorsArgs.Unpack(arguments);

            async Task<FeishuApiOutcome<ChatGroupModeratorPageListResult>> FetchPageAsync(string? token, CancellationToken ct)
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
    [FeishuToolHandler(typeof(IFeishuTenantChatgroupIsInChatTool))]
    public Task<FeishuToolResult> IsInChatAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
    [FeishuToolHandler(typeof(IFeishuTenantChatgroupGetMenuTool))]
    public Task<FeishuToolResult> GetMenuAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
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
    private static FeishuToolResult? Absent(string toolName, object? client)
        => client is null
            ? FeishuToolResult.FromError(FeishuToolBinding.StructuredError(
                toolName,
                $"未注册 {ClientName} 客户端——群管理能力需宿主启用群管理 API（软缺席，不影响其它域工具）"))
            : null;
}
