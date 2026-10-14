// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.HelpDesk;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Helpdesk「飞书服务台」只读工具执行器（B1 第三个补域：6 个只读工具）。
/// </summary>
/// <remarks>
/// <para>
/// <b>软依赖</b>：四个客户端全部可空注入——宿主未启用服务台 API 时本执行器软缺席
/// （执行期给可执行的错误说明），不影响其它域工具。
/// </para>
/// <para>
/// <b>PII 纪律（本域的核心设计约束）</b>：服务台的 PII 集中在"工单内容"——
/// 备注正文、工单消息对话、客户自定义字段值，以及客户 / 客服的<b>邮箱 · 部门 · 城市 · 国家</b>。
/// 本执行器的投影分两档：
/// <list type="bullet">
/// <item><b>FAQ / 分类 / 技能组</b>：给全（问题、答案、富文本片段、目录、技能名），
/// 但裁掉 FAQ 的 <c>create_user</c>/<c>update_user</c>（员工身份）；</item>
/// <item><b>工单</b>：只给状态与流转事实（状态 / 类型 / 渠道 / 评分 / 时间线 / 标签 /
/// 处理时长 / 相关人<b>标识与姓名</b>），<b>不返回</b>正文与联系方式——
/// 姓名在本仓库的工具面里是既有口径（<c>contact.list_department_members</c> 就返回 name），
/// 而邮箱 / 部门 / 城市 / 国家属客户画像，不进模型上下文。</item>
/// </list>
/// </para>
/// <para>
/// <b>分页</b>：两个 FAQ 列表工具首批即接统一出口——Helpdesk 的结果类型<b>都不继承</b>
/// <c>ApiPageListResult</c>，故一律走"页状态显式"重载（读各自的 <c>HasMore</c>/<c>PageToken</c>）。
/// </para>
/// <para>
/// <b>命名陷阱（实测踩过一次）</b>：工具名前缀 <c>helpdesk</c> 在生成器里被规范化为
/// <b><c>Helpdesk</c></b>（只首字母大写，不是 <c>HelpDesk</c>）——所以生成的常量是
/// <c>FeishuToolNames.HelpdeskSearchFaqs</c>、参数类是 <c>HelpdeskSearchFaqsArgs</c>、
/// DI Core 入口是 <c>AddFeishuHelpdeskToolsCore</c>；而 <b>SDK 接口名</b>仍是
/// <c>IFeishuTenantV1HelpDeskFaq</c>（SDK 侧按专有名词大写）。两套命名都要按各自口径写。
/// </para>
/// </remarks>
internal sealed class HelpdeskTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1HelpDeskFaq? faqClient = null,
    Mud.Feishu.IFeishuTenantV1HelpDeskCategory? categoryClient = null,
    Mud.Feishu.IFeishuTenantV1HelpDeskTicket? ticketClient = null,
    Mud.Feishu.IFeishuTenantV1HelpDeskAgent? agentClient = null)
{
    /// <summary>FAQ 答案与工单字段的预览截断长度（摘要够用；要全文用对应的 get_* 工具）。</summary>
    private const int AnswerPreviewLength = 300;

    /// <summary>客户端缺席时的统一说明。</summary>
    private const string ClientName = "IFeishuTenantV1HelpDesk*（服务台 API）";

    private readonly Mud.Feishu.IFeishuTenantV1HelpDeskFaq? _faqClient = faqClient;
    private readonly Mud.Feishu.IFeishuTenantV1HelpDeskCategory? _categoryClient = categoryClient;
    private readonly Mud.Feishu.IFeishuTenantV1HelpDeskTicket? _ticketClient = ticketClient;
    private readonly Mud.Feishu.IFeishuTenantV1HelpDeskAgent? _agentClient = agentClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>统一分页语义（专题四）：<c>fetch_all</c> 的条目 / 页数预算。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    // ─────────────────────────── helpdesk.search_faqs ───────────────────────────

    /// <summary>helpdesk.search_faqs：关键词检索服务台 FAQ（本域主力）。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskSearchFaqsTool))]
    public Task<ToolResult> SearchFaqsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskSearchFaqs, _maxResultLength);
        if (Absent(executor.ToolName, _faqClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskSearchFaqsArgs.Unpack(arguments);

            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
            async Task<ToolApiOutcome<SearchFaqResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _faqClient!
                    .SearchFaqAsync(args.Query, page_token: token, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                result => FaqEnvelope(result.HasMore, result.PageToken, result.Total, result.Items),
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    // ─────────────────────────── helpdesk.list_faqs ───────────────────────────

    /// <summary>helpdesk.list_faqs：浏览服务台 FAQ（分页）。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskListFaqsTool))]
    public Task<ToolResult> ListFaqsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskListFaqs, _maxResultLength);
        if (Absent(executor.ToolName, _faqClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskListFaqsArgs.Unpack(arguments);

            async Task<ToolApiOutcome<GetFaqListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _faqClient!
                    .GetFaqListAsync(
                        category_id: args.CategoryId,
                        status: args.Status,
                        search: args.Search,
                        page_token: token,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                result => FaqEnvelope(result.HasMore, result.PageToken, result.Total, result.Items),
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    // ─────────────────────────── helpdesk.get_faq ───────────────────────────

    /// <summary>helpdesk.get_faq：查单条 FAQ 全文。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskGetFaqTool))]
    public Task<ToolResult> GetFaqAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskGetFaq, _maxResultLength);
        if (Absent(executor.ToolName, _faqClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskGetFaqArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _faqClient!
                .GetFaqAsync(args.FaqId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.Faq is null
                ? new JsonObject
                {
                    // 空结果哨兵（F-5）：把"ID 不对"与"没权限/服务台未启用"分开讲。
                    ["found"] = false,
                    ["faq_id"] = args.FaqId,
                    ["message"] = "未查询到该 FAQ。请确认 faq_id 来自 helpdesk.search_faqs / helpdesk.list_faqs 的返回；"
                        + "若本域整体无返回，多为服务台未启用，或当前身份缺 helpdesk:faq:readonly（该权限常见要求「仅自建应用」）。",
                }
                : FaqDetail(result.Faq));
        });
    }

    // ─────────────────────────── helpdesk.list_categories ───────────────────────────

    /// <summary>helpdesk.list_categories：列出 FAQ 分类目录树。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskListCategoriesTool))]
    public Task<ToolResult> ListCategoriesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskListCategories, _maxResultLength);
        if (Absent(executor.ToolName, _categoryClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskListCategoriesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _categoryClient!
                .GetCategoryListAsync(lang: args.Lang, cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result =>
            {
                var tree = CategoryTree(result.Categories);
                var payload = new JsonObject { ["items"] = tree };

                if (tree.Count == 0)
                {
                    payload["found"] = false;
                    payload["message"] = "服务台没有可返回的 FAQ 分类——通常是尚未配置 FAQ 知识库，或当前身份无权读取。";
                }

                return payload;
            });
        });
    }

    // ─────────────────────────── helpdesk.get_ticket ───────────────────────────

    /// <summary>helpdesk.get_ticket：查工单状态与流转（不含工单正文）。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskGetTicketTool))]
    public Task<ToolResult> GetTicketAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskGetTicket, _maxResultLength);
        if (Absent(executor.ToolName, _ticketClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskGetTicketArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _ticketClient!
                .GetTicketAsync(args.TicketId, cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.Ticket is null
                ? new JsonObject
                {
                    ["found"] = false,
                    ["ticket_id"] = args.TicketId,
                    ["message"] = "未查询到该工单。请确认 ticket_id 正确（通常来自服务台或事件回调），"
                        + "并确认当前身份有 helpdesk:ticket:readonly。",
                }
                : Ticket(result.Ticket));
        });
    }

    // ─────────────────────────── helpdesk.list_agent_skills ───────────────────────────

    /// <summary>helpdesk.list_agent_skills：列出客服技能组（路由配置）。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskListAgentSkillsTool))]
    public Task<ToolResult> ListAgentSkillsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskListAgentSkills, _maxResultLength);
        if (Absent(executor.ToolName, _agentClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var outcome = FeishuApiResultReader.Read(await _agentClient!
                .GetAgentSkillListAsync(cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result =>
            {
                var items = new JsonArray();
                foreach (var skill in result.AgentSkills ?? [])
                {
                    items.AddNode(new JsonObject
                    {
                        ["skill_id"] = skill.Id,
                        ["name"] = skill.Name,
                        ["is_default"] = skill.IsDefault,
                    });
                }

                var payload = new JsonObject { ["items"] = items };

                if (items.Count == 0)
                {
                    payload["found"] = false;
                    payload["message"] = "服务台没有配置客服技能（技能组）——此时工单不会被按技能路由。";
                }

                return payload;
            });
        });
    }

    // ─────────────────────────── 内部工具 ───────────────────────────

    /// <summary>FAQ 列表/检索共用的信封与条目投影（两类结果 DTO 结构一致，故共用一份投影）。</summary>
    private static JsonObject FaqEnvelope(bool? hasMore, string? pageToken, int? total, Faq[]? items)
    {
        var envelope = ToolResultJsons.PageEnvelope(hasMore, pageToken);
        var array = envelope["items"]!.AsArray();
        foreach (var faq in items ?? [])
        {
            array.AddNode(FaqSummary(faq));
        }

        envelope["total"] = total;

        if (array.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "没有匹配的 FAQ。建议：换更短的关键词、去掉 category_id 限制，"
                + "或先 helpdesk.list_categories 看服务台是否有对应分类（空知识库也很常见）。";
        }

        return envelope;
    }

    /// <summary>FAQ 摘要：问题 + 答案预览 + 分类 / 标签（**不含** create_user / update_user）。</summary>
    private static JsonObject FaqSummary(Faq faq)
        => new()
        {
            ["faq_id"] = faq.FaqId ?? faq.Id,
            ["question"] = faq.Question,
            ["answer"] = ToolResultText.Truncate(faq.Answer, AnswerPreviewLength),
            ["categories"] = CategoryNames(faq.Categories),
            ["tags"] = StringArray(faq.Tags),

            // 单位进键名（毫秒时间戳）——不然模型只能猜是秒还是毫秒。
            ["create_time_ms"] = faq.CreateTime,
            ["update_time_ms"] = faq.UpdateTime,
        };

    /// <summary>FAQ 详情：摘要 + 全文 + 富文本片段（作者身份仍不出站）。</summary>
    private static JsonObject FaqDetail(Faq faq)
    {
        var payload = FaqSummary(faq);
        payload["found"] = true;
        payload["answer"] = faq.Answer;

        // 正文字段必须预截断（ToolExecutorSkeletonGuards 机械拦截）：超长消息体挤占上下文。
        payload["content"] = ToolResultText.Truncate(faq.Content, PageSizes.MessagePreviewLength);
        payload["type"] = faq.Type;
        payload["expire_time_ms"] = faq.ExpireTime;
        payload["answer_richtext"] = FaqRichtext(faq.AnswerRichtext);
        return payload;
    }

    /// <summary>富文本片段投影：类型 + 文本 + 链接（<c>src</c>/<c>key</c> 是素材定位信息，对模型无决策价值）。</summary>
    private static JsonArray FaqRichtext(FaqRichtext[]? fragments)
    {
        var array = new JsonArray();
        foreach (var fragment in fragments ?? [])
        {
            array.AddNode(new JsonObject
            {
                ["type"] = fragment.Type,
                ["text"] = fragment.Text,
                ["url"] = fragment.Url,
            });
        }

        return array;
    }

    /// <summary>工单投影：<b>只给状态与流转事实</b>（正文、自定义字段值、联系方式一律不出站）。</summary>
    private static JsonObject Ticket(Ticket ticket)
        => new()
        {
            ["found"] = true,
            ["ticket_id"] = ticket.TicketId,
            ["status"] = ticket.Status,
            ["ticket_type"] = ticket.TicketType,
            ["channel"] = ticket.Channel,
            ["score"] = ticket.Score,
            ["solve"] = ticket.Solve,
            ["created_at_ms"] = ticket.CreatedAt,
            ["updated_at_ms"] = ticket.UpdatedAt,
            ["closed_at_ms"] = ticket.ClosedAt,
            ["tags"] = TicketTags(ticket.Tags),
            ["guest"] = Person(ticket.Guest),
            ["agents"] = People(ticket.Agents),
            ["closed_by"] = Person(ticket.ClosedBy),
            ["agent_owner"] = Person(ticket.AgentOwner),
            ["collaborator_count"] = ticket.Collaborators?.Length ?? 0,

            // 不给正文，但让模型知道"还有多少信息没给"，并给出取正文的下一步（避免它以为这就是全部）。
            ["has_comments"] = ticket.Comments is not null,
            ["customized_field_count"] = ticket.CustomizedFields?.Length ?? 0,
            ["notice"] = "本工具刻意只返回状态与流转事实：备注正文、工单消息与客户自定义字段值属敏感内容，"
                + "未纳入工具面（不要据此推断工单里没有细节）。",

            ["agent_service_duration"] = ticket.AgentServiceDuration,
            ["agent_first_response_duration"] = ticket.AgentFirstResponseDuration,
            ["agent_resolution_time"] = ticket.AgentResolutionTime,
            ["bot_service_duration"] = ticket.BotServiceDuration,
        };

    /// <summary>工单相关人：<b>仅标识与姓名</b>（邮箱 / 部门 / 城市 / 国家属客户画像，不进上下文）。</summary>
    private static JsonObject? Person(TicketUser? user)
        => user is null
            ? null
            : new JsonObject
            {
                ["user_id"] = user.Id,
                ["name"] = user.Name,
            };

    private static JsonArray People(TicketUser[]? users)
    {
        var array = new JsonArray();
        foreach (var user in users ?? [])
        {
            if (Person(user) is { } person)
            {
                array.AddNode(person);
            }
        }

        return array;
    }

    private static JsonArray TicketTags(TicketTag[]? tags)
    {
        var array = new JsonArray();
        foreach (var tag in tags ?? [])
        {
            array.AddNode(new JsonObject
            {
                ["id"] = tag.Id,
                ["name"] = tag.Name,
            });
        }

        return array;
    }

    private static JsonArray CategoryNames(Category[]? categories)
    {
        var array = new JsonArray();
        foreach (var category in categories ?? [])
        {
            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            array.AddNode(category.Name);
        }

        return array;
    }

    /// <summary>分类目录树（children 递归；只回结构，不回语言 / 服务台 ID 等平台内部字段）。</summary>
    private static JsonArray CategoryTree(Category[]? categories)
    {
        var array = new JsonArray();
        foreach (var category in categories ?? [])
        {
            array.AddNode(new JsonObject
            {
                ["category_id"] = category.CategoryId ?? category.Id,
                ["name"] = category.Name,
                ["parent_id"] = category.ParentId,
                ["children"] = CategoryTree(category.Children),
            });
        }

        return array;
    }

    private static JsonArray StringArray(string[]? values)
    {
        var array = new JsonArray();
        foreach (var value in values ?? [])
        {
            array.AddNode(value);
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
                $"未注册 {ClientName} 客户端——服务台能力需宿主启用服务台 API（软缺席，不影响其它域工具）"))
            : null;

    // ─────────────────── 写面：工单服务动作（tenant 侧） ───────────────────

    /// <summary>helpdesk.start_service：工单转人工。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskStartServiceTool))]
    public Task<ToolResult> StartServiceAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskStartService, _maxResultLength);
        if (Absent(executor.ToolName, _ticketClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskStartServiceArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/helpdesk/v1/start_service",
                    ToolDryRun.IdempotencyNote(null),
                    ("open_id", args.OpenId?.Length ?? 0),
                    ("appointed_agents", args.AppointedAgents?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _ticketClient!
                .StartServiceTicketAsync(
                    new StartServiceTicketRequest
                    {
                        OpenId = args.OpenId,
                        HumanService = args.HumanService,
                        AppointedAgents = args.AppointedAgents,
                        CustomizedInfo = args.CustomizedInfo,
                    },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["chat_id"] = data.ChatId,
                ["ticket_id"] = data.TicketId,
                ["started"] = true,
                ["notice"] = "转人工后由客服接管会话；指定客服未上线时服务台会按技能组重新分配。",
            });
        });
    }

    /// <summary>helpdesk.answer_user_query：回传用户提问命中的 FAQ。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskAnswerUserQueryTool))]
    public Task<ToolResult> AnswerUserQueryAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskAnswerUserQuery, _maxResultLength);
        if (Absent(executor.ToolName, _ticketClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskAnswerUserQueryArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/helpdesk/v1/tickets/{ticket_id}/answer_user_query",
                    ToolDryRun.IdempotencyNote(null),
                    ("ticket_id", args.TicketId?.Length ?? 0),
                    ("event_id", args.EventId?.Length ?? 0),
                    ("faq_ids", args.FaqIds?.Length ?? 0)));
            }

            var result = await _ticketClient!
                .AnswerUserQueryTicketAsync(
                    args.TicketId,
                    new AnswerUserQueryTicketRequest
                    {
                        EventId = args.EventId,
                        Faqs = args.FaqIds is { Length: > 0 }
                            ? [.. args.FaqIds.Select(static id => new UserQueryFaqInfo { Id = id })]
                            : null,
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            RequireNullDataSuccess(executor.ToolName, result);

            return ToolResultPipeline.OkReceipt(new JsonObject
            {
                ["ticket_id"] = args.TicketId,
                ["faq_count"] = args.FaqIds?.Length ?? 0,
                ["answered"] = true,
                ["notice"] = "已回传命中结果；本工具**不直接给用户发消息**——要回复用户请用 helpdesk.create_ticket_message。",
            });
        });
    }

    /// <summary>helpdesk.create_ticket_message：在工单里发消息（回复工单）。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskCreateTicketMessageTool))]
    public Task<ToolResult> CreateTicketMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskCreateTicketMessage, _maxResultLength);
        if (Absent(executor.ToolName, _ticketClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskCreateTicketMessageArgs.Unpack(arguments);

            // 参数校验早于 dry_run：msg_type 只接受 text/post，别的不该放行。
            if (!string.Equals(args.MsgType, "text", StringComparison.Ordinal)
                && !string.Equals(args.MsgType, "post", StringComparison.Ordinal))
            {
                return Invalid(executor.ToolName,
                    "msg_type 只支持 text（纯文本）或 post（富文本）——工单消息不支 image/interactive，"
                    + "那类请用 helpdesk.create_bot_message。");
            }

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/helpdesk/v1/tickets/{ticket_id}/messages",
                    ToolDryRun.IdempotencyNote(null),
                    ("ticket_id", args.TicketId?.Length ?? 0),
                    ("msg_type", args.MsgType?.Length ?? 0),
                    ("content", args.Content?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _ticketClient!
                .CreateTicketMessageAsync(
                    args.TicketId,
                    new CreateTicketMessageRequest { MsgType = args.MsgType, Content = args.Content },
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["ticket_id"] = args.TicketId,
                ["message_id"] = data.MessageId,
                ["msg_type"] = args.MsgType,
                ["sent"] = true,
                // 富文本原文不回显（与读面"工单正文不出站"纪律一致）。
            });
        });
    }

    /// <summary>helpdesk.create_bot_message：以机器人身份发消息。</summary>
    [ToolHandler(typeof(IFeishuTenantHelpdeskCreateBotMessageTool))]
    public Task<ToolResult> CreateBotMessageAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.HelpdeskCreateBotMessage, _maxResultLength);
        if (Absent(executor.ToolName, _ticketClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = HelpdeskCreateBotMessageArgs.Unpack(arguments);

            if (ToolDryRun.IsRequested(args.DryRun))
            {
                return ToolResult.FromText(ToolDryRun.Describe(
                    executor.ToolName,
                    "POST",
                    "/open-apis/helpdesk/v1/message",
                    ToolDryRun.IdempotencyNote(null),
                    ("msg_type", args.MsgType?.Length ?? 0),
                    ("content", args.Content?.Length ?? 0),
                    ("receiver_id", args.ReceiverId?.Length ?? 0)));
            }

            var outcome = FeishuApiResultReader.Read(await _ticketClient!
                .CreateBotMessageAsync(
                    new CreateBotMessageRequest
                    {
                        MsgType = args.MsgType,
                        Content = args.Content,
                        ReceiverId = args.ReceiverId,
                        ReceiveType = args.ReceiveType,
                    },
                    user_id_type: null,
                    cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, data => new JsonObject
            {
                ["message_id"] = data.MessageId,
                ["msg_type"] = args.MsgType,
                ["receive_type"] = args.ReceiveType,
                ["sent"] = true,
            });
        });
    }

    // ─────────────────── 写面内部工具 ───────────────────

    /// <summary>参数非法：结构化错误（不是异常——异常会逃出执行链，模型看不到可执行信息）。</summary>
    private static ToolResult Invalid(string toolName, string message)
        => ToolResult.FromError(FeishuToolBinding.StructuredError(toolName, message));

    /// <summary>
    /// 空数据结果的成功判定（返回 <see cref="FeishuNullDataApiResult"/>，Data 天然为 null，
    /// 不能用 <c>FeishuApiResultReader.Read</c>——那会把"无载荷"误判为失败）。
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
