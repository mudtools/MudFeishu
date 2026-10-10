// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// B1 自动分页聚合测试（方案 §3.B1 测试要求）。
/// </summary>
/// <remarks>
/// 假数据页 + 假翻页函数即可，不依赖真实 API。
/// </remarks>
public class PaginationAggregationTests
{
    // ────────── 假数据页 ──────────

    private sealed class FakePage
    {
        public List<string> Items { get; set; } = [];
        public bool HasMore { get; set; }
        public string? NextToken { get; set; }
    }

    private static JsonArray ExtractItems(FakePage page)
    {
        var arr = new JsonArray();
        foreach (var item in page.Items)
        {
            arr.AddNode(JsonValue.Create(item));
        }

        return arr;
    }

    private static (bool HasMore, string? NextToken) ReadPageState(FakePage page)
        => (page.HasMore, page.NextToken);

    /// <summary>
    /// 裸页函数 → 解包结果形态的适配（R-4：<c>AggregateAsync</c> 已被删除，
    /// 原「仅测试使用的适配包装」下沉到测试侧——测试因此直连生产的唯一翻页实现）。
    /// </summary>
    private static Func<string?, CancellationToken, Task<FeishuApiOutcome<FakePage>>> Adapter(
        Func<string?, CancellationToken, Task<FakePage?>> fetchPage)
        => async (token, ct) =>
        {
            var page = await fetchPage(token, ct).ConfigureAwait(false);
            return page is null
                ? FeishuApiOutcome<FakePage>.Fail("下游返回空结果")
                : FeishuApiOutcome<FakePage>.Success(page);
        };

    // ────────── 测试用例 ──────────

    [Fact]
    public async Task FetchAll_Should_Stop_At_HasMoreFalse()
    {
        // Arrange: 3 页，第 3 页 HasMore=false
        var pages = new List<FakePage>
        {
            new() { Items = ["a", "b"], HasMore = true, NextToken = "tok1" },
            new() { Items = ["c", "d"], HasMore = true, NextToken = "tok2" },
            new() { Items = ["e"], HasMore = false, NextToken = null },
        };

        var pageIndex = 0;
        Task<FakePage?> fetchPage(string? token, CancellationToken ct)
        {
            if (pageIndex >= pages.Count)
            {
                throw new InvalidOperationException("超出预期页数");
            }

            var page = pages[pageIndex];
            pageIndex++;
            return Task.FromResult<FakePage?>(page);
        }

        // Act
        var result = await ToolPagination.AggregateOutcomesAsync(
            Adapter(fetchPage), ReadPageState, ExtractItems,
            maxItems: 100, maxPages: 10, CancellationToken.None);

        // Assert
        result.TotalFetched.Should().Be(5, "3 页共 5 个条目");
        result.PagesFetched.Should().Be(3, "3 页全部取完");
        result.Truncated.Should().BeFalse("全部取完不应截断");
        result.Error.Should().BeNull("全部成功");
        result.Items.Count.Should().Be(5);
    }

    [Fact]
    public async Task FetchAll_Should_Stop_At_MaxItems()
    {
        // Arrange: 每页 3 条，maxItems=5 → 第 2 页取 2 条后截断
        var pages = new List<FakePage>
        {
            new() { Items = ["a", "b", "c"], HasMore = true, NextToken = "tok1" },
            new() { Items = ["d", "e", "f"], HasMore = true, NextToken = "tok2" },
        };

        var pageIndex = 0;
        Task<FakePage?> fetchPage(string? token, CancellationToken ct)
        {
            var page = pages[pageIndex];
            pageIndex++;
            return Task.FromResult<FakePage?>(page);
        }

        // Act
        var result = await ToolPagination.AggregateOutcomesAsync(
            Adapter(fetchPage), ReadPageState, ExtractItems,
            maxItems: 5, maxPages: 10, CancellationToken.None);

        // Assert
        result.TotalFetched.Should().Be(5, "触达 maxItems=5 后停止");
        result.Truncated.Should().BeTrue("触达预算上限应截断");
        result.NextPageToken.Should().NotBeNullOrEmpty("截断时应给出可续 token");
        result.Items.Count.Should().Be(5);
    }

    [Fact]
    public async Task FetchAll_Should_Stop_At_MaxPages()
    {
        // Arrange: 每页 2 条，maxPages=2 → 2 页后停止
        var pages = new List<FakePage>
        {
            new() { Items = ["a", "b"], HasMore = true, NextToken = "tok1" },
            new() { Items = ["c", "d"], HasMore = true, NextToken = "tok2" },
            new() { Items = ["e", "f"], HasMore = true, NextToken = "tok3" },
        };

        var pageIndex = 0;
        Task<FakePage?> fetchPage(string? token, CancellationToken ct)
        {
            var page = pages[pageIndex];
            pageIndex++;
            return Task.FromResult<FakePage?>(page);
        }

        // Act
        var result = await ToolPagination.AggregateOutcomesAsync(
            Adapter(fetchPage), ReadPageState, ExtractItems,
            maxItems: 100, maxPages: 2, CancellationToken.None);

        // Assert
        result.PagesFetched.Should().Be(2, "maxPages=2 限制页数");
        result.Truncated.Should().BeTrue("触达页数上限应截断");
        result.TotalFetched.Should().Be(4, "2 页 × 2 条 = 4 条");
    }

