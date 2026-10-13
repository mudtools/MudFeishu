// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

namespace Mud.Feishu.AI.Tools.Tests.Tools.Results;

/// <summary>
/// <c>ToolExecutor.FromPagedApiAsync</c>（统一分页出口）的骨架测试。
/// </summary>
/// <remarks>
/// <para>
/// 与 <see cref="PaginationAggregationTests"/>（只测 <c>ToolPagination</c> 的纯函数）互补：这里是
/// <b>执行器出口的完整路径</b>——单页 / 聚合两条分支、两个重载（约束版 / 页状态显式版）、
/// 以及 <c>untruncated</c> 单页语义开关。
/// </para>
/// <para>
/// <b>为什么必须单独钉</b>：30+ 个分页工具都从这一个方法出行，而它的分支写反
/// （"<c>fetch_all</c> 仍只取一页"或"单页路径被意外截断"）<b>不会让任何既有用例变红</b>——
/// 只会让模型拿到残缺数据，且看起来"很像成功"。
/// </para>
/// </remarks>
public class ToolExecutorPagingTests
{
    /// <summary>执行器出站预算（够用即可；本类只验证分页分支，不验证截断阈值）。</summary>
    private const int MaxResultLength = 4000;

    // ─────────────────────────── 假页 / 假投影 ───────────────────────────

    /// <summary>约束版重载的页类型：真实工具里就是 <c>ApiPageListResult&lt;T&gt;</c>。</summary>
    private static ApiPageListResult<JsonObject> Page(bool hasMore, string? token, params string[] ids)
        => new()
        {
            HasMore = hasMore,
            PageToken = token,
            Items = [.. ids.Select(static id => new JsonObject { ["id"] = id })],
        };

    private static JsonObject Project(ApiPageListResult<JsonObject> page)
    {
        var envelope = ToolResultJsons.PageEnvelope(page.HasMore, page.PageToken);
        foreach (var item in page.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(item);
        }

        return envelope;
    }

    /// <summary>
    /// 「历史 DTO」形态：自带 <c>items</c>/<c>has_more</c>/<c>page_token</c>，但<b>不继承</b>
    /// <c>ApiPageListResult</c>（真实例子：<c>SearchMinutesResult</c>、<c>SearchUserMailboxMessageResult</c>、
    /// <c>GetAuditLogDataResult</c>）。这类 DTO 只能走「页状态显式」重载。
    /// </summary>
    private sealed class StandalonePage
    {
        public List<JsonObject>? Items { get; set; }

        public bool? HasMore { get; set; }

        public string? PageToken { get; set; }

        public int? Total { get; set; }
    }

    private static JsonObject ProjectStandalone(StandalonePage page)
    {
        var envelope = ToolResultJsons.PageEnvelope(page.HasMore, page.PageToken);
        foreach (var item in page.Items ?? [])
        {
            envelope["items"]!.AsArray().AddNode(item);
        }

        envelope["total"] = page.Total;
        return envelope;
    }

    /// <summary>生产里各执行器采用的页状态读法（显式重载的 <c>readPageState</c> 约定）。</summary>
    private static (bool HasMore, string? PageToken) ReadStandalonePageState(StandalonePage page)
        => (page.HasMore == true, page.PageToken);

    private static JsonArray ItemsOf(ToolResult result)
        => JsonNode.Parse(result.Text!)!.AsObject()["items"]!.AsArray();

    private static bool TruncatedOf(ToolResult result)
        => JsonNode.Parse(result.Text!)!.AsObject()["truncated"]!.GetValue<bool>();

    // ─────────────────────────── 约束版重载 ───────────────────────────

