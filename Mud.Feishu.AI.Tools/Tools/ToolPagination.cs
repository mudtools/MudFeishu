// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Tools;

/// <summary>
/// 自动分页聚合器（B1）：把多次翻页调用的结果合并为单个信封，在预算内自动取完。
/// </summary>
/// <remarks>
/// <para>
/// <b>设计约束</b>：
/// <list type="bullet">
/// <item>唯一翻页实现——执行器内不得手写 <c>while</c>/<c>do</c> 翻页循环（守卫机械断言）；</item>
/// <item><c>fetch_all=false</c> 时逐字节等价既有行为（单次请求，返回 <c>has_more</c>/<c>page_token</c>）；</item>
/// <item><c>fetch_all=true</c> 时循环翻页，直到 <c>has_more=false</c> 或触达 <c>maxItems</c>/<c>maxPages</c>；</item>
/// <item>触达预算上限时结果信封带 <c>truncated: true</c> + <c>next_page_token</c>（给出可续 token）；</item>
/// <item>中途某页失败：保留已取数据并以结构化错误说明第几页失败（不整批丢弃）；</item>
/// <item><c>dry_run=true</c> 时不翻页（只预览第一页的请求）。</item>
/// </list>
/// </para>
/// </remarks>
internal static class ToolPagination
{
    /// <summary>硬上限：单次 <c>fetch_all</c> 最多取多少条（防失控）。</summary>
    public const int HardMaxItems = 1000;

    /// <summary>硬上限：单次 <c>fetch_all</c> 最多翻多少页（防失控）。</summary>
    public const int HardMaxPages = 50;

    /// <summary>
    /// 聚合分页结果。
    /// </summary>
    /// <param name="Items">已聚合的全部条目（JSON 数组）。</param>
    /// <param name="Truncated">是否因触达预算上限而截断。</param>
    /// <param name="NextPageToken">截断时的可续 token（未截断时为 null）。</param>
    /// <param name="TotalFetched">已取条目总数。</param>
    /// <param name="PagesFetched">已请求页数。</param>
    /// <param name="Error">中途失败时的错误信息（null = 全部成功）。</param>
    /// <param name="FailedPageIndex">失败页的序号（0 起；未失败时为 null）。</param>
    internal sealed record PagedFetchResult(
        JsonArray Items,
        bool Truncated,
        string? NextPageToken,
        int TotalFetched,
        int PagesFetched,
        string? Error,
        int? FailedPageIndex);

    /// <summary>
    /// 自动翻页聚合：循环调用 <paramref name="fetchPage"/> 直到 <c>has_more=false</c> 或触达预算上限。
    /// </summary>
    /// <typeparam name="TPage">单页数据类型（须为 class，因为来自 SDK 返回）。</typeparam>
    /// <param name="fetchPage">翻页函数：接收 pageToken，返回单页结果（null 表示下游失败）。</param>
    /// <param name="readPageState">从单页结果读取 (HasMore, NextToken)。</param>
    /// <param name="extractItems">从单页结果提取条目列表（JsonArray 形态）。</param>
    /// <param name="maxItems">结果预算上限（条目数）。</param>
    /// <param name="maxPages">页数上限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>聚合结果。</returns>
    public static async Task<PagedFetchResult> AggregateAsync<TPage>(
        Func<string?, CancellationToken, Task<TPage?>> fetchPage,
        Func<TPage, (bool HasMore, string? NextToken)> readPageState,
        Func<TPage, JsonArray> extractItems,
        int maxItems,
        int maxPages,
        CancellationToken cancellationToken)
        where TPage : class
    {
        if (fetchPage is null) throw new ArgumentNullException(nameof(fetchPage));
        if (readPageState is null) throw new ArgumentNullException(nameof(readPageState));
        if (extractItems is null) throw new ArgumentNullException(nameof(extractItems));

        var allItems = new JsonArray();
        var totalFetched = 0;
        var pagesFetched = 0;
        string? currentPageToken = null;
        var truncated = false;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // 页数上限检查（maxPages 优先）
            if (pagesFetched >= maxPages)
            {
                truncated = true;
                break;
            }

            // 条目预算检查
            if (totalFetched >= maxItems)
            {
                truncated = true;
                break;
            }

            TPage? page;
            try
            {
                page = await fetchPage(currentPageToken, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 守卫白名单：中途某页失败时保留已取数据并显式回填警告（非静默——失败原因
                // 随 PagedFetchResult.Warning 透出给调用方；SilentCatchContractGuards 豁免标记）。
                return new PagedFetchResult(
                    allItems, truncated, currentPageToken, totalFetched, pagesFetched,
                    $"第 {pagesFetched.ToString(CultureInfo.InvariantCulture)} 页获取失败: {ex.Message}",
                    pagesFetched);
            }

            if (page is null)
            {
                // 下游返回 null（网络层吞异常的形态）
                return new PagedFetchResult(
                    allItems, truncated, currentPageToken, totalFetched, pagesFetched,
                    $"第 {pagesFetched.ToString(CultureInfo.InvariantCulture)} 页返回空结果",
                    pagesFetched);
            }

            pagesFetched++;

            var (hasMore, nextToken) = readPageState(page);
            var pageItems = extractItems(page);

            // 追加条目
            foreach (var item in pageItems)
            {
                if (totalFetched >= maxItems)
                {
                    truncated = true;
                    break;
                }

                // Clone the node to avoid detaching from the source page's JsonArray
                allItems.Add(item.DeepClone());
                totalFetched++;
            }

            if (truncated)
            {
                // 触达预算上限，next_page_token 给出可续 token
                return new PagedFetchResult(
                    allItems, true, nextToken, totalFetched, pagesFetched, null, null);
            }

            if (!hasMore || string.IsNullOrEmpty(nextToken))
            {
                // 全部取完
                break;
            }

            currentPageToken = nextToken;
        }

        return new PagedFetchResult(
            allItems, truncated, truncated ? currentPageToken : null,
            totalFetched, pagesFetched, null, null);
    }

