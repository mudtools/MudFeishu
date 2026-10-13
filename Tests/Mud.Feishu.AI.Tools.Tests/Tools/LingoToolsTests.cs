// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Lingo 企业词典域级测试（专题一 B1 首个补域，SOP 第 8 步）。
/// </summary>
/// <remarks>
/// <para>
/// 覆盖三类必测面：
/// <list type="bullet">
/// <item><b>happy path</b>：投影字段与信封（含分页透传）符合契约；</item>
/// <item><b>空结果不给空数组</b>：每个"可能查不到"的工具都要有可执行的哨兵文案；</item>
/// <item><b>出站净化</b>：词条投影<b>不得</b>带出 creator / updater / related_meta（本域的 PII 边界）。</item>
/// <item><b>软缺席</b>：客户端缺席时给结构化错误结果，而不是抛异常。</item>
/// </list>
/// </para>
/// <para>
/// policy 拒绝零调用与审计三类投递<b>不在此重复</b>——它们由
/// <c>ToolPolicyContractGuards</c> / <c>FeishuToolBindingAuditTests</c> 通用覆盖（域无关）。
/// </para>
/// </remarks>
public class LingoToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static LingoTools CreateTools(
        Mock<Mud.Feishu.IFeishuTenantV1LingoRepo>? repo = null,
        Mock<Mud.Feishu.IFeishuTenantV1LingoClassification>? classification = null,
        Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>? entity = null,
        Mock<Mud.Feishu.IFeishuTenantV1LingoDraft>? draft = null)
        => new(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            repo?.Object,
            classification?.Object,
            entity?.Object,
            draft?.Object);

    private static JsonObject Payload(FeishuToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static FeishuApiResult<T> Ok<T>(T data)
        where T : class
        => new() { Code = 0, Data = data };

    private static LingoEntity SampleEntity(string id, string mainKey, string description)
        => new()
        {
            Id = id,
            MainKeys = [new LingoTerm { Key = mainKey }],
            Aliases = [new LingoTerm { Key = $"{mainKey}-别名" }],
            Description = description,
            RichText = "<p>富文本</p>",
            Source = 2,
            UpdateTime = "2026-10-13T10:00:00Z",

            // ⚠️ 以下三项是**必须被投影丢掉**的字段（本测试断言它们不出现在出站 JSON 里）。
            Creator = "ou_creator",
            Updater = "ou_updater",
            RelatedMeta = new LingoRelatedMeta { Users = [new LingoReferer { Id = "ou_x" }] },
            Statistics = new LingoStatistics { LikeCount = 3, DislikeCount = 0 },
            I18nDescs = [new LingoI18nEntryDesc { Language = 2, Description = "quarterly review" }],
        };

    // ─────────────────────────── happy path ───────────────────────────

    [Fact]
    public async Task ListRepos_Should_Project_Id_And_Name()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoRepo>();
        client.Setup(c => c.GetRepoListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetRepoListResult
            {
                Items = [new LingoRepo { Id = "r1", Name = "术语库" }],
            }));

        var payload = Payload(await CreateTools(repo: client).ListReposAsync(
            Args(), CancellationToken.None));

        payload["items"]!.AsArray().Should().HaveCount(1);
        payload["items"]!.AsArray()[0]!["repo_id"]!.GetValue<string>().Should().Be("r1");
        payload["items"]!.AsArray()[0]!["name"]!.GetValue<string>().Should().Be("术语库");
    }

    [Fact]
    public async Task ListRepos_Should_Return_Executable_Sentinel_When_No_Repos()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoRepo>();
        client.Setup(c => c.GetRepoListAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetRepoListResult { Items = [] }));

        var payload = Payload(await CreateTools(repo: client).ListReposAsync(
            Args(), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse("空结果必须显式给 found=false 哨兵");
        payload["message"]!.GetValue<string>().Should().Contain("词库", "哨兵要指向可执行的下一步");
    }

    [Fact]
    public async Task SearchEntities_Should_Project_Summary_And_Paging()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>();
        client.Setup(c => c.SearchEntityAsync(
                It.IsAny<SearchEntityRequest>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new SearchEntityResult
            {
                Entities = [SampleEntity("e1", "OKR", "目标与关键结果")],
                HasMore = true,
                PageToken = "t2",
            }));

        var payload = Payload(await CreateTools(entity: client).SearchEntitiesAsync(
            Args(("query", "OKR")), CancellationToken.None));

        payload["items"]!.AsArray().Should().HaveCount(1);
        payload["items"]!.AsArray()[0]!["entity_id"]!.GetValue<string>().Should().Be("e1");
        payload["items"]!.AsArray()[0]!["main_keys"]!.AsArray()[0]!.GetValue<string>().Should().Be("OKR");
        payload["has_more"]!.GetValue<bool>().Should().BeTrue("单页路径原样透传下游的 has_more");
        payload["page_token"]!.GetValue<string>().Should().Be("t2");
    }

    [Fact]
    public async Task SearchEntities_Should_Return_Executable_Sentinel_When_No_Match()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>();
        client.Setup(c => c.SearchEntityAsync(
                It.IsAny<SearchEntityRequest>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new SearchEntityResult { Entities = [], HasMore = false }));

        var payload = Payload(await CreateTools(entity: client).SearchEntitiesAsync(
            Args(("query", "不存在的词")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>().Should().Contain("repo_id", "哨兵要给出可执行的排查方向");
    }

    [Fact]
    public async Task GetEntity_Should_Project_Full_Entity_Without_Any_Pii_Field()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>();
        client.Setup(c => c.GetEntityAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetEntityResult { Entity = SampleEntity("e1", "OKR", "目标与关键结果") }));

        var payload = Payload(await CreateTools(entity: client).GetEntityAsync(
            Args(("entity_id", "e1")), CancellationToken.None));

        var entity = payload["entity"] ?? payload;
        payload["found"]!.GetValue<bool>().Should().BeTrue();

        // happy path 字段
        payload["entity_id"]!.GetValue<string>().Should().Be("e1");
        payload["main_keys"]!.AsArray()[0]!.GetValue<string>().Should().Be("OKR");
        payload["aliases"]!.AsArray()[0]!.GetValue<string>().Should().Be("OKR-别名");
        payload["description"]!.GetValue<string>().Should().Be("目标与关键结果");
        payload["rich_text"]!.GetValue<string>().Should().Be("<p>富文本</p>");
        payload["source"]!.GetValue<int>().Should().Be(2);
        payload["statistics"]!["like_count"]!.GetValue<int>().Should().Be(3);
        payload["i18n_descriptions"]!.AsArray()[0]!["description"]!.GetValue<string>().Should().Be("quarterly review");

        // 出站净化（本域 PII 边界）：身份与关联对象一律不得出现
        payload.ContainsKey("creator").Should().BeFalse("创建人属员工身份信息，不得出站");
        payload.ContainsKey("updater").Should().BeFalse("更新人同上");
        payload.ContainsKey("related_meta").Should().BeFalse("关联对象展开后是成片组织信息，不得出站");
        entity.Should().NotBeNull("投影必须是词条对象本身（不是包一层 let-the-model-guess 的壳）");
    }

    [Fact]
    public async Task GetEntity_Should_Return_Executable_Sentinel_When_NotFound()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>();
        client.Setup(c => c.GetEntityAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetEntityResult { Entity = null }));

        var payload = Payload(await CreateTools(entity: client).GetEntityAsync(
            Args(("entity_id", "e404")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["entity_id"]!.GetValue<string>().Should().Be("e404");
        payload["message"]!.GetValue<string>().Should().Contain("search_entities", "哨兵要指名下一步该用哪个工具");
    }

    [Fact]
    public async Task MatchEntities_Should_Explain_When_NotMatched()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>();
        client.Setup(c => c.MatchEntityAsync(
                It.IsAny<MatchEntityRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new MatchEntityResult { Results = [] }));

        var payload = Payload(await CreateTools(entity: client).MatchEntitiesAsync(
            Args(("word", "季度经营会")), CancellationToken.None));

        payload["word"]!.GetValue<string>().Should().Be("季度经营会", "回显输入词，模型才能把这行结果对上号");
        payload["matched"]!.GetValue<bool>().Should().BeFalse();
        payload["matches"]!.AsArray().Should().BeEmpty();
        payload["message"]!.GetValue<string>().Should().Contain("search_entities", "要指出精确匹配与关键词搜索的判据不同");
    }

    [Fact]
    public async Task ListClassifications_Should_Pass_Through_Paging_Envelope()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoClassification>();
        client.Setup(c => c.GetClassificationListAsync(
                It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new GetClassificationListResult
            {
                Items = [new LingoClassification { Id = "c1", Name = "业务术语", FatherId = "0" }],
                HasMore = false,
            }));

        var payload = Payload(await CreateTools(classification: client).ListClassificationsAsync(
            Args(("repo_id", "r1")), CancellationToken.None));

        payload["items"]!.AsArray()[0]!["classification_id"]!.GetValue<string>().Should().Be("c1");
        payload["items"]!.AsArray()[0]!["father_id"]!.GetValue<string>().Should().Be("0");
        payload["has_more"]!.GetValue<bool>().Should().BeFalse();
    }

    // ─────────────────────────── 软缺席 ───────────────────────────

    [Fact]
    public async Task AllTools_Should_SoftlyDegrade_When_Clients_Are_Absent()
    {
        // 三个客户端全缺席 = 宿主未启用词典 API：六个工具都必须给**结构化错误结果**（不抛异常）。
        var tools = CreateTools();

        var results = new[]
        {
            await tools.ListReposAsync(Args(), CancellationToken.None),
            await tools.ListClassificationsAsync(Args(), CancellationToken.None),
            await tools.ListEntitiesAsync(Args(), CancellationToken.None),
            await tools.GetEntityAsync(Args(("entity_id", "e1")), CancellationToken.None),
            await tools.SearchEntitiesAsync(Args(("query", "OKR")), CancellationToken.None),
            await tools.MatchEntitiesAsync(Args(("word", "OKR")), CancellationToken.None),
        };

        foreach (var result in results)
        {
            // 软缺席走的是**文本形态**的结构化错误（与 FeishuToolResult.FromError(string) 同款）：
            // 它不进 result.Error（那是宿主侧异常语义），而是让模型在正文里读到"不可用 + 为什么"。
            result.Text.Should().NotBeNull("软缺席必须给可执行说明，而不是抛异常");
            result.Text.Should().Contain("未注册", "文案要说清是宿主未启用该 API，而不是模型参数错");
            result.Text.Should().Contain("tool_error", "必须是结构化错误（第一行紧凑 JSON），便于模型与宿主统一处理");
        }
    }

    // ───────────────────── 写面（免审词条 CRUD + 草稿申请） ─────────────────────

    private static Mock<Mud.Feishu.IFeishuTenantV1LingoEntity> WriteEntityClient(
        Action? onCreate = null,
        Action? onUpdate = null,
        Action? onDelete = null)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>();
        client.Setup(c => c.CreateEntityAsync(
                It.IsAny<CreateOrUpdateEntityRequest>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateOrUpdateEntityRequest _, string? __, string? ___, CancellationToken ____) =>
            {
                onCreate?.Invoke();
                return Ok(new CreateEntityResult { Entity = SampleEntity("ent_new", "OKR", "目标与关键结果") });
            });

        client.Setup(c => c.UpdateEntityAsync(
                It.IsAny<string>(), It.IsAny<CreateOrUpdateEntityRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CreateOrUpdateEntityRequest __, string? ___, CancellationToken ____) =>
            {
                onUpdate?.Invoke();
                return Ok(new UpdateEntityResult { Entity = SampleEntity("ent_1", "OKR", "目标与关键结果（改）") });
            });

        client.Setup(c => c.DeleteEntityAsync(
                It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string? __, string? ___, CancellationToken ____) =>
            {
                onDelete?.Invoke();
                return Ok(new DeleteEntityResult());
            });

        return client;
    }

    private static Mock<Mud.Feishu.IFeishuTenantV1LingoDraft> WriteDraftClient(
        Action? onCreate = null,
        Action? onUpdate = null)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoDraft>();
        client.Setup(c => c.CreateDraftAsync(
                It.IsAny<CreateOrUpdateEntityRequest>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateOrUpdateEntityRequest _, string? __, string? ___, CancellationToken ____) =>
            {
                onCreate?.Invoke();
                return Ok(new CreateDraftResult { Draft = new LingoDraft { DraftId = "dft_new", Entity = SampleEntity("ent_new", "OKR", "草稿释义") } });
            });

        client.Setup(c => c.UpdateDraftAsync(
                It.IsAny<string>(), It.IsAny<CreateOrUpdateEntityRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, CreateOrUpdateEntityRequest __, string? ___, CancellationToken ____) =>
            {
                onUpdate?.Invoke();
                return Ok(new UpdateDraftResult { Draft = new LingoDraft { DraftId = "dft_1", Entity = SampleEntity("ent_1", "OKR", "草稿释义（改）") } });
            });

        return client;
    }

    /// <summary>dry_run 必须零调用下游（写工具第一道防线）。</summary>
    [Fact]
    public async Task CreateEntity_DryRun_Should_Not_Call_Downstream()
    {
        var calls = 0;
        var client = WriteEntityClient(onCreate: () => calls++);

        var result = await CreateTools(entity: client).CreateEntityAsync(
            Args(("main_key", "OKR"), ("description", "目标与关键结果"), ("aliases", new[] { "Objectives" }), ("dry_run", true)),
            CancellationToken.None);

        calls.Should().Be(0, "dry_run=true 不得调用下游");
        result.Text.Should().Contain("未调用下游");
        result.Text.Should().Contain("POST");
        result.Text.Should().Contain("main_key", "预演只报字段长度，不回原文");
    }

    /// <summary>实发：回执带 entity_id，且请求体按白名单合成（含展示状态）。</summary>
    [Fact]
    public async Task CreateEntity_Should_Project_EntityId_And_Build_Request()
    {
        CreateOrUpdateEntityRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1LingoEntity>();
        client.Setup(c => c.CreateEntityAsync(
                It.IsAny<CreateOrUpdateEntityRequest>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CreateOrUpdateEntityRequest request, string? _, string? __, CancellationToken ___) =>
            {
                captured = request;
                return Ok(new CreateEntityResult { Entity = SampleEntity("ent_new", "OKR", "目标与关键结果") });
            });

        var payload = Payload(await CreateTools(entity: client).CreateEntityAsync(
            Args(
                ("main_key", "OKR"),
                ("description", "目标与关键结果"),
                ("aliases", new[] { "Objectives", "O-K-R" }),
                ("allow_highlight", false),
                ("allow_search", true)),
            CancellationToken.None));

        captured!.MainKeys!.Length.Should().Be(1, "main_keys 最多 1 个");
        captured.MainKeys[0].Key.Should().Be("OKR");
        captured.MainKeys[0].DisplayStatus!.AllowHighlight.Should().BeFalse("allow_highlight 必须下发给平台");
        captured.MainKeys[0].DisplayStatus.AllowSearch.Should().BeTrue();
        captured.Aliases!.Length.Should().Be(2);
        captured.Description.Should().Be("目标与关键结果");

        payload["entity_id"]!.GetValue<string>().Should().Be("ent_new");
        payload["created"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("免审", "必须复述免审直写这一后果");
    }

    /// <summary>description 与 rich_text 都空 ⇒ 平台会报 1540001，必须在 dry_run 之前拦下。</summary>
    [Fact]
    public async Task CreateEntity_Without_Description_Should_Reject_Before_DryRun()
    {
        var calls = 0;
        var client = WriteEntityClient(onCreate: () => calls++);

        var result = await CreateTools(entity: client).CreateEntityAsync(
            Args(("main_key", "OKR"), ("dry_run", true)), CancellationToken.None);

        calls.Should().Be(0, "参数非法时连预演都不放行");
        result.Text.Should().Contain("tool_error");
        result.Text.Should().Contain("1540001", "错误信息要带平台错误码，便于模型自查");
    }

    /// <summary>别名超 10 个 ⇒ 平台硬约束，必须拦。</summary>
    [Fact]
    public async Task CreateEntity_With_Too_Many_Aliases_Should_Reject()
    {
        var client = WriteEntityClient();

        var result = await CreateTools(entity: client).CreateEntityAsync(
            Args(
                ("main_key", "OKR"),
                ("description", "释义"),
                ("aliases", Enumerable.Range(0, 11).Select(i => $"alias-{i}").ToArray())),
            CancellationToken.None);

        result.Text.Should().Contain("tool_error");
        result.Text.Should().Contain("10", "要报出平台上限");
    }

    /// <summary>update_entity：覆盖式语义，回执给改动字段数 + 覆盖提醒。</summary>
    [Fact]
    public async Task UpdateEntity_Should_Project_Replace_Semantics()
    {
        var client = WriteEntityClient();

        var payload = Payload(await CreateTools(entity: client).UpdateEntityAsync(
            Args(("entity_id", "ent_1"), ("main_key", "OKR"), ("description", "新释义")),
            CancellationToken.None));

        payload["entity_id"]!.GetValue<string>().Should().Be("ent_1");
        payload["updated_fields"]!.GetValue<int>().Should().Be(2);
        payload["updated"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("覆盖式");
    }

    /// <summary>update_entity 缺 main_key ⇒ 由必填参数校验层拦（平台请求体口径：main_keys 必填）。</summary>
    /// <remarks>
    /// 这条用例同时钉住一个**设计修正**：最初我把 <c>main_key</c> 标成可选并写了
    /// "不传则沿用原名"——但平台请求体要求 <c>main_keys</c> 必填，缺了必被拒。
    /// 故改为必填参数（Required = true），由框架校验层统一拦，
    /// 执行器里那个"一个字段都不传"的自定义分支随之成为死代码并被删除。
    /// </remarks>
    [Fact]
    public async Task UpdateEntity_Without_MainKey_Should_Reject_As_InvalidArgs()
    {
        var calls = 0;
        var client = WriteEntityClient(onUpdate: () => calls++);

        var result = await CreateTools(entity: client).UpdateEntityAsync(
            Args(("entity_id", "ent_1"), ("description", "只改释义")), CancellationToken.None);

        calls.Should().Be(0, "平台要求 main_keys 必填，缺了必被拒，不该放行");
        result.Text.Should().Contain("tool_error");
        result.Text.Should().Contain("invalid_args", "缺必填参数由校验层拦，subtype 是 invalid_args");
    }

    /// <summary>delete_entity（高危）：两段式——先 dry_run 零调用，再实发回执。</summary>
    [Fact]
    public async Task DeleteEntity_DryRun_Then_Real_Should_Return_Receipt()
    {
        var calls = 0;
        var client = WriteEntityClient(onDelete: () => calls++);

        var preview = await CreateTools(entity: client).DeleteEntityAsync(
            Args(("entity_id", "ent_1"), ("dry_run", true)), CancellationToken.None);

        calls.Should().Be(0);
        preview.Text.Should().Contain("未调用下游");
        preview.Text.Should().Contain("DELETE");

        var payload = Payload(await CreateTools(entity: client).DeleteEntityAsync(
            Args(("entity_id", "ent_1")), CancellationToken.None));

        calls.Should().Be(1);
        payload["entity_id"]!.GetValue<string>().Should().Be("ent_1");
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("不可逆");
    }

    /// <summary>草稿走审核流程：回执给 draft_id + 需审核提醒。</summary>
    [Fact]
    public async Task CreateDraft_Should_Project_DraftId_And_Review_Notice()
    {
        var client = WriteDraftClient();

        var payload = Payload(await CreateTools(draft: client).CreateDraftAsync(
            Args(("main_key", "OKR"), ("description", "目标与关键结果")),
            CancellationToken.None));

        payload["draft_id"]!.GetValue<string>().Should().Be("dft_new");
        payload["submitted"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("审核", "草稿必须说明要过审核");
    }

    /// <summary>update_draft：dry_run 零调用 + 实发回执带"已审批不可编辑"提醒。</summary>
    [Fact]
    public async Task UpdateDraft_DryRun_Then_Real_Should_Return_Receipt()
    {
        var calls = 0;
        var client = WriteDraftClient(onUpdate: () => calls++);

        var preview = await CreateTools(draft: client).UpdateDraftAsync(
            Args(("draft_id", "dft_1"), ("main_key", "OKR"), ("description", "新释义"), ("dry_run", true)), CancellationToken.None);

        calls.Should().Be(0);
        preview.Text.Should().Contain("未调用下游");

        var payload = Payload(await CreateTools(draft: client).UpdateDraftAsync(
            Args(("draft_id", "dft_1"), ("main_key", "OKR"), ("description", "新释义")), CancellationToken.None));

        calls.Should().Be(1);
        payload["draft_id"]!.GetValue<string>().Should().Be("dft_1");
        payload["updated"]!.GetValue<bool>().Should().BeTrue();
        payload["notice"]!.GetValue<string>().Should().Contain("已审批", "必须复述平台限制");
    }

    /// <summary>写面客户端软缺席：必须给结构化错误，不得抛异常。</summary>
    [Fact]
    public async Task Write_Face_Missing_Clients_Should_Return_Structured_Error()
    {
        var tools = CreateTools();

        foreach (var result in new[]
        {
            await tools.CreateEntityAsync(Args(("main_key", "x"), ("description", "y")), CancellationToken.None),
            await tools.UpdateEntityAsync(Args(("entity_id", "e"), ("description", "y")), CancellationToken.None),
            await tools.DeleteEntityAsync(Args(("entity_id", "e")), CancellationToken.None),
            await tools.CreateDraftAsync(Args(("main_key", "x"), ("description", "y")), CancellationToken.None),
            await tools.UpdateDraftAsync(Args(("draft_id", "d"), ("description", "y")), CancellationToken.None),
        })
        {
            result.Text.Should().Contain("未注册", "写面软缺席同样要给可执行说明");
            result.Text.Should().Contain("tool_error");
        }
    }
}
