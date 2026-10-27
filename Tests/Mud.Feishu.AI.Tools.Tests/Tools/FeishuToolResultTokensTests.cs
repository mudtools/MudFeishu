// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Diagnostics.Metrics;
using Mud.AI.Agent.Conversations;
using Mud.Feishu.Abstractions.Metrics;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// 工具结果成本归因（<c>feishu.tool.result.tokens</c>，E-2 支柱②）——执行链第⑧步的读数口径。
/// </summary>
/// <remarks>
/// 判据是「模型真正读到的量」：取整形之后的文本，且拒绝路径不产生读数（被拒的调用没消耗上下文）。
/// app_key 用作本用例的过滤维度——指标是进程级通道，并行用例会打同一 histogram。
/// </remarks>
public class FeishuToolResultTokensTests
{
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();

    public FeishuToolResultTokensTests()
        => _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(() => new DisposableScope());

    private FeishuToolBinding CreateBinding(
        IToolResultShaper? resultShaper = null,
        IToolExecutionAuthorizer? authorizer = null)
        => new(
            _scopeFactory.Object,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            authorizer,
            resultShaper: resultShaper);

    private static FeishuToolDefinition Definition(string name)
        => new(name, "成本归因工具", ["test:read"], false, FeishuToolRisk.Read, "tenant",
            (_, _, _) => Task.FromResult(ToolResult.FromText("unused")));

    private static IReadOnlyDictionary<string, object?> Args()
        => new Dictionary<string, object?> { ["app_token"] = "bascnXxx" };

    /// <summary>读数等于回填文本的 token 计数，维度只有 tool + app_key。</summary>
    [Fact]
    public async Task Execute_ShouldRecordBackfilledTextTokens_WithToolAndAppKeyDimensions()
    {
        var appKey = Unique("app");
        const string toolName = "cost.read_text";
        const string Backfilled = "多维表格返回了三条记录，含字段与视图";
        var binding = CreateBinding();

        ToolResult? returned = null;
        var measurement = await SingleMeasurementAsync(appKey, async () =>
        {
            returned = await binding.ExecuteAsync(
                Definition(toolName), Args(), new FeishuToolContext(appKey),
                _ => Task.FromResult(ToolResult.FromText(Backfilled)));
        });

        measurement.Value.Should().Be(ChatTokenCounter.Count(returned!.ToString()),
            "口径与模型侧同一编码器，宿主可与之对账");
        measurement.Tool.Should().Be(toolName);
        measurement.AppKey.Should().Be(appKey);
        measurement.TagCount.Should().Be(2,
            "conversation_key/chat_id/user_id 不入 tags——高基数维度会让序列数随会话数线性膨胀");
    }

    /// <summary>宿主整形钩子改写后按<b>改写后</b>文本计数（取整形之前会把成本归因到模型从未收到的字符上）。</summary>
    [Fact]
    public async Task Execute_ShouldRecordTokensOfShapedText_NotDownstreamText()
    {
        var appKey = Unique("app");
        var shaper = new Mock<IToolResultShaper>();
        shaper.Setup(s => s.ShapeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("已收口");

        var binding = CreateBinding(resultShaper: shaper.Object);

        var measurement = await SingleMeasurementAsync(appKey, () => binding.ExecuteAsync(
            Definition("cost.shaped"), Args(), new FeishuToolContext(appKey),
            _ => Task.FromResult(ToolResult.FromText(new string('长', 5_000)))));

        measurement.Value.Should().Be(ChatTokenCounter.Count("已收口"));
        measurement.Value.Should().BeLessThan(ChatTokenCounter.Count(new string('长', 5_000)),
            "整形钩子收缩了回填面，成本读数必须跟着收缩");
    }

    /// <summary>授权拒绝（零下游调用）不产生成本读数——没有文本回填就没有上下文消耗。</summary>
    [Fact]
    public async Task Execute_ShouldNotRecordTokens_WhenCallIsDenied()
    {
        var appKey = Unique("app");
        var denied = new Mock<IToolExecutionAuthorizer>();
        denied
            .Setup(a => a.AuthorizeAsync(
                It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(), It.IsAny<bool>(),
                It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<FeishuToolContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthorizationResult.Deny("租户未开通该工具"));

        var binding = CreateBinding(authorizer: denied.Object);

        var measurements = await CaptureAsync(appKey, () => binding.ExecuteAsync(
            Definition("cost.denied"), Args(), new FeishuToolContext(appKey),
            _ => Task.FromResult(ToolResult.FromText("ok"))));

        measurements.Should().BeEmpty("被拒调用不回填结果，计入成本会虚增用量");
    }

    private readonly record struct ResultTokenMeasurement(double Value, string Tool, string AppKey, int TagCount);

    /// <summary>
    /// 在监听期内执行 <paramref name="emit"/>，只收本用例 app_key 的读数
    /// （指标是进程级通道，并行用例会打同一 histogram，故按维度收口而非按全量断言）。
    /// </summary>
    private static async Task<List<ResultTokenMeasurement>> CaptureAsync(string appKey, Func<Task> emit)
    {
        var seen = new List<ResultTokenMeasurement>();

        using var listener = new MeterListener
        {
            InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Name == "feishu.tool.result.tokens")
                {
                    l.EnableMeasurementEvents(instrument);
                }
            },
        };
        listener.SetMeasurementEventCallback<double>((_, value, tags, _) =>
        {
            var tool = string.Empty;
            var owner = string.Empty;
            var tagCount = 0;
            foreach (var tag in tags)
            {
                tagCount++;
                if (tag.Key == FeishuMetrics.Tags.Tool)
                {
                    tool = tag.Value as string ?? string.Empty;
                }
                else if (tag.Key == FeishuMetrics.Tags.AppKey)
                {
                    owner = tag.Value as string ?? string.Empty;
                }
            }

            if (string.Equals(owner, appKey, StringComparison.Ordinal))
            {
                seen.Add(new ResultTokenMeasurement(value, tool, owner, tagCount));
            }
        });
        listener.Start();
        // 兜底：instrument 是 static readonly，可能早于监听器创建（Published 不保证回放）。
        listener.EnableMeasurementEvents(FeishuMetrics.ToolResultTokens);

        await emit();
        return seen;
    }

    private static async Task<ResultTokenMeasurement> SingleMeasurementAsync(string appKey, Func<Task> emit)
        => (await CaptureAsync(appKey, emit)).Should().ContainSingle("一次调用只产生一条结果读数").Subject;

    private static string Unique(string prefix) => $"{prefix}-{Guid.NewGuid():N}";

    private sealed class DisposableScope : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
