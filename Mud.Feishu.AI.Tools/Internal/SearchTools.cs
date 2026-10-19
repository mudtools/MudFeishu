// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Search;

namespace Mud.Feishu.AI.Tools.Internal;

/// <summary>
/// Search 单工具执行器（<c>search.doc_wiki</c>）：<c>search_in</c> →
/// <see cref="SearchDocWikiRequest.DocFilter"/>/<see cref="SearchDocWikiRequest.WikiFilter"/>
/// 扁平化构造（满足官方「两 filter 至少一个」约束，§3.3.3）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class SearchTools(IFeishuTenantV2SearchDocWiki searchClient, IOptions<FeishuAgentOptions> options)
{
    private readonly IFeishuTenantV2SearchDocWiki _searchClient = searchClient
        ?? throw new ArgumentNullException(nameof(searchClient));
    private readonly int _maxResultLength = ToolExecutor.Require(options).MaxToolResultLength;

    /// <summary>P-4（统一分页语义）：<c>fetch_all</c> 的条目/页数预算（来自宿主配置）。</summary>
    private readonly int _maxAutoFetchItems = ToolExecutor.Require(options).MaxAutoFetchItems;
    private readonly int _maxAutoFetchPages = ToolExecutor.Require(options).MaxAutoFetchPages;

    /// <summary>search.doc_wiki：云文档与知识库搜索（白名单 title/url/owner/doc_type）。</summary>
    [ToolHandler(typeof(IFeishuTenantSearchDocWikiTool))]
    public Task<ToolResult> SearchAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SearchDocWiki, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var args = SearchDocWikiArgs.Unpack(arguments);

            if (args.Query.Length > 30)
            {
                return ToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, "query 上限 30 字符，实际 " + args.Query.Length.ToString(CultureInfo.InvariantCulture) + " 字符"));
            }

            var searchIn = (args.SearchIn ?? "both").ToLowerInvariant();

            // search_in 的闭集校验保持**一次**（在构造首个请求之前），两条路径共用同一判定。
            if (searchIn is not ("doc" or "wiki" or "both"))
            {
                return ToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, $"search_in 仅支持 doc/wiki/both，实际: {searchIn}"));
            }

            // P-4：统一分页出口——fetch_all 聚合与单页共用**同一次调用定义**与**同一份投影**。
            // 每次翻页都按同一个 search_in 重建请求（token 只影响 PageToken）。
            async Task<ToolApiOutcome<SearchDocWikiResult>> FetchPageAsync(string? token, CancellationToken ct)
            {
                var request = new SearchDocWikiRequest
                {
                    Query = args.Query,
                    PageToken = token,
                    PageSize = PageSizes.Search,
                };

                if (searchIn is "doc" or "both")
                {
                    request.DocFilter = new DocFilterParam { FolderTokens = args.FolderTokens };
                }

                if (searchIn is "wiki" or "both")
                {
                    request.WikiFilter = new WikiFilterParam { SpaceIds = args.SpaceIds };
                }

                return FeishuApiResultReader.Read(await _searchClient
                    .SearchDocWikiAsync(request, ct)
                    .ConfigureAwait(false));
            }

            return await executor.FromPagedApiAsync(
                FetchPageAsync, ProjectSearch, args.FetchAll, args.MaxItems, args.PageToken,
                _maxAutoFetchItems, _maxAutoFetchPages, cancellationToken).ConfigureAwait(false);
        });
    }

    /// <summary>search 投影：items（title/url/owner/doc_type/token）+ total + 翻页契约。</summary>
    private static JsonObject ProjectSearch(SearchDocWikiResult data)
    {
        // R-3：信封形态单源（ToolResultJsons.PageEnvelope）。
        var envelope = ToolResultJsons.PageEnvelope(data.HasMore, data.PageToken);

        if (data.Total.HasValue)
        {
            envelope["total"] = data.Total.Value;
        }

        foreach (var unit in data.ResUnits ?? [])
        {
            envelope["items"]!.AsArray().AddNode(new JsonObject
            {
                ["title"] = unit.TitleHighlighted,
                ["url"] = unit.ResultMeta?.Url,
                ["owner"] = unit.ResultMeta?.OwnerName,
                ["doc_type"] = unit.EntityType,
                ["token"] = unit.ResultMeta?.Token,
            });
        }

        return envelope;
    }
}
