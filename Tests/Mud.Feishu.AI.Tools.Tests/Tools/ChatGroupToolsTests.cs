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
}
