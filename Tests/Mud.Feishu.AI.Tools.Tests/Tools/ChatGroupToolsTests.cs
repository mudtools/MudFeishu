// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.ChatGroup;
using Mud.Feishu.DataModels.ChatGroupMember;
using Mud.Feishu.DataModels.ChatGroupMenu;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// ChatGroup 群管理域级测试（专题一 B1 第四个补域，SOP 第 8 步）。
/// </summary>
/// <remarks>
/// <para>
/// 本域是"去重评审"的落地：只补既有工具面缺的那半边（群列表 / 发言名单 / 在群预检 / 群菜单）。
/// 故测试同时钉住两件事：<b>新能力正确</b>，以及<b>边界不外扩</b>
/// （例如 <c>is_in_chat</c> 不给"某个成员在不在群"）。
/// </para>
/// <para>
/// policy 拒绝零调用与审计三类投递由 <c>ToolPolicyContractGuards</c> /
/// <c>FeishuToolBindingAuditTests</c> 通用覆盖，本类不重复。
/// </para>
/// </remarks>
public class ChatGroupToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static ChatGroupTools CreateTools(
        Mock<Mud.Feishu.IFeishuTenantV1ChatGroup>? chatGroup = null,
        Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>? member = null,
        Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMenu>? menu = null)
        => new(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            chatGroup?.Object,
            member?.Object,
            menu?.Object);

    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static FeishuApiPageListResult<ChatItemInfo> ChatPage(
        bool hasMore, string? pageToken, params string[] chatIds)
        => new()
        {
            Code = 0,
            Data = new ApiPageListResult<ChatItemInfo>
            {
                HasMore = hasMore,
                PageToken = pageToken,
                Items =
                [
                    .. chatIds.Select(id => new ChatItemInfo
                    {
                        ChatId = id,
                        Name = $"群-{id}",
                        Description = "测试群",
                        OwnerId = "ou_owner",
                        OwnerIdType = "open_id",
                        External = false,
                        ChatStatus = "normal",
                        TenantKey = "tk_secret_tenant",
                    }),
                ],
            },
        };

    // ─────────────────────────── chatgroup.list_chats ───────────────────────────

    [Fact]
    public async Task ListChats_Should_Project_Summary_Without_TenantKey()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroup>();
        client.Setup(c => c.GetChatGroupPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ChatPage(hasMore: false, pageToken: null, "oc_1"));

        var payload = Payload(await CreateTools(chatGroup: client).ListChatsAsync(Args(), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!.AsObject();
        item["chat_id"]!.GetValue<string>().Should().Be("oc_1");
        item["name"]!.GetValue<string>().Should().Be("群-oc_1");
        item["owner_id"]!.GetValue<string>().Should().Be("ou_owner");
        item["external"]!.GetValue<bool>().Should().BeFalse();

        // 出站净化：组织标识（tenant_key）不进模型上下文。
        payload.ToJsonString().Should().NotContain("tk_secret_tenant");
        item.ContainsKey("tenant_key").Should().BeFalse();
    }

    [Fact]
    public async Task ListChats_FetchAll_Should_Aggregate_Pages()
    {
        var calls = 0;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroup>();
        client.Setup(c => c.GetChatGroupPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string __, int? ___, string? pageToken, CancellationToken ____) =>
            {
                calls++;
                return ChatPage(pageToken is null, pageToken is null ? "t2" : null, pageToken is null ? "oc_1" : "oc_2");
            });

        var payload = Payload(await CreateTools(chatGroup: client).ListChatsAsync(
            Args(("fetch_all", true)), CancellationToken.None));

        calls.Should().Be(2, "fetch_all 必须循环到 has_more=false");
        payload["items"]!.AsArray().Count.Should().Be(2);
        payload["truncated"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task ListChats_Should_Return_Executable_Sentinel_When_Bot_Is_In_No_Chat()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroup>();
        client.Setup(c => c.GetChatGroupPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<ChatItemInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<ChatItemInfo> { HasMore = false, Items = [] },
            });

        var payload = Payload(await CreateTools(chatGroup: client).ListChatsAsync(Args(), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse("空结果必须给显式哨兵");
        payload["message"]!.GetValue<string>().Should().Contain("群机器人", "哨兵要给出「怎么让 chat_id 出现」的下一步");
    }

    // ─────────────────────────── chatgroup.list_moderators ───────────────────────────

    [Fact]
    public async Task ListModerators_Should_Project_Setting_And_Identifiers_Only()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroup>();
        client.Setup(c => c.GetChatGroupModeratorPageListByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ChatGroupModeratorPageListResult>
            {
                Code = 0,
                Data = new ChatGroupModeratorPageListResult
                {
                    ModerationSetting = "moderator_list",
                    HasMore = false,
                    Items = [new ModeratorItem { UserId = "ou_1", UserIdType = "open_id", TenantKey = "tk_1" }],
                },
            });

        var payload = Payload(await CreateTools(chatGroup: client).ListModeratorsAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        payload["moderation_setting"]!.GetValue<string>().Should().Be("moderator_list");
        var item = payload["items"]!.AsArray()[0]!.AsObject();
        item["user_id"]!.GetValue<string>().Should().Be("ou_1");
        item.ContainsKey("tenant_key").Should().BeFalse("组织标识不出站");
    }

    [Fact]
    public async Task ListModerators_Should_Explain_Empty_List()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroup>();
        client.Setup(c => c.GetChatGroupModeratorPageListByIdAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ChatGroupModeratorPageListResult>
            {
                Code = 0,
                Data = new ChatGroupModeratorPageListResult { ModerationSetting = "all", HasMore = false, Items = [] },
            });

        var payload = Payload(await CreateTools(chatGroup: client).ListModeratorsAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>().Should().Contain("全员可发言",
            "空名单 ≠ 没人能说话——哨兵必须消解这个歧义");
    }

    // ─────────────────────────── chatgroup.is_in_chat ───────────────────────────

    [Fact]
    public async Task IsInChat_Should_Explain_Next_Step_When_Not_In_Chat()
    {
        var member = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>();
        member.Setup(c => c.GetMemberInChatByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetMemberIsInChatResult>
            {
                Code = 0,
                Data = new GetMemberIsInChatResult { IsInChat = false },
            });

        var payload = Payload(await CreateTools(member: member).IsInChatAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        payload["is_in_chat"]!.GetValue<bool>().Should().BeFalse();
        payload["notice"]!.GetValue<string>().Should().Contain("群机器人",
            "预检的价值在下一步：不在群里要先把应用拉进去");
    }

    // ─────────────────────────── chatgroup.get_menu ───────────────────────────

    [Fact]
    public async Task GetMenu_Should_Build_Two_Level_Tree()
    {
        var menu = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMenu>();
        menu.Setup(c => c.GetMenuByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ChatGroupMenuResult>
            {
                Code = 0,
                Data = new ChatGroupMenuResult
                {
                    MenuTree = new ChatMenuTreeInfo
                    {
                        ChatMenuTopLevels =
                        [
                            new ChatMenuTopLevelInfo
                            {
                                ChatMenuTopLevelId = "m1",
                                ChatMenuItem = new ChatMenuItem
                                {
                                    Name = "帮助中心",
                                    ActionType = "redirect_link",
                                    RedirectLink = new ChatMenuItemRedirectLink { CommonUrl = "https://example/help" },
                                },
                                Childrens =
                                [
                                    new ChatMenuSecondLevelInfo
                                    {
                                        ChatMenuSecondLevelId = "m1-1",
                                        ChatMenuItem = new ChatMenuItem { Name = "常见问题", ActionType = "redirect_link" },
                                    },
                                ],
                            },
                        ],
                    },
                },
            });

        var payload = Payload(await CreateTools(menu: menu).GetMenuAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        var top = payload["items"]!.AsArray()[0]!.AsObject();
        top["chat_menu_top_level_id"]!.GetValue<string>().Should().Be("m1");
        top["name"]!.GetValue<string>().Should().Be("帮助中心");
        top["url"]!.GetValue<string>().Should().Be("https://example/help");
        var child = top["children"]!.AsArray()[0]!.AsObject();
        child["chat_menu_second_level_id"]!.GetValue<string>().Should().Be("m1-1");
        child["name"]!.GetValue<string>().Should().Be("常见问题");
    }

    [Fact]
    public async Task GetMenu_Should_Say_NotConfigured_When_Empty()
    {
        var menu = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMenu>();
        menu.Setup(c => c.GetMenuByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ChatGroupMenuResult> { Code = 0, Data = new ChatGroupMenuResult() });

        var payload = Payload(await CreateTools(menu: menu).GetMenuAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>().Should().Contain("未配置", "空菜单是配置缺失，不是失败");
    }

    // ─────────────────────────── 软缺席 ───────────────────────────

    [Fact]
    public async Task AllTools_Should_SoftlyDegrade_When_Clients_Are_Absent()
    {
        var tools = CreateTools();

        var results = new[]
        {
            await tools.ListChatsAsync(Args(), CancellationToken.None),
            await tools.ListModeratorsAsync(Args(("chat_id", "oc_1")), CancellationToken.None),
            await tools.IsInChatAsync(Args(("chat_id", "oc_1")), CancellationToken.None),
            await tools.GetMenuAsync(Args(("chat_id", "oc_1")), CancellationToken.None),
        };

        foreach (var result in results)
        {
            result.Text.Should().NotBeNull("软缺席必须给可执行说明，而不是抛异常");
            result.Text.Should().Contain("未注册", "文案要说清是宿主未启用该 API，而不是模型参数错");
            result.Text.Should().Contain("tool_error", "必须是结构化错误（第一行紧凑 JSON）");
        }
    }

    // ───────────────────── 写面（群设置 / 成员 / 管理员 / 置顶 / 菜单） ─────────────────────

    private static Mock<Mud.Feishu.IFeishuTenantV1ChatGroup> WriteChatGroupClient(
        Action? onCreate = null,
        Action? onUpdate = null,
        Action? onDelete = null,
        Action? onModeration = null,
        Action? onPutTopNotice = null,
        Action? onDeleteTopNotice = null,
        int nullDataCode = 0)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroup>();
        client.Setup(c => c.CreateChatGroupAsync(
                It.IsAny<CreateChatRequest>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateChatRequest _, string __, bool? ___, string? ____, CancellationToken _____) =>
            {
                onCreate?.Invoke();
                return new FeishuApiResult<CreateUpdateChatResult> { Code = 0, Data = new CreateUpdateChatResult { ChatId = "oc_new" } };
            });

        client.Setup(c => c.UpdateChatGroupByIdAsync(
                It.IsAny<string>(), It.IsAny<UpdateChatRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, UpdateChatRequest __, string ___, CancellationToken ____) =>
            {
                onUpdate?.Invoke();
                return new FeishuApiResult<CreateUpdateChatResult> { Code = 0, Data = new CreateUpdateChatResult { ChatId = "oc_1" } };
            });

        client.Setup(c => c.DeleteChatGroupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken __) =>
            {
                onDelete?.Invoke();
                return new FeishuNullDataApiResult { Code = nullDataCode, Msg = "ok" };
            });

        client.Setup(c => c.UpdateChatModerationAsync(
                It.IsAny<string>(), It.IsAny<UpdateChatModerationRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, UpdateChatModerationRequest __, string ___, CancellationToken ____) =>
            {
                onModeration?.Invoke();
                return new FeishuNullDataApiResult { Code = nullDataCode, Msg = "ok" };
            });

        client.Setup(c => c.PutChatGroupTopNoticeAsync(
                It.IsAny<string>(), It.IsAny<ChatTopNoticeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, ChatTopNoticeRequest __, CancellationToken ___) =>
            {
                onPutTopNotice?.Invoke();
                return new FeishuNullDataApiResult { Code = nullDataCode, Msg = "ok" };
            });

        client.Setup(c => c.DeleteChatGroupTopNoticeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken __) =>
            {
                onDeleteTopNotice?.Invoke();
                return new FeishuNullDataApiResult { Code = nullDataCode, Msg = "ok" };
            });

        return client;
    }

    private static Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember> WriteMemberClient(
        Action? onAdd = null,
        Action? onRemove = null,
        Action? onMeJoin = null,
        Action? onAddManager = null,
        Action? onDeleteManager = null,
        int nullDataCode = 0)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>();
        client.Setup(c => c.AddMemberAsync(
                It.IsAny<string>(), It.IsAny<MembersRequest>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, MembersRequest __, string ___, int ____, CancellationToken _____) =>
            {
                onAdd?.Invoke();
                return new FeishuApiResult<AddMemberResult>
                {
                    Code = 0,
                    Data = new AddMemberResult { InvalidIdList = ["ou_bad"], NotExistedIdList = [], PendingApprovalIdList = [] },
                };
            });

        client.Setup(c => c.RemoveMemberAsync(
                It.IsAny<string>(), It.IsAny<MembersRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, MembersRequest __, string ___, CancellationToken ____) =>
            {
                onRemove?.Invoke();
                return new FeishuApiResult<RemoveMemberResult> { Code = 0, Data = new RemoveMemberResult { InvalidIdList = [] } };
            });

        client.Setup(c => c.MeJoinChatGroupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CancellationToken __) =>
            {
                onMeJoin?.Invoke();
                return new FeishuNullDataApiResult { Code = nullDataCode, Msg = "ok" };
            });

        client.Setup(c => c.AddManagersAsync(
                It.IsAny<string>(), It.IsAny<GroupManagerRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, GroupManagerRequest __, string ___, CancellationToken ____) =>
            {
                onAddManager?.Invoke();
                return new FeishuApiResult<GroupManagerResult>
                {
                    Code = 0,
                    Data = new GroupManagerResult { ChatManagers = ["ou_a"], ChatBotManagers = [] },
                };
            });

        client.Setup(c => c.DeleteManagersAsync(
                It.IsAny<string>(), It.IsAny<GroupManagerRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, GroupManagerRequest __, string ___, CancellationToken ____) =>
            {
                onDeleteManager?.Invoke();
                return new FeishuApiResult<GroupManagerResult>
                {
                    Code = 0,
                    Data = new GroupManagerResult { ChatManagers = [], ChatBotManagers = [] },
                };
            });

        return client;
    }

    /// <summary>dry_run 必须零调用下游（写工具第一道防线）。</summary>
    [Fact]
    public async Task CreateChat_DryRun_Should_Not_Call_Downstream()
    {
        var calls = 0;
        var client = WriteChatGroupClient(onCreate: () => calls++);

        var result = await CreateTools(chatGroup: client).CreateChatAsync(
            Args(("name", "季度复盘"), ("user_id_list", new[] { "ou_a", "ou_b" }), ("dry_run", true)),
            CancellationToken.None);

        calls.Should().Be(0, "dry_run=true 不得调用下游");
        result.Text.Should().Contain("未调用下游");
        result.Text.Should().Contain("POST");
        result.Text.Should().Contain("user_id_list", "预演只报字段条数，不回原文 ID");
    }

    /// <summary>实发回执带 chat_id，且请求体按白名单合成。</summary>
    [Fact]
    public async Task CreateChat_Should_Project_ChatId()
    {
        CreateChatRequest? captured = null;
        var client = WriteChatGroupClient(onCreate: () => { });
        client.Setup(c => c.CreateChatGroupAsync(
                It.IsAny<CreateChatRequest>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateChatRequest request, string _, bool? __, string? ___, CancellationToken ____) =>
            {
                captured = request;
                return new FeishuApiResult<CreateUpdateChatResult> { Code = 0, Data = new CreateUpdateChatResult { ChatId = "oc_new" } };
            });

        var payload = Payload(await CreateTools(chatGroup: client).CreateChatAsync(
            Args(("name", "季度复盘"), ("user_id_list", new[] { "ou_a" }), ("chat_type", "public")),
            CancellationToken.None));

        captured!.Name.Should().Be("季度复盘");
        captured.UserIdList.Should().Equal(["ou_a"]);
        captured.ChatType.Should().Be("public");

        payload["chat_id"]!.GetValue<string>().Should().Be("oc_new");
        payload["created"]!.GetValue<bool>().Should().BeTrue();
    }

    /// <summary>update_chat 一个字段都不传必须被拒（空更新没有意义）。</summary>
    [Fact]
    public async Task UpdateChat_With_No_Fields_Should_Reject()
    {
        var calls = 0;
        var client = WriteChatGroupClient(onUpdate: () => calls++);

        var result = await CreateTools(chatGroup: client).UpdateChatAsync(
            Args(("chat_id", "oc_1"), ("dry_run", true)), CancellationToken.None);

        calls.Should().Be(0);
        result.Text.Should().Contain("tool_error");
        result.Text.Should().Contain("至少传一个", "错误信息要点名缺的是什么");
    }

    /// <summary>update_chat 实发：回执给改动字段数 + 增量语义提醒。</summary>
    [Fact]
    public async Task UpdateChat_Should_Project_Incremental_Semantics()
    {
        var client = WriteChatGroupClient();

        var payload = Payload(await CreateTools(chatGroup: client).UpdateChatAsync(
            Args(("chat_id", "oc_1"), ("name", "新群名"), ("edit_permission", "only_owner")),
            CancellationToken.None));

        payload["updated_fields"]!.GetValue<int>().Should().Be(2);
        payload["updated"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("增量更新", "必须提醒未传字段保持原值");
    }

    /// <summary>解散群（高危）：两段式——先 dry_run 零调用，再实发回执。</summary>
    [Fact]
    public async Task DeleteChat_DryRun_Then_Real_Should_Return_Receipt()
    {
        var calls = 0;
        var client = WriteChatGroupClient(onDelete: () => calls++);

        var preview = await CreateTools(chatGroup: client).DeleteChatAsync(
            Args(("chat_id", "oc_1"), ("dry_run", true)), CancellationToken.None);

        calls.Should().Be(0);
        preview.Text.Should().Contain("未调用下游");
        preview.Text.Should().Contain("DELETE");

        var payload = Payload(await CreateTools(chatGroup: client).DeleteChatAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        calls.Should().Be(1);
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("不可逆");
    }

    /// <summary>moderator_list 模式不给名单 = 全群静音，必须在 dry_run 之前拦下。</summary>
    [Fact]
    public async Task UpdateModeration_ModeratorList_Without_Names_Should_Reject_Before_DryRun()
    {
        var calls = 0;
        var client = WriteChatGroupClient(onModeration: () => calls++);

        var result = await CreateTools(chatGroup: client).UpdateModerationAsync(
            Args(("chat_id", "oc_1"), ("moderation_setting", "moderator_list"), ("dry_run", true)),
            CancellationToken.None);

        calls.Should().Be(0, "参数非法时连预演都不放行");
        result.Text.Should().Contain("tool_error");
        result.Text.Should().Contain("静音", "错误信息要说清后果");
    }

    /// <summary>add_member 实发：无效 ID 清单必须带回（否则模型会以为全员拉入）。</summary>
    [Fact]
    public async Task AddMember_Should_Project_Invalid_Id_List()
    {
        var client = WriteMemberClient();

        var payload = Payload(await CreateTools(member: client).AddMemberAsync(
            Args(("chat_id", "oc_1"), ("id_list", new[] { "ou_a", "ou_bad" })),
            CancellationToken.None));

        payload["invalid_id_list"]!.AsArray().Count.Should().Be(1);
        payload["invalid_id_list"]!.AsArray()[0]!.GetValue<string>().Should().Be("ou_bad");
        payload["added"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("不要当成全员拉入");
    }

    /// <summary>移出成员（高危）：dry_run 零调用 + 实发回执带"静默移出"提醒。</summary>
    [Fact]
    public async Task RemoveMember_DryRun_Then_Real_Should_Return_Receipt()
    {
        var calls = 0;
        var client = WriteMemberClient(onRemove: () => calls++);

        var preview = await CreateTools(member: client).RemoveMemberAsync(
            Args(("chat_id", "oc_1"), ("id_list", new[] { "ou_a" }), ("dry_run", true)), CancellationToken.None);

        calls.Should().Be(0);
        preview.Text.Should().Contain("未调用下游");

        var payload = Payload(await CreateTools(member: client).RemoveMemberAsync(
            Args(("chat_id", "oc_1"), ("id_list", new[] { "ou_a" })), CancellationToken.None));

        calls.Should().Be(1);
        payload["removed"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("不会", "必须复述「静默移出」这一后果");
    }

    /// <summary>me_join_chat：加的是当前身份，回执要给 joined。</summary>
    [Fact]
    public async Task MeJoinChat_Should_Project_Joined()
    {
        var calls = 0;
        var client = WriteMemberClient(onMeJoin: () => calls++);

        var payload = Payload(await CreateTools(member: client).MeJoinChatAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        calls.Should().Be(1);
        payload["joined"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("当前应用/机器人身份");
    }

    /// <summary>add/delete_manager：回执带两份管理员名单。</summary>
    [Fact]
    public async Task Managers_Should_Project_Both_Manager_Lists()
    {
        var addClient = WriteMemberClient();
        var addPayload = Payload(await CreateTools(member: addClient).AddManagerAsync(
            Args(("chat_id", "oc_1"), ("manager_ids", new[] { "ou_a" })), CancellationToken.None));

        addPayload["chat_managers"]!.AsArray().Count.Should().Be(1);
        addPayload["added"]!.GetValue<bool>().Should().BeTrue();

        var deleteClient = WriteMemberClient();
        var deletePreview = await CreateTools(member: deleteClient).DeleteManagerAsync(
            Args(("chat_id", "oc_1"), ("manager_ids", new[] { "ou_a" }), ("dry_run", true)), CancellationToken.None);

        deletePreview.Text.Should().Contain("未调用下游", "高危写先 dry_run");

        var deletePayload = Payload(await CreateTools(member: deleteClient).DeleteManagerAsync(
            Args(("chat_id", "oc_1"), ("manager_ids", new[] { "ou_a" })), CancellationToken.None));

        deletePayload["deleted"]!.GetValue<bool>().Should().BeTrue();
        deletePayload["notice"]!.GetValue<string>().Should().Contain("不会收到通知");
    }

    /// <summary>put_top_notice：action_type=1 不给 message_id 是必然失败的请求，必须拦。</summary>
    [Fact]
    public async Task PutTopNotice_ActionType1_Without_MessageId_Should_Reject()
    {
        var calls = 0;
        var client = WriteChatGroupClient(onPutTopNotice: () => calls++);

        var result = await CreateTools(chatGroup: client).PutTopNoticeAsync(
            Args(("chat_id", "oc_1"), ("action_type", "1")), CancellationToken.None);

        calls.Should().Be(0);
        result.Text.Should().Contain("tool_error");
        result.Text.Should().Contain("message_id");
    }

    /// <summary>put_top_notice 实发：回执带 action_type 与 message_id。</summary>
    [Fact]
    public async Task PutTopNotice_Should_Project_ActionType()
    {
        var client = WriteChatGroupClient();

        var payload = Payload(await CreateTools(chatGroup: client).PutTopNoticeAsync(
            Args(("chat_id", "oc_1"), ("action_type", "1"), ("message_id", "om_1")),
            CancellationToken.None));

        payload["action_type"]!.GetValue<string>().Should().Be("1");
        payload["message_id"]!.GetValue<string>().Should().Be("om_1");
        payload["updated"]!.GetValue<bool>().Should().BeTrue();
    }

    /// <summary>delete_top_notice（高危）：两段式 + 消息不丢失的提醒。</summary>
    [Fact]
    public async Task DeleteTopNotice_DryRun_Then_Real_Should_Return_Receipt()
    {
        var calls = 0;
        var client = WriteChatGroupClient(onDeleteTopNotice: () => calls++);

        var preview = await CreateTools(chatGroup: client).DeleteTopNoticeAsync(
            Args(("chat_id", "oc_1"), ("dry_run", true)), CancellationToken.None);

        calls.Should().Be(0);
        preview.Text.Should().Contain("未调用下游");

        var payload = Payload(await CreateTools(chatGroup: client).DeleteTopNoticeAsync(
            Args(("chat_id", "oc_1")), CancellationToken.None));

        calls.Should().Be(1);
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("不丢失");
    }

    /// <summary>delete_menu（高危）：回执给删除条数与剩余菜单数。</summary>
    [Fact]
    public async Task DeleteMenu_Should_Project_Menu_Counts()
    {
        var menuClient = new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMenu>();
        menuClient.Setup(c => c.DeleteMenuByIdAsync(
                It.IsAny<string>(), It.IsAny<ChartMenuIdsRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ChatGroupMenuResult>
            {
                Code = 0,
                Data = new ChatGroupMenuResult { MenuTree = new ChatMenuTreeInfo { ChatMenuTopLevels = [] } },
            });

        var preview = await CreateTools(menu: menuClient).DeleteMenuAsync(
            Args(("chat_id", "oc_1"), ("chat_menu_top_level_ids", new[] { "m1" }), ("dry_run", true)),
            CancellationToken.None);

        preview.Text.Should().Contain("未调用下游", "高危写先 dry_run");

        var payload = Payload(await CreateTools(menu: menuClient).DeleteMenuAsync(
            Args(("chat_id", "oc_1"), ("chat_menu_top_level_ids", new[] { "m1" })), CancellationToken.None));

        payload["deleted_menu_count"]!.GetValue<int>().Should().Be(1);
        payload["remaining_menu_count"]!.GetValue<int>().Should().Be(0);
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
    }

    /// <summary>写面三个客户端各自软缺席：必须给结构化错误，不得抛异常。</summary>
    [Fact]
    public async Task Write_Face_Missing_Clients_Should_Return_Structured_Error()
    {
        var chatGroup = CreateTools();
        foreach (var result in new[]
        {
            await chatGroup.CreateChatAsync(Args(("name", "x")), CancellationToken.None),
            await chatGroup.UpdateChatAsync(Args(("chat_id", "oc_1"), ("name", "x")), CancellationToken.None),
            await chatGroup.DeleteChatAsync(Args(("chat_id", "oc_1")), CancellationToken.None),
            await chatGroup.UpdateModerationAsync(Args(("chat_id", "oc_1"), ("moderation_setting", "all_members")), CancellationToken.None),
            await chatGroup.PutTopNoticeAsync(Args(("chat_id", "oc_1"), ("action_type", "2")), CancellationToken.None),
            await chatGroup.DeleteTopNoticeAsync(Args(("chat_id", "oc_1")), CancellationToken.None),
            await chatGroup.AddMemberAsync(Args(("chat_id", "oc_1"), ("id_list", new[] { "ou_a" })), CancellationToken.None),
            await chatGroup.RemoveMemberAsync(Args(("chat_id", "oc_1"), ("id_list", new[] { "ou_a" })), CancellationToken.None),
            await chatGroup.MeJoinChatAsync(Args(("chat_id", "oc_1")), CancellationToken.None),
            await chatGroup.AddManagerAsync(Args(("chat_id", "oc_1"), ("manager_ids", new[] { "ou_a" })), CancellationToken.None),
            await chatGroup.DeleteManagerAsync(Args(("chat_id", "oc_1"), ("manager_ids", new[] { "ou_a" })), CancellationToken.None),
            await chatGroup.DeleteMenuAsync(Args(("chat_id", "oc_1"), ("chat_menu_top_level_ids", new[] { "m1" })), CancellationToken.None),
        })
        {
            result.Text.Should().Contain("未注册", "写面软缺席同样要给可执行说明");
            result.Text.Should().Contain("tool_error");
        }
    }
}
