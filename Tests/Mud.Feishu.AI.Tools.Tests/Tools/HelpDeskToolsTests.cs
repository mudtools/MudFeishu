// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// HelpDesk 服务台域级测试（专题一 B1 第三个补域，SOP 第 8 步）。
/// </summary>
/// <remarks>
/// <para>
/// 本域的核心断言是<b>两档 PII 边界</b>：
/// <list type="bullet">
/// <item>FAQ：给内容，但<b>不给作者身份</b>（<c>create_user</c>/<c>update_user</c>）；</item>
/// <item>工单：给状态与流转，<b>不给正文与客户画像</b>（备注正文、对话、邮箱 / 部门 / 城市 / 国家）。</item>
/// </list>
/// 与 Application 域同款做法：用"脏数据"（把敏感字段全填上）来证明投影确实丢掉了它们——
/// 只测"正常数据能出字段面"是测不出泄露的。
/// </para>
/// <para>
/// policy 拒绝零调用与审计三类投递由 <c>ToolPolicyContractGuards</c> /
/// <c>FeishuToolBindingAuditTests</c> 通用覆盖，本类不重复。
/// </para>
/// </remarks>
public class HelpDeskToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static HelpdeskTools CreateTools(
        Mock<Mud.Feishu.IFeishuTenantV1HelpDeskFaq>? faq = null,
        Mock<Mud.Feishu.IFeishuTenantV1HelpDeskCategory>? category = null,
        Mock<Mud.Feishu.IFeishuTenantV1HelpDeskTicket>? ticket = null,
        Mock<Mud.Feishu.IFeishuTenantV1HelpDeskAgent>? agent = null)
        => new(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            faq?.Object,
            category?.Object,
            ticket?.Object,
            agent?.Object);

    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static FeishuApiResult<T> Ok<T>(T data)
        where T : class
        => new() { Code = 0, Data = data };

    /// <summary>带"脏数据"的 FAQ：作者身份全部填上，用于验证投影丢弃它们。</summary>
    private static Faq DirtyFaq(string faqId, string question)
        => new()
        {
            FaqId = faqId,
            Id = faqId,
            Question = question,
            Answer = "先登录管理后台 → 财务 → 发票管理 → 申请开票。",
            Content = "正文",
            Type = "1",
            CreateTime = 1760000000000,
            UpdateTime = 1760000001000,
            Categories = [new Category { CategoryId = "c1", Name = "财务" }],
            Tags = ["发票"],
            CreateUser = new TicketUser { Id = "ou_author", Name = "张三", Email = "author@example.com" },
            UpdateUser = new TicketUser { Id = "ou_editor", Name = "李四", Email = "editor@example.com" },
        };

    /// <summary>带"脏数据"的工单：客户画像与备注正文全部填上。</summary>
    private static Ticket DirtyTicket()
        => new()
        {
            TicketId = "694123",
            Status = 2,
            TicketType = 1,
            Channel = 1,
            Score = 5,
            Solve = 1,
            CreatedAt = 1760000000000,
            UpdatedAt = 1760000100000,
            ClosedAt = 1760000200000,
            Tags = [new TicketTag { Id = "t1", Name = "退款" }],
            Agents = [new TicketUser { Id = "ou_agent", Name = "客服小王", Email = "agent@example.com" }],
            Guest = new TicketUser
            {
                Id = "ou_guest",
                Name = "客户老王",
                Email = "guest@example.com",
                Department = "研发中心",
                City = "北京",
                Country = "中国",
            },
            Comments = new TicketComments
            {
                Content = "内部备注：客户要求走绿色通道",
                UserName = "客服小王",

                // 备注人的 ID 在平台侧是**数字 user_id**（不是 open_id）——本工具不返回它，
                // 这里填上只为证明"即便下游给了，出站也没有"。
                UserId = 123456789,
                CreatedAt = 1760000150000,
            },
            AgentServiceDuration = 12.5,
            AgentFirstResponseDuration = 30,
        };

    // ─────────────────────────── FAQ ───────────────────────────

    [Fact]
    public async Task SearchFaqs_Should_Project_Summary_Without_Authors()
    {
        var faq = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskFaq>();
        faq.Setup(c => c.SearchFaqAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new SearchFaqResult
            {
                Items = [DirtyFaq("6941", "发票怎么开")],
                HasMore = false,
                Total = 1,
            }));

        var payload = Payload(await CreateTools(faq: faq).SearchFaqsAsync(
            Args(("query", "发票")), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!.AsObject();
        item["faq_id"]!.GetValue<string>().Should().Be("6941");
        item["question"]!.GetValue<string>().Should().Be("发票怎么开");
        item["answer"]!.GetValue<string>().Should().Contain("申请开票");
        item["categories"]!.AsArray()[0]!.GetValue<string>().Should().Be("财务");
        item["tags"]!.AsArray()[0]!.GetValue<string>().Should().Be("发票");

        AssertNoPersonFields(item, "FAQ 不得返回作者 / 更新人身份");
    }

    [Fact]
    public async Task SearchFaqs_FetchAll_Should_Aggregate_Pages()
    {
        var calls = 0;
        var faq = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskFaq>();
        faq.Setup(c => c.SearchFaqAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string? __, string? pageToken, int? ___, CancellationToken _) =>
            {
                calls++;
                return Ok(new SearchFaqResult
                {
                    Items = [DirtyFaq(pageToken is null ? "6941" : "6942", "问题")],
                    HasMore = pageToken is null,
                    PageToken = pageToken is null ? "t2" : null,
                });
            });

        var payload = Payload(await CreateTools(faq: faq).SearchFaqsAsync(
            Args(("query", "发票"), ("fetch_all", true)), CancellationToken.None));

        calls.Should().Be(2, "页状态显式重载：has_more=true 必须继续翻页");
        payload["items"]!.AsArray().Count.Should().Be(2);
        payload["truncated"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task ListFaqs_Should_Return_Executable_Sentinel_When_Empty()
    {
        var faq = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskFaq>();
        faq.Setup(c => c.GetFaqListAsync(
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetFaqListResult { Items = [], HasMore = false }));

        var payload = Payload(await CreateTools(faq: faq).ListFaqsAsync(Args(), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse("空结果必须给显式哨兵");
        payload["message"]!.GetValue<string>().Should().Contain("list_categories", "哨兵要指向可执行的下一步");
    }

    [Fact]
    public async Task GetFaq_Should_Return_Detail_And_Truncate_Content()
    {
        var dirty = DirtyFaq("6941", "发票怎么开");
        dirty.Content = new string('正', 4000);
        var faq = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskFaq>();
        faq.Setup(c => c.GetFaqAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetFaqResult { Faq = dirty }));

        var payload = Payload(await CreateTools(faq: faq).GetFaqAsync(
            Args(("faq_id", "6941")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["answer"]!.GetValue<string>().Should().Contain("申请开票", "详情给全文答案");
        payload["content"]!.GetValue<string>().Length.Should().BeLessThan(4000,
            "正文字段必须预截断（ToolExecutorSkeletonGuards 机械拦截超长消息体）");
        AssertNoPersonFields(payload, "FAQ 详情同样不得返回作者身份");
    }

    [Fact]
    public async Task GetFaq_Should_Return_Executable_Sentinel_When_NotFound()
    {
        var faq = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskFaq>();
        faq.Setup(c => c.GetFaqAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetFaqResult { Faq = null }));

        var payload = Payload(await CreateTools(faq: faq).GetFaqAsync(
            Args(("faq_id", "nope")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["faq_id"]!.GetValue<string>().Should().Be("nope");
        payload["message"]!.GetValue<string>().Should().Contain("search_faqs");
    }

    [Fact]
    public async Task ListCategories_Should_Build_Tree()
    {
        var category = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskCategory>();
        category.Setup(c => c.GetCategoryListAsync(
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<bool?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetCategoryListResult
            {
                Categories =
                [
                    new Category
                    {
                        CategoryId = "c1",
                        Name = "财务",
                        ParentId = "0",
                        Children = [new Category { CategoryId = "c2", Name = "发票", ParentId = "c1" }],
                    },
                ],
            }));

        var payload = Payload(await CreateTools(category: category).ListCategoriesAsync(
            Args(), CancellationToken.None));

        var root = payload["items"]!.AsArray()[0]!.AsObject();
        root["category_id"]!.GetValue<string>().Should().Be("c1");
        root["children"]!.AsArray()[0]!["name"]!.GetValue<string>().Should().Be("发票");
    }

    // ─────────────────────────── 工单（PII 边界） ───────────────────────────

    [Fact]
    public async Task GetTicket_Should_Return_Status_Only_Without_Content_Or_Contact()
    {
        var ticket = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskTicket>();
        ticket.Setup(c => c.GetTicketAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetTicketResult { Ticket = DirtyTicket() }));

        var payload = Payload(await CreateTools(ticket: ticket).GetTicketAsync(
            Args(("ticket_id", "694123")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["ticket_id"]!.GetValue<string>().Should().Be("694123");
        payload["status"]!.GetValue<int>().Should().Be(2);
        payload["tags"]!.AsArray()[0]!["name"]!.GetValue<string>().Should().Be("退款");
        payload["guest"]!["name"]!.GetValue<string>().Should().Be("客户老王", "姓名是既有口径（如 contact 域）");
        payload["has_comments"]!.GetValue<bool>().Should().BeTrue(
            "不给正文，但要告诉模型「还有备注没给」，否则它会以为这就是全部工单信息");

        // 出站净化：正文与客户画像一律不出站。
        var text = payload.ToJsonString();
        text.Should().NotContain("内部备注", "备注正文属工单内容，未纳入工具面");
        text.Should().NotContain("guest@example.com").And.NotContain("agent@example.com");
        text.Should().NotContain("研发中心").And.NotContain("北京");
        payload.ContainsKey("comments").Should().BeFalse();
        payload.ContainsKey("customized_fields").Should().BeFalse();
        payload.ContainsKey("dissatisfaction_reason").Should().BeFalse();

        // 相关人只留标识与姓名（各 2 个键）。
        payload["guest"]!.AsObject().Count.Should().Be(2);
        payload["agents"]!.AsArray()[0]!.AsObject().Count.Should().Be(2);
    }

    [Fact]
    public async Task ListAgentSkills_Should_Project_Config_Only()
    {
        var agent = new Mock<Mud.Feishu.IFeishuTenantV1HelpDeskAgent>();
        agent.Setup(c => c.GetAgentSkillListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetAgentSkillListResult
            {
                AgentSkills =
                [
                    new AgentSkillInfo { Id = "s1", Name = "财务咨询", IsDefault = true, AgentIds = ["ou_1", "ou_2"] },
                ],
            }));

        var payload = Payload(await CreateTools(agent: agent).ListAgentSkillsAsync(
            Args(), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!.AsObject();
        item["skill_id"]!.GetValue<string>().Should().Be("s1");
        item["name"]!.GetValue<string>().Should().Be("财务咨询");
        item["is_default"]!.GetValue<bool>().Should().BeTrue();
        item.ContainsKey("agent_ids").Should().BeFalse("技能组回答的是「有哪些组」，成员名单是多余的身份面");
    }

    // ─────────────────────────── 软缺席 ───────────────────────────

    [Fact]
    public async Task AllTools_Should_SoftlyDegrade_When_Clients_Are_Absent()
    {
        var tools = CreateTools();

        var results = new[]
        {
            await tools.SearchFaqsAsync(Args(("query", "发票")), CancellationToken.None),
            await tools.ListFaqsAsync(Args(), CancellationToken.None),
            await tools.GetFaqAsync(Args(("faq_id", "6941")), CancellationToken.None),
            await tools.ListCategoriesAsync(Args(), CancellationToken.None),
            await tools.GetTicketAsync(Args(("ticket_id", "694123")), CancellationToken.None),
            await tools.ListAgentSkillsAsync(Args(), CancellationToken.None),
        };

        foreach (var result in results)
        {
            result.Text.Should().NotBeNull("软缺席必须给可执行说明，而不是抛异常");
            result.Text.Should().Contain("未注册", "文案要说清是宿主未启用该 API，而不是模型参数错");
            result.Text.Should().Contain("tool_error", "必须是结构化错误（第一行紧凑 JSON）");
        }
    }

    /// <summary>出站净化的公共断言：FAQ 的作者 / 更新人身份不得出现。</summary>
    private static void AssertNoPersonFields(JsonObject payload, string because)
    {
        payload.ContainsKey("create_user").Should().BeFalse(because);
        payload.ContainsKey("update_user").Should().BeFalse(because);

        var text = payload.ToJsonString();
        text.Should().NotContain("author@example.com").And.NotContain("editor@example.com").And.NotContain("ou_author");
    }
}