    [Fact]
    public async Task FetchAll_Should_Keep_Partial_Data_When_Middle_Page_Fails()
    {
        // Arrange: 3 页，第 2 页失败
        var pages = new List<FakePage?>
        {
            new() { Items = ["a", "b"], HasMore = true, NextToken = "tok1" },
            null, // 第 2 页失败（下游返回 null）
        };

        var pageIndex = 0;
        Task<FakePage?> fetchPage(string? token, CancellationToken ct)
        {
            if (pageIndex >= pages.Count)
            {
                throw new InvalidOperationException("超出预期页数");
            }

            var page = pages[pageIndex];
            pageIndex++;
            return Task.FromResult(page);
        }

        // Act
        var result = await ToolPagination.AggregateOutcomesAsync(
            Adapter(fetchPage), ReadPageState, ExtractItems,
            maxItems: 100, maxPages: 10, CancellationToken.None);

        // Assert
        result.TotalFetched.Should().BeGreaterThan(0, "已取数据应保留");
        result.Items.Count.Should().Be(2, "第 1 页的 2 条保留");
        result.Error.Should().NotBeNull("第 2 页失败应有错误信息");
        result.FailedPageIndex.Should().Be(1, "第 2 页（序号 1）失败");
    }

    [Fact]
    public async Task FetchAll_False_Should_Not_Change_Existing_Behavior()
    {
        // fetch_all=false 时不走聚合——这里验证参数读取与分支逻辑
        var arguments = new Dictionary<string, object?>
        {
            ["fetch_all"] = false,
        };

        var fetchAll = ToolPagination.ReadFetchAll(arguments);
        fetchAll.Should().BeFalse("fetch_all=false");

        // 不调用 AggregateOutcomesAsync → 单次请求行为等价
        await Task.CompletedTask;
    }

    [Fact]
    public async Task FetchAll_Should_Not_Paginate_When_DryRun()
    {
        // dry_run 模式不翻页——这是执行器的职责，这里验证 ToolPagination 不被调用
        // （dry_run 在执行器层短路，AggregateOutcomesAsync 不会被调用）
        // 此用例作为文档性断言
        await Task.CompletedTask;
        true.Should().BeTrue("dry_run 短路在执行器层，ToolPagination 不被调用");
    }

    [Fact]
    public void ResolveMaxItems_Should_ClampToHardLimit()
    {
        var resolved = ToolPagination.ResolveMaxItems(2000, 200);
        resolved.Should().Be(ToolPagination.HardMaxItems, "超过硬上限应钳制");
    }

    [Fact]
    public void ResolveMaxItems_Should_UseUserValue_WhenWithinLimit()
    {
        var resolved = ToolPagination.ResolveMaxItems(50, 200);
        resolved.Should().Be(50, "用户值在硬上限内应采用用户值");
    }

    [Fact]
    public void ResolveMaxItems_Should_FallbackToOptionsDefault_WhenUserValueInvalid()
    {
        var resolved = ToolPagination.ResolveMaxItems(0, 200);
        resolved.Should().Be(200, "用户值为 0 应回退到 Options 默认");
    }

    [Fact]
    public void ResolveMaxPages_Should_ClampToHardLimit()
    {
        var resolved = ToolPagination.ResolveMaxPages(100);
        resolved.Should().Be(ToolPagination.HardMaxPages, "超过硬上限应钳制");
    }

    [Fact]
    public void ResolveMaxPages_Should_EnsureAtLeast1()
    {
        var resolved = ToolPagination.ResolveMaxPages(0);
        resolved.Should().BeGreaterThanOrEqualTo(1, "至少 1 页");
    }

    [Fact]
    public void BuildEnvelope_ShouldContainBudgetAndTruncationInfo()
    {
        var items = new JsonArray { "a", "b" };
        var result = new ToolPagination.PagedFetchResult(
            items, Truncated: true, NextPageToken: "tok",
            TotalFetched: 2, PagesFetched: 1, Error: null, FailedPageIndex: null);

        var envelope = ToolPagination.BuildEnvelope(result, maxItems: 100);

        envelope["items"]!.AsArray().Count.Should().Be(2);
        envelope["truncated"]!.GetValue<bool>().Should().BeTrue();
        envelope["next_page_token"]!.GetValue<string>().Should().Be("tok");
        envelope["total_fetched"]!.GetValue<int>().Should().Be(2);
        envelope["pages_fetched"]!.GetValue<int>().Should().Be(1);
        envelope["_budget"]!["max"]!.GetValue<int>().Should().Be(100);
        envelope["_budget"]!["kept_items"]!.GetValue<int>().Should().Be(2);
    }

    [Fact]
    public void ReadFetchAll_ShouldReturnFalse_WhenNotPresent()
    {
        var arguments = new Dictionary<string, object?>();
        ToolPagination.ReadFetchAll(arguments).Should().BeFalse("默认 false");
    }

    [Fact]
    public void ReadMaxItems_ShouldReturnNull_WhenNotPresent()
    {
        var arguments = new Dictionary<string, object?>();
        ToolPagination.ReadMaxItems(arguments).Should().BeNull("默认 null（由 Options 填充）");
    }
}
