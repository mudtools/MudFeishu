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
    /// 聚合层截断的<b>统一原因字面量</b>（B-1 口径单源）：触达 <c>max_items</c> 或 <c>max_pages</c>
    /// 时回填给模型与审计的 <c>TruncationReason</c>。具体是哪一个由信封内的
    /// <c>truncation_reason</c> 细化（见 <see cref="BuildEnvelope"/>）。
    /// </summary>
    public const string AggregateTruncationReason = "fetch_all 预算上限";

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
    /// <param name="ApiCode">失败页的飞书业务 code（错误分类消费；非 API 失败为 null）。</param>
    internal sealed record PagedFetchResult(
        JsonArray Items,
        bool Truncated,
        string? NextPageToken,
        int TotalFetched,
        int PagesFetched,
        string? Error,
        int? FailedPageIndex,
        int? ApiCode = null);

    /// <summary>
    /// 自动翻页聚合（<b>解包结果</b>形态，执行器实际使用）：保留失败页的飞书业务 code，
    /// 使"首页即失败"能回到既有的结构化错误分类路径（<c>ToolErrorClassifier.ClassifyCode</c>）。
    /// </summary>
    /// <typeparam name="TPage">单页数据类型（须为 class）。</typeparam>
    /// <param name="fetchPage">翻页函数：接收 pageToken，返回解包结果（<c>Ok=false</c> 视为该页失败）。</param>
    /// <param name="readPageState">从单页结果读取 (HasMore, NextToken)。</param>
    /// <param name="extractItems">从单页结果提取条目列表（JsonArray 形态）。</param>
    /// <param name="maxItems">结果预算上限（条目数）。</param>
    /// <param name="maxPages">页数上限。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>聚合结果。</returns>
    public static async Task<PagedFetchResult> AggregateOutcomesAsync<TPage>(
        Func<string?, CancellationToken, Task<FeishuApiOutcome<TPage>>> fetchPage,
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

            FeishuApiOutcome<TPage> outcome;
            try
            {
                outcome = await fetchPage(currentPageToken, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // 首页即抛异常（尚无任何已取数据）：**原样上抛**——让执行链的分类/重试语义保持完整
                // （429/5xx/超时 → Retryable 可退避；若在此吞成"部分成功"会同时丢掉分类与重试机会）。
                if (pagesFetched == 0)
                {
                    throw;
                }

                // 中途某页失败：保留已取数据并显式回填失败原因（**非静默**——错误文本随
                // PagedFetchResult.Error 透出给调用方，并进结果信封的 partial_error；
                // 本 catch 块含上抛分支，故不触发 SilentCatchContractGuards 的"静默 catch"判定）。
                return new PagedFetchResult(
                    allItems, truncated, currentPageToken, totalFetched, pagesFetched,
                    $"第 {pagesFetched.ToString(CultureInfo.InvariantCulture)} 页获取失败: {ex.Message}",
                    pagesFetched);
            }

            if (outcome is null || !outcome.Ok || outcome.Data is null)
            {
                // 下游返回失败（code != 0 / 网络层吞异常的形态）
                return new PagedFetchResult(
                    allItems, truncated, currentPageToken, totalFetched, pagesFetched,
                    outcome?.ErrorText ?? "下游返回空结果",
                    pagesFetched,
                    outcome?.Code);
            }

            var page = outcome.Data;
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
    /// <remarks>
    /// R-2：取值经唯一门面 <see cref="ToolArgs.OptionalBool"/>（此前本类型自带一份 <c>TryReadBool</c>，
    /// 只认 <c>bool</c> / JSON <c>true|false</c>，而生成的 <c>*Args.Unpack</c> 走 <c>ToolArgs</c>——
    /// 同一入参在两条路径上被判成不同形态，属 S1 同类根因）。
    /// </remarks>
    public static bool ReadFetchAll(IReadOnlyDictionary<string, object?> arguments)
        => ToolArgs.OptionalBool(arguments, "fetch_all") ?? false;

    /// <summary>
    /// 从参数字典读取 <c>max_items</c> 参数（默认 null → 由 Options 填充）。
    /// </summary>
    /// <remarks>
    /// R-2：取值经唯一门面 <see cref="ToolArgs.OptionalInt"/>——语义随之与生成的 <c>*Args.Unpack</c>
    /// 对齐：<b>参数存在但无法解析为整数时抛</b>（由执行链转结构化错误），不再静默降级为
    /// "未提供"。"0 / 非法" 与 "未提供" 是两种语义（见 <see cref="ToolArgs.OptionalInt"/> 的 remarks）。
    /// </remarks>
    public static int? ReadMaxItems(IReadOnlyDictionary<string, object?> arguments)
        => ToolArgs.OptionalInt(arguments, "max_items");

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
}
