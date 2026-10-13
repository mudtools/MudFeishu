// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Lingo;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Lingo「企业词典」只读工具执行器（B1 首个补域切片：6 个只读工具）。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么客户端是可空（软依赖）</b>：<c>FeishuToolDomainRegistrars</c> 的契约是
/// "客户端缺席 ⇒ 执行器缺席 ⇒ 注册器不注册"。若把 lingo 客户端声明为硬依赖，
/// 宿主未启用词典 API 时会连带让本执行器整体缺席；声明为可空后：未启用时这 6 个工具
/// <b>软缺席</b>（执行期给可执行的错误说明），已启用时正常在位。
/// </para>
/// <para>
/// <b>出站净化（本域的核心纪律）</b>：词典条目 DTO 自带 <c>creator</c> / <c>updater</c>
/// （员工身份，字段权限 <c>contact:user.employee_id:readonly</c>）与 <c>related_meta</c>
/// （关联用户 / 群 / 文档等外键，展开后是成片的组织信息）。本执行器的投影<b>一律不带出</b>
/// 这两类字段——词典回答的是"这个词什么意思"，不是"谁建了它 / 它关联了谁"。
/// </para>
/// <para>
/// <b>一次做对分页</b>：三个列表工具（分类 / 词条 / 搜索）在首批就接
/// <c>FromPagedApiAsync</c> 统一出口（专题四），不留技术债；
/// <c>list_repos</c>（无分页契约）与 <c>match_entities</c>（单次匹配）走单页出口。
/// </para>
/// </remarks>
internal sealed class LingoTools(
    IOptions<FeishuAgentOptions> options,
    Mud.Feishu.IFeishuTenantV1LingoRepo? repoClient = null,
    Mud.Feishu.IFeishuTenantV1LingoClassification? classificationClient = null,
    Mud.Feishu.IFeishuTenantV1LingoEntity? entityClient = null)
{
    /// <summary>词条正文预览的截断长度（复用消息预览口径：摘要够用，正文仍可 get_entity 取全）。</summary>
    private const int DescriptionPreviewLength = 200;

    private readonly Mud.Feishu.IFeishuTenantV1LingoRepo? _repoClient = repoClient;
    private readonly Mud.Feishu.IFeishuTenantV1LingoClassification? _classificationClient = classificationClient;
    private readonly Mud.Feishu.IFeishuTenantV1LingoEntity? _entityClient = entityClient;

    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>统一分页语义（专题四）：<c>fetch_all</c> 的条目 / 页数预算。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>软件缺席说明的统一措辞（三个客户端各自的缺席文案由 <see cref="Require{T}"/> 生成）。</summary>
    private const string ClientName = "IFeishuTenantV1Lingo*（词典 API）";

    // ─────────────────────────── lingo.list_repos ───────────────────────────

    /// <summary>lingo.list_repos：列出词库（进入本域的第一跳）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantLingoListReposTool))]
    public Task<FeishuToolResult> ListReposAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.LingoListRepos, _maxResultLength);
        if (Absent(executor.ToolName, _repoClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var outcome = FeishuApiResultReader.Read(await _repoClient!
                .GetRepoListAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, ProjectRepos);
        });
    }

    /// <summary>词库投影：只回 repo_id/name（空结果给"未配置词库"的可执行提示）。</summary>
    private static JsonObject ProjectRepos(GetRepoListResult result)
    {
        var items = new JsonArray();
        foreach (var repo in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["repo_id"] = repo.Id,
                ["name"] = repo.Name,
            });
        }

        var payload = new JsonObject { ["items"] = items };

        if (items.Count == 0)
        {
            // 空结果哨兵（F-5）：显式"没找到" + 可执行的下一步，而不是空数组让模型猜。
            payload["found"] = false;
            payload["message"] = "当前租户没有可返回的词库——需要管理员先在飞书「词典」后台创建词库；"
                + "在此之前 lingo.list_entities / lingo.search_entities 也不会返回租户自建词条。";
        }

        return payload;
    }

    // ─────────────────────────── lingo.list_classifications ───────────────────────────

    /// <summary>lingo.list_classifications：列出分类目录（分页）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantLingoListClassificationsTool))]
    public Task<FeishuToolResult> ListClassificationsAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.LingoListClassifications, _maxResultLength);
        if (Absent(executor.ToolName, _classificationClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = LingoListClassificationsArgs.Unpack(arguments);

            // page_size 刻意不下发（保持 SDK 默认）——运维参数不进模型可见面（DP-B1-0）。
            async Task<FeishuApiOutcome<GetClassificationListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _classificationClient!
                    .GetClassificationListAsync(page_token: token, repo_id: args.RepoId, cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectClassifications, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>分类投影：classification_id/name/father_id（father_id 供模型自行还原层级）。</summary>
    private static JsonObject ProjectClassifications(GetClassificationListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var item in result.Items ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["classification_id"] = item.Id,
                ["name"] = item.Name,
                ["father_id"] = item.FatherId,
            });
        }

        return envelope;
    }

    // ─────────────────────────── lingo.list_entities ───────────────────────────

    /// <summary>lingo.list_entities：浏览词条（分页，无关键字）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantLingoListEntitiesTool))]
    public Task<FeishuToolResult> ListEntitiesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.LingoListEntities, _maxResultLength);
        if (Absent(executor.ToolName, _entityClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = LingoListEntitiesArgs.Unpack(arguments);

            // page_size 不下发（DP-B1-0）；provider 是 SDK 原生业务过滤（可下发）。
            async Task<FeishuApiOutcome<GetEntityListResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _entityClient!
                    .GetEntityListAsync(
                        page_token: token,
                        provider: args.Provider,
                        repo_id: args.RepoId,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectEntityList, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>词条列表投影：entity_id/主词/别名/描述摘要（**不带** creator/updater/related_meta）。</summary>
    private static JsonObject ProjectEntityList(GetEntityListResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var entity in result.Entities ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["entity_id"] = entity.Id,
                ["main_keys"] = Terms(entity.MainKeys),
                ["aliases"] = Terms(entity.Aliases),
                ["description"] = ToolResultText.Truncate(entity.Description, DescriptionPreviewLength),
            });
        }

        return envelope;
    }

    // ─────────────────────────── lingo.get_entity ───────────────────────────

    /// <summary>lingo.get_entity：按 ID 查单个词条（完整投影）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantLingoGetEntityTool))]
    public Task<FeishuToolResult> GetEntityAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.LingoGetEntity, _maxResultLength);
        if (Absent(executor.ToolName, _entityClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = LingoGetEntityArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _entityClient!
                .GetEntityAsync(
                    args.EntityId,
                    provider: args.Provider,
                    outer_id: args.OuterId,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => result.Entity is null
                ? new JsonObject
                {
                    // 空结果哨兵（F-5）：把"ID 不对"与"没权限"分开讲，模型才知道下一步做什么。
                    ["found"] = false,
                    ["entity_id"] = args.EntityId,
                    ["message"] = "未查询到该词条。请确认 entity_id 来自 lingo.list_entities / lingo.search_entities 的返回，"
                        + "或用 lingo.search_entities 按关键词重新定位；若本域整体无返回，多为租户未启用词典或当前身份无 baike:entity:readonly。",
                }
                : Entity(result.Entity));
        });
    }

    // ─────────────────────────── lingo.search_entities ───────────────────────────

    /// <summary>lingo.search_entities：按关键词搜索词条（分页）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantLingoSearchEntitiesTool))]
    public Task<FeishuToolResult> SearchEntitiesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.LingoSearchEntities, _maxResultLength);
        if (Absent(executor.ToolName, _entityClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = LingoSearchEntitiesArgs.Unpack(arguments);

            var request = new SearchEntityRequest { Query = args.Query };
            if (args.Creators is { Length: > 0 } creators)
            {
                request.Creators = [.. creators];
            }

            // page_size 不下发（DP-B1-0）。SearchEntityResult 是"自带 has_more/page_token
            // 但不继承 ApiPageListResult"的历史 DTO（HasMore 为 bool?），故走显式页状态重载。
            async Task<FeishuApiOutcome<SearchEntityResult>> FetchPageAsync(string? token, CancellationToken ct)
                => FeishuApiResultReader.Read(await _entityClient!
                    .SearchEntityAsync(
                        request,
                        page_token: token,
                        repo_id: args.RepoId,
                        cancellationToken: ct)
                    .ConfigureAwait(false));

            return await executor.FromPagedApiAsync(
                FetchPageAsync,
                ProjectSearchResult,
                static page => (page.HasMore == true, page.PageToken),
                args.FetchAll,
                args.MaxItems,
                args.PageToken,
                _maxAutoFetchItems,
                _maxAutoFetchPages,
                cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>搜索结果投影：与 list_entities 同形（模型不必学两套条目形状）+ 空结果哨兵。</summary>
    private static JsonObject ProjectSearchResult(SearchEntityResult result)
    {
        var envelope = ToolResultJsons.PageEnvelope(result.HasMore, result.PageToken);
        var items = envelope["items"]!.AsArray();
        foreach (var entity in result.Entities ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["entity_id"] = entity.Id,
                ["main_keys"] = Terms(entity.MainKeys),
                ["aliases"] = Terms(entity.Aliases),
                ["description"] = ToolResultText.Truncate(entity.Description, DescriptionPreviewLength),
            });
        }

        if (items.Count == 0)
        {
            envelope["found"] = false;
            envelope["message"] = "没有匹配的词条。建议：换更短的短语重试（整句长文本命中率低）、"
                + "去掉 repo_id 限制以扩大到全部词库、或先 lingo.list_repos 确认词库是否选错。";
        }

        return envelope;
    }

    // ─────────────────────────── lingo.match_entities ───────────────────────────

    /// <summary>lingo.match_entities：词形精确匹配（单次，无分页）。</summary>
    [FeishuToolHandler(typeof(IFeishuTenantLingoMatchEntitiesTool))]
    public Task<FeishuToolResult> MatchEntitiesAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.LingoMatchEntities, _maxResultLength);
        if (Absent(executor.ToolName, _entityClient) is { } unavailable)
        {
            return Task.FromResult(unavailable);
        }

        return executor.RunAsync(async () =>
        {
            var args = LingoMatchEntitiesArgs.Unpack(arguments);

            var outcome = FeishuApiResultReader.Read(await _entityClient!
                .MatchEntityAsync(
                    new MatchEntityRequest { Word = args.Word },
                    repo_id: args.RepoId,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false));

            return executor.FromApi(outcome, result => ProjectMatches(result, args.Word));
        });
    }

    /// <summary>匹配结果投影：entity_id/type（未命中时给"不是术语"的明确结论 + 下一步）。</summary>
    private static JsonObject ProjectMatches(MatchEntityResult result, string word)
    {
        var items = new JsonArray();
        foreach (var match in result.Results ?? [])
        {
            items.AddNode(new JsonObject
            {
                ["entity_id"] = match.EntityId,
                ["type"] = match.Type,
            });
        }

        var payload = new JsonObject { ["word"] = word, ["matches"] = items };

        if (items.Count == 0)
        {
            payload["matched"] = false;
            payload["message"] = "该词不是企业词典中的术语（精确匹配未命中）。若要找相近含义的词条，"
                + "请改用 lingo.search_entities 按关键词搜索——两者的判据不同（精确词形 vs 关键词相关性）。";
        }

        return payload;
    }

    // ─────────────────────────── 内部工具 ───────────────────────────

    /// <summary>词条正文投影：主词/别名/描述/富文本/统计/多语言（**不含** creator/updater/related_meta）。</summary>
    private static JsonObject Entity(LingoEntity entity)
    {
        var payload = new JsonObject
        {
            ["found"] = true,
            ["entity_id"] = entity.Id,
            ["main_keys"] = Terms(entity.MainKeys),
            ["aliases"] = Terms(entity.Aliases),
            ["description"] = entity.Description,
            ["rich_text"] = entity.RichText,
            ["source"] = entity.Source,
            ["update_time"] = entity.UpdateTime,
        };

        if (entity.Statistics is { } statistics)
        {
            payload["statistics"] = new JsonObject
            {
                ["like_count"] = statistics.LikeCount,
                ["dislike_count"] = statistics.DislikeCount,
            };
        }

        if (entity.I18nDescs is { Length: > 0 } descriptions)
        {
            var i18n = new JsonArray();
            foreach (var description in descriptions)
            {
                i18n.AddNode(new JsonObject
                {
                    ["language"] = description.Language,
                    ["description"] = description.Description,
                });
            }

            payload["i18n_descriptions"] = i18n;
        }

        return payload;
    }

    /// <summary>词形数组投影：只取 term 文本（display_status 是内部展示档位，对模型无决策价值）。</summary>
    private static JsonArray Terms(LingoTerm[]? terms)
    {
        var array = new JsonArray();
        foreach (var term in terms ?? [])
        {
            // AddNode（而非 Add）：见 ToolResultText.AddNode——泛型 Add<T>(T) 的裁剪/AOT 注解会红。
            array.AddNode(term.Key);
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
                $"未注册 {ClientName} 客户端——企业词典能力需宿主启用词典 API（软缺席，不影响其它域工具）"))
            : null;
}
