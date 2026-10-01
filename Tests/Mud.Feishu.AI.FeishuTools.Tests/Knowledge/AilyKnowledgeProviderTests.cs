// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Text;
using Mud.Feishu.AI.FeishuTools.Knowledge;
using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.AI.FeishuTools.Tests.Knowledge;

/// <summary>
/// Aily 托管知识问答提供器测试（RAG-A，Phase 2 §3.4）：SSE 事件投影（答案 + chunks）、
/// FAQ 命中回退、租户上下文缺失拒绝、HTTP 200 + JSON 错误体识别、IRetriever 检索面。
/// </summary>
public class AilyKnowledgeProviderTests : IDisposable
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge> _knowledgeClient = new();
    private readonly Mock<IFeishuAppContextScopeFactory> _scopeFactory = new();
    private readonly FeishuToolContextAccessor _contextAccessor = new();
    private readonly List<HttpResponseMessage> _responses = [];

    public AilyKnowledgeProviderTests()
    {
        _scopeFactory
            .Setup(f => f.BeginScope(It.IsAny<string>()))
            .Returns(new ScopedRelease());
    }

    public void Dispose()
    {
        foreach (var response in _responses)
        {
            response.Dispose();
        }
    }

    private sealed class ScopedRelease : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private AilyKnowledgeProvider CreateProvider(AilyKnowledgeOptions? options = null)
        => new(
            _knowledgeClient.Object,
            _scopeFactory.Object,
            Options.Create(options ?? new AilyKnowledgeOptions { AppId = "spring_demo__c" }),
            _contextAccessor);

    private void SetupSseResponse(string sseBody, string mediaType = "text/event-stream")
    {
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(sseBody, Encoding.UTF8, mediaType),
        };
        _responses.Add(response);
        _knowledgeClient
            .Setup(c => c.AskDataKnowledgeAsync(
                It.IsAny<string>(), It.IsAny<AskDataKnowledgeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);
    }

    private const string QaSse =
        "data: {\"status\":\"processing\",\"message\":{\"content\":\"正在检索知识...\"}}\n"
        + "\n"
        + "data: {\"status\":\"finished\",\"finish_type\":\"qa\",\"message\":{\"content\":\"采购流程共 3 步。\"},"
        + "\"process_data\":{\"chunks\":[\"切片一：采购申请\",\"切片二：采购审批\"],\"sql_data\":[]},\"has_answer\":true}\n";

    [Fact]
    public async Task Ask_ShouldParseSse_AndCarryChunks()
    {
        using var _scope = _contextAccessor.Begin(new FeishuToolContext("appA"));
        SetupSseResponse(QaSse);

        var answer = await CreateProvider().AskAsync("采购流程是什么？");

        answer.HasAnswer.Should().BeTrue();
        answer.AnswerText.Should().Be("采购流程共 3 步。", "以最后出现的非空 content 为准（processing 中间事件被覆盖）");
        answer.Chunks.Should().HaveCount(2, "召回切片随 finished 事件返回（引用回链来源）");
        answer.Chunks[0].Text.Should().Be("切片一：采购申请");
    }

    /// <summary>
    /// R3-6①：<c>chunks</c> 中出现非字符串元素（数字 / 布尔 / 对象 / 数组）时必须降级为文本，
    /// 而不是让 <c>JsonElement.GetString()</c> 抛 <see cref="InvalidOperationException"/>——否则
    /// 一条脏数据会让<b>整次召回</b>（含已解析的 answer）一起失败。
    /// </summary>
    [Fact]
    public async Task Ask_ShouldSkipNonStringChunk_WithoutLosingOthers()
    {
        using var _scope = _contextAccessor.Begin(new FeishuToolContext("appA"));
        SetupSseResponse(
            "data: {\"status\":\"finished\",\"message\":{\"content\":\"答案\"},"
            + "\"process_data\":{\"chunks\":[\"切片一\",123,{\"nested\":\"v\"},\"切片二\"]},\"has_answer\":true}\n");

        var answer = await CreateProvider().AskAsync("问题");

        answer.HasAnswer.Should().BeTrue();
        answer.Chunks.Should().HaveCount(4, "非字符串元素降级为文本，不得整条丢弃也不得抛异常");
        answer.Chunks[0].Text.Should().Be("切片一");
        answer.Chunks[1].Text.Should().Be("123", "数字元素按原始 JSON 文本降级");
        answer.Chunks[2].Text.Should().Be("{\"nested\":\"v\"}", "对象元素按原始 JSON 文本降级");
        answer.Chunks[3].Text.Should().Be("切片二", "脏数据之后的切片必须仍然保留");
    }

    [Fact]
    public async Task Ask_ShouldMapRequestOptions_AndUseConfiguredAppId()
    {
        using var _scope = _contextAccessor.Begin(new FeishuToolContext("appA"));
        AskDataKnowledgeRequest? captured = null;
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(QaSse, Encoding.UTF8, "text/event-stream"),
        };
        _responses.Add(response);
        _knowledgeClient
            .Setup(c => c.AskDataKnowledgeAsync("spring_fixed__c", It.IsAny<AskDataKnowledgeRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, AskDataKnowledgeRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(response);

        var provider = new AilyKnowledgeProvider(
            _knowledgeClient.Object,
            _scopeFactory.Object,
            Options.Create(new AilyKnowledgeOptions
            {
                AppId = "spring_fixed__c",
                DataAssetIds = ["asset_a"],
                DataAssetTagIds = ["tag_b"],
            }),
            _contextAccessor);
        await provider.AskAsync("问题");

        captured.Should().NotBeNull();
        captured!.Message.Content.Should().Be("问题");
        captured.DataAssetIds.Should().Equal(["asset_a"], "AilyKnowledgeOptions.DataAssetIds 消费点（R5）");
        captured.DataAssetTagIds.Should().Equal(["tag_b"], "AilyKnowledgeOptions.DataAssetTagIds 消费点（R5）");
    }

    [Fact]
    public async Task Ask_ShouldFallbackToFaqAnswer_WhenFaqHitWithoutChunks()
    {
        using var _scope = _contextAccessor.Begin(new FeishuToolContext("appA"));
        SetupSseResponse(
            "data: {\"status\":\"finished\",\"finish_type\":\"faq\",\"faq_result\":{\"question\":\"年假几天\",\"answer\":\"法定 5 天\"},\"has_answer\":true}\n");

        var answer = await CreateProvider().AskAsync("年假几天？");

        answer.HasAnswer.Should().BeTrue();
        answer.AnswerText.Should().Be("法定 5 天");
    }

    [Fact]
    public async Task Retrieve_ShouldFallbackAnswerAsChunk_ForFaqHit()
    {
        using var _scope = _contextAccessor.Begin(new FeishuToolContext("appA"));
        SetupSseResponse(
            "data: {\"status\":\"finished\",\"finish_type\":\"faq\",\"faq_result\":{\"question\":\"年假几天\",\"answer\":\"法定 5 天\"},\"has_answer\":true}\n");

        var chunks = await CreateProvider().RetrieveAsync("年假几天？");

        chunks.Should().ContainSingle().Which.Source.Should().Be("aily:faq", "FAQ 命中无 chunks 时答案文本即知识单元");
    }

    [Fact]
    public async Task Ask_ShouldThrowWithoutContext_MaintainingTenantIsolation()
    {
        // 未 Begin 上下文（模拟绕过事件入口直调）。
        SetupSseResponse(QaSse);

        var act = async () => await CreateProvider().AskAsync("问题");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*appKey*", "多租户隔离禁止默认应用兜底（TMA2-20）");
    }

    [Fact]
    public async Task Ask_ShouldDetectJsonErrorBody_OnHttp200()
    {
        using var _scope = _contextAccessor.Begin(new FeishuToolContext("appA"));
        SetupSseResponse("{\"code\":2700034,\"msg\":\"问答被禁用\"}", mediaType: "application/json");

        var act = async () => await CreateProvider().AskAsync("问题");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*2700034*", "HTTP 200 + JSON 错误体残余风险须转可读异常（documents/ErrorHandling.md）");
    }

    [Fact]
    public void Options_ShouldFailFast_WhenAppIdMissing()
    {
        var act = () => new AilyKnowledgeOptions { AppId = string.Empty }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*AppId*");
    }

    /// <summary>
    /// R4-3：<c>has_answer</c> 必须<b>单调累积</b>——末事件省略该字段时，不得把先前事件的真值覆盖为 false。
    /// </summary>
    /// <remarks>
    /// SSE 的 processing/中间事件普遍<b>省略</b> <c>has_answer</c>，而 <c>status=finished</c> 的终结事件也不保证携带。
    /// 覆盖式赋值会让"明确有答案"被最后一次无字段事件重置为 false ⇒ <see cref="KnowledgeAnswer.HasAnswer"/> 假阴性，
    /// 消费点据此丢弃整次召回（静默丢失，无异常、无日志）。本条为端到端用例（经 <c>AskAsync</c>），
    /// 因 <c>ParseSseBody</c> 是 private static 不可直测（见方案 §5.1 可行性约束①）。
    /// </remarks>
    [Fact]
    public async Task Ask_ShouldAccumulateHasAnswer_WhenLastEventOmitsField()
    {
        using var _scope = _contextAccessor.Begin(new FeishuToolContext("appA"));
        SetupSseResponse(
            "data: {\"status\":\"processing\",\"message\":{\"content\":\"正在检索知识...\"},\"has_answer\":true}\n"
            + "\n"
            + "data: {\"status\":\"finished\",\"finish_type\":\"qa\",\"message\":{\"content\":\"采购流程共 3 步。\"},"
            + "\"process_data\":{\"chunks\":[\"切片一：采购申请\"],\"sql_data\":[]}}\n");

        var answer = await CreateProvider().AskAsync("采购流程是什么？");

        answer.AnswerText.Should().Be("采购流程共 3 步。");
        answer.HasAnswer.Should().BeTrue("末事件省略 has_answer 不得覆盖先前事件的真值（R4-3：单调累积而非覆盖）");
        answer.Chunks.Should().ContainSingle();
    }
}