    [Fact]
    public async Task ConstrainedOverload_FetchAll_Should_Aggregate_All_Pages()
    {
        var calls = 0;
        var executor = new ToolExecutor("test.paged", MaxResultLength);

        var result = await executor.FromPagedApiAsync<ApiPageListResult<JsonObject>>(
            (token, _) =>
            {
                calls++;
                var page = token switch
                {
                    null => Page(hasMore: true, token: "t2", "a"),
                    "t2" => Page(hasMore: true, token: "t3", "b"),
                    _ => Page(hasMore: false, token: null, "c"),
                };
                return Task.FromResult(ToolApiOutcome<ApiPageListResult<JsonObject>>.Success(page));
            },
            Project,
            fetchAll: true,
            maxItems: null,
            pageToken: null,
            optionsDefaultMaxItems: 200,
            optionsDefaultMaxPages: 10,
            cancellationToken: CancellationToken.None);

        calls.Should().Be(3, "fetch_all=true 必须循环到 has_more=false（3 页）");
        var items = ItemsOf(result);
        items.Count.Should().Be(3, "三页条目必须合并");
        items.Select(static i => i!["id"]!.GetValue<string>()).Should().Equal("a", "b", "c");
        result.Truncated.Should().BeFalse();

        var payload = JsonNode.Parse(result.Text!)!.AsObject();
        payload["total_fetched"]!.GetValue<int>().Should().Be(3);
        payload["pages_fetched"]!.GetValue<int>().Should().Be(3);
        payload["truncated"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task ConstrainedOverload_Should_Fetch_Exactly_One_Page_When_FetchAll_Not_Requested()
    {
        var calls = 0;
        var executor = new ToolExecutor("test.paged", MaxResultLength);

        var result = await executor.FromPagedApiAsync<ApiPageListResult<JsonObject>>(
            (_, _) =>
            {
                calls++;
                return Task.FromResult(ToolApiOutcome<ApiPageListResult<JsonObject>>.Success(
                    Page(hasMore: true, token: "t2", "a")));
            },
            Project,
            fetchAll: false,
            maxItems: null,
            pageToken: null,
            optionsDefaultMaxItems: 200,
            optionsDefaultMaxPages: 10,
            cancellationToken: CancellationToken.None);

        calls.Should().Be(1, "fetch_all 未请求时只允许一次下游调用（P-6 红线：单页路径行为不变）");

        var payload = JsonNode.Parse(result.Text!)!.AsObject();
        payload["items"]!.AsArray().Count.Should().Be(1);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue("单页路径原样透传下游的 has_more");
        payload["page_token"]!.GetValue<string>().Should().Be("t2", "单页路径原样透传可续游标");
        payload.ContainsKey("total_fetched").Should().BeFalse("单页路径不得混入聚合信封字段");
    }

    [Fact]
    public async Task Aggregate_Should_Stop_At_Budget_And_Expose_NextPageToken()
    {
        var calls = 0;
        var executor = new ToolExecutor("test.paged", MaxResultLength);

        var result = await executor.FromPagedApiAsync<ApiPageListResult<JsonObject>>(
            (token, _) =>
            {
                calls++;
                return Task.FromResult(ToolApiOutcome<ApiPageListResult<JsonObject>>.Success(
                    Page(hasMore: true, token: "t2", "a", "b")));
            },
            Project,
            fetchAll: true,
            maxItems: 2,
            pageToken: null,
            optionsDefaultMaxItems: 200,
            optionsDefaultMaxPages: 10,
            cancellationToken: CancellationToken.None);

        calls.Should().Be(1, "预算在首页即被占满 ⇒ 不得再多打一次下游（省一次往返是 fetch_all 的全部意义）");
        var payload = JsonNode.Parse(result.Text!)!.AsObject();
        payload["truncated"]!.GetValue<bool>().Should().BeTrue();
        payload["total_fetched"]!.GetValue<int>().Should().Be(2);
        payload["next_page_token"]!.GetValue<string>().Should().Be("t2", "必须给出可续页游标，模型才能接着取");
    }

    // ─────────────────────────── 页状态显式重载 ───────────────────────────

    [Fact]
    public async Task ExplicitOverload_FetchAll_Should_Aggregate_Standalone_Dto()
    {
        var calls = 0;
        var executor = new ToolExecutor("test.standalone", MaxResultLength);

        var result = await executor.FromPagedApiAsync(
            (token, _) =>
            {
                calls++;
                var page = token is null
                    ? new StandalonePage
                    {
                        Items = [new JsonObject { ["id"] = "a" }],
                        HasMore = true,
                        PageToken = "t2",
                        Total = 2,
                    }
                    : new StandalonePage
                    {
                        Items = [new JsonObject { ["id"] = "b" }],
                        HasMore = false,
                        PageToken = null,
                        Total = 2,
                    };
                return Task.FromResult(ToolApiOutcome<StandalonePage>.Success(page));
            },
            ProjectStandalone,
            ReadStandalonePageState,
            fetchAll: true,
            maxItems: null,
            pageToken: null,
            optionsDefaultMaxItems: 200,
            optionsDefaultMaxPages: 10,
            cancellationToken: CancellationToken.None);

        calls.Should().Be(2, "不继承 ApiPageListResult 的历史 DTO 也必须能聚合翻页");
        ItemsOf(result).Count.Should().Be(2);
        TruncatedOf(result).Should().BeFalse();
    }

    [Fact]
    public async Task ExplicitOverload_Should_Stop_When_HasMore_Is_Null()
    {
        var calls = 0;
        var executor = new ToolExecutor("test.standalone", MaxResultLength);

        // 历史 DTO 的 has_more 是 bool?：null 表示"下游没给结论"。
        // 生产读法（各执行器统一）：`HasMore == true` 才算有下一页 ⇒ null 必须**停下**，
        // 而不是"当作 true 无限翻"或"当作 false 但丢掉已取到的页"。
        var result = await executor.FromPagedApiAsync(
            (_, _) =>
            {
                calls++;
                return Task.FromResult(ToolApiOutcome<StandalonePage>.Success(new StandalonePage
                {
                    Items = [new JsonObject { ["id"] = "a" }],
                    HasMore = null,
                    PageToken = null,
                    Total = 1,
                }));
            },
            ProjectStandalone,
            ReadStandalonePageState,
            fetchAll: true,
            maxItems: null,
            pageToken: null,
            optionsDefaultMaxItems: 200,
            optionsDefaultMaxPages: 10,
            cancellationToken: CancellationToken.None);

        calls.Should().Be(1, "has_more=null（未知）⇒ 停在第 1 页，不得无限翻页");
        ItemsOf(result).Count.Should().Be(1, "已取到的条目必须保留（未知 has_more 不等于丢弃数据）");
    }

    // ─────────────────────────── untruncated 单页开关 ───────────────────────────

    [Fact]
    public async Task SinglePage_Untruncated_Should_Not_Be_Cut_While_Default_Path_Is()
    {
        // 同一份大载荷、同一个预算：唯一变量是 untruncated 开关。
        // ⚠️ 每次调用都必须**新建**页对象：投影会把条目挂进新信封，而 JsonNode 有单亲约束
        //    （复用同一份页对象会抛"The node already has a parent"）。
        //    这正对应生产纪律——投影**每次调用都新建节点**，绝不缓存 JsonNode。
        Task<ToolApiOutcome<StandalonePage>> Fetch(string? _, CancellationToken __)
            => Task.FromResult(ToolApiOutcome<StandalonePage>.Success(new StandalonePage
            {
                Items = [.. Enumerable.Range(0, 60).Select(static i => new JsonObject { ["id"] = $"item-{i}" })],
                HasMore = false,
                PageToken = null,
                Total = 60,
            }));

        var truncating = await new ToolExecutor("test.truncating", 64).FromPagedApiAsync(
            Fetch, ProjectStandalone, ReadStandalonePageState,
            fetchAll: false, maxItems: null, pageToken: null,
            optionsDefaultMaxItems: 200, optionsDefaultMaxPages: 10, cancellationToken: CancellationToken.None);

        var untruncated = await new ToolExecutor("test.untruncated", 64).FromPagedApiAsync(
            Fetch, ProjectStandalone, ReadStandalonePageState,
            fetchAll: false, maxItems: null, pageToken: null,
            optionsDefaultMaxItems: 200, optionsDefaultMaxPages: 10,
            cancellationToken: CancellationToken.None,
            untruncated: true);

        truncating.Truncated.Should().BeTrue("默认单页路径受出站预算约束（maxLength=64 必然截断）");
        untruncated.Truncated.Should().BeFalse("untruncated=true 的单页语义是「不截断」（如 minutes.search 自带逐条窗口）");
    }
}