    /// <summary>
    /// 构造聚合信封 JSON：items + has_more + truncated + next_page_token + _budget。
    /// </summary>
    /// <param name="result">聚合结果。</param>
    /// <param name="maxItems">预算上限。</param>
    /// <returns>JSON 信封对象。</returns>
    public static JsonObject BuildEnvelope(PagedFetchResult result, int maxItems)
    {
        var envelope = new JsonObject
        {
            ["items"] = result.Items,
            ["has_more"] = result.Truncated,
            ["total_fetched"] = result.TotalFetched,
            ["pages_fetched"] = result.PagesFetched,
            ["_budget"] = new JsonObject
            {
                ["max"] = maxItems,
                ["kept_items"] = result.TotalFetched,
            },
        };

        if (result.Truncated && !string.IsNullOrEmpty(result.NextPageToken))
        {
            envelope["truncated"] = true;
            envelope["next_page_token"] = result.NextPageToken;
            envelope["truncation_reason"] = "fetch_all 预算上限";
        }
        else if (result.Truncated)
        {
            envelope["truncated"] = true;
            envelope["truncation_reason"] = "fetch_all 页数上限";
        }
        else
        {
            envelope["truncated"] = false;
        }

        if (result.Error is not null)
        {
            envelope["partial_error"] = result.Error;
            if (result.FailedPageIndex.HasValue)
            {
                envelope["failed_page_index"] = result.FailedPageIndex.Value;
            }
        }

        return envelope;
    }

    /// <summary>
    /// 从参数字典读取 <c>fetch_all</c> 参数（默认 false）。
    /// </summary>
    public static bool ReadFetchAll(IReadOnlyDictionary<string, object?> arguments)
        => TryReadBool(arguments, "fetch_all") ?? false;

    /// <summary>
    /// 从参数字典读取 <c>max_items</c> 参数（默认 null → 由 Options 填充）。
    /// </summary>
    public static int? ReadMaxItems(IReadOnlyDictionary<string, object?> arguments)
        => TryReadInt(arguments, "max_items");

    /// <summary>
    /// 解析实际 maxItems：用户传入 → Options 默认 → 硬上限钳制。
    /// </summary>
    public static int ResolveMaxItems(int? userValue, int optionsDefault)
    {
        var resolved = userValue ?? optionsDefault;
        if (resolved < 1)
        {
            resolved = optionsDefault;
        }

        return Math.Min(resolved, HardMaxItems);
    }

    /// <summary>
    /// 解析实际 maxPages：Options 默认 → 硬上限钳制。
    /// </summary>
    public static int ResolveMaxPages(int optionsDefault)
    {
        return Math.Min(Math.Max(optionsDefault, 1), HardMaxPages);
    }

    private static bool? TryReadBool(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            bool b => b,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.True } => true,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.False } => false,
            _ => null,
        };
    }

    private static int? TryReadInt(IReadOnlyDictionary<string, object?> arguments, string key)
    {
        if (!arguments.TryGetValue(key, out var value) || value is null)
        {
            return null;
        }

        return value switch
        {
            int i => i,
            long l => (int)l,
            System.Text.Json.JsonElement { ValueKind: System.Text.Json.JsonValueKind.Number } e
                when e.TryGetInt32(out var i) => i,
            _ => null,
        };
    }
}
