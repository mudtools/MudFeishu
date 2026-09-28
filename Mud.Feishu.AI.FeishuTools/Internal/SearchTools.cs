// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Search;

namespace Mud.Feishu.AI.FeishuTools.Internal;

/// <summary>
/// Search 单工具执行器（<c>search.doc_wiki</c>）：<c>search_in</c> →
/// <see cref="SearchDocWikiRequest.DocFilter"/>/<see cref="SearchDocWikiRequest.WikiFilter"/>
/// 扁平化构造（满足官方「两 filter 至少一个」约束，§3.3.3）。
/// </summary>
/// <remarks>执行骨架（catch/回填/截断）由 <see cref="ToolExecutor"/> 承担（WP3）；本类只保留参数校验与投影语义。</remarks>
internal sealed class SearchTools(Mud.Feishu.IFeishuTenantV2SearchDocWiki searchClient, IOptions<FeishuAgentOptions> options)
{
    private readonly Mud.Feishu.IFeishuTenantV2SearchDocWiki _searchClient = searchClient
        ?? throw new ArgumentNullException(nameof(searchClient));
    private readonly int _maxResultLength = (options ?? throw new ArgumentNullException(nameof(options))).Value.MaxToolResultLength;

    /// <summary>search.doc_wiki：云文档与知识库搜索（白名单 title/url/owner/doc_type）。</summary>
    public Task<FeishuToolResult> SearchAsync(IReadOnlyDictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var executor = new ToolExecutor(FeishuToolNames.SearchDocWiki, _maxResultLength);
        return executor.RunAsync(async () =>
        {
            var query = ToolArgs.RequireString(arguments, "query");
            if (query.Length > 30)
            {
                return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, "query 上限 30 字符，实际 " + query.Length.ToString(CultureInfo.InvariantCulture) + " 字符"));
            }

            var searchIn = (ToolArgs.OptionalString(arguments, "search_in") ?? "both").ToLowerInvariant();
            var folderTokens = ToolArgs.OptionalStringArray(arguments, "folder_tokens");
            var spaceIds = ToolArgs.OptionalStringArray(arguments, "space_ids");
            var pageToken = ToolArgs.OptionalString(arguments, "page_token");

            var request = new SearchDocWikiRequest
            {
                Query = query,
                PageToken = pageToken,
                PageSize = PageSizes.Search,
            };

            switch (searchIn)
            {
                case "doc":
                    request.DocFilter = new DocFilterParam { FolderTokens = folderTokens };
                    break;
                case "wiki":
                    request.WikiFilter = new WikiFilterParam { SpaceIds = spaceIds };
                    break;
                case "both":
                    // 两者都下发（空过滤对象），folder_tokens/space_ids 分别入对应 filter。
                    request.DocFilter = new DocFilterParam { FolderTokens = folderTokens };
                    request.WikiFilter = new WikiFilterParam { SpaceIds = spaceIds };
                    break;
                default:
                    return FeishuToolResult.FromError(FeishuToolBinding.StructuredError(executor.ToolName, $"search_in 仅支持 doc/wiki/both，实际: {searchIn}"));
            }

            var outcome = FeishuApiResultReader.Read(await _searchClient
                .SearchDocWikiAsync(request, cancellationToken)
                .ConfigureAwait(false));
            return executor.FromApi(outcome, ProjectSearch);
        });
    }

    /// <summary>search 投影：items（title/url/owner/doc_type/token）+ total + 翻页契约。</summary>
    private static JsonObject ProjectSearch(SearchDocWikiResult data)
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
