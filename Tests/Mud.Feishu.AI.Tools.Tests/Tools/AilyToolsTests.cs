// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.Aily;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Aily「智能伙伴 · 数据知识」域级测试（专题一 B2 补全域，SOP 第 8 步）。
/// </summary>
/// <remarks>
/// <para>
/// 本域的重点不在"能不能取到"，而在三件事：
/// ① **多语言映射取单值**（<c>Label</c>/<c>Description</c> 是 <c>Dictionary</c>，
/// 按 <c>zh_cn → en_us → 第一个</c> 取一个，不铺全语言表）；
/// ② **统一分页**（本域结果继承 <c>ApiPageListResult</c>，走约束版重载）；
/// ③ **域边界**：连接失败原因、条目数等配置事实给，知识库内容不出站。
/// </para>
/// <para>
/// 本类同时承担**分页抽查**职责（方案 §B.8 遗留②的"按域抽查"）：
/// <see cref="ListDataAssets_FetchAll_Should_Aggregate"/> 钉住"继承 <c>ApiPageListResult</c>
/// 的约束版重载在真实消费者上确实跨页聚合"。
/// </para>
/// </remarks>
public class AilyToolsTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static AilyTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>? dataKnowledge = null)
        => new(
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            dataKnowledge?.Object);

    private static JsonObject Payload(ToolResult result)
        => JsonNode.Parse(result.ToString())!.AsObject();

    private static FeishuApiResult<T> Ok<T>(T data) where T : class => new() { Code = 0, Data = data };

    private static DataAsset SampleAsset(string id, string zhLabel, string? enLabel = null, string? connectStatus = "connected")
        => new()
        {
            DataAssetId = id,
            Label = new Dictionary<string, string>
            {
                ["zh_cn"] = zhLabel,
                ["en_us"] = enLabel ?? $"{zhLabel}-en",
            },
            Description = new Dictionary<string, string> { ["zh_cn"] = $"{zhLabel}的描述" },
            DataSourceType = "feishu_wiki",
            ConnectStatus = connectStatus,
            Tags = [new DataAssetTag { DataAssetTagId = "tag_1", Name = "内部制度" }],
            Items = [],
            UpdatedTime = "1760000000",
        };

    /// <summary>fetch_all 必须真的翻页（约束版统一分页出口的真实消费者）。</summary>
    [Fact]
    public async Task ListDataAssets_FetchAll_Should_Aggregate()
    {
        var calls = 0;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>();
        client.Setup(c => c.GetDataAssetPageListAsync(
                It.IsAny<string>(), It.IsAny<DataAssetListQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, DataAssetListQuery? query, CancellationToken __) =>
            {
                calls++;
                return Ok(new DataAssetPageListResult
                {
                    Items = [SampleAsset($"asset_{calls}", $"知识库{calls}")],
                    HasMore = calls == 1,
                    PageToken = calls == 1 ? "t2" : null,
                });
            });

        var result = await CreateTools(client).ListDataAssetsAsync(
            Args(("app_id", "app_1"), ("fetch_all", true)), CancellationToken.None);

        calls.Should().Be(2, "has_more=true 时聚合路径必须继续翻页");

        var payload = Payload(result);
        var items = payload["items"]!.AsArray();
        items.Count.Should().Be(2);
        items[0]!["label"]!.GetValue<string>().Should().Be("知识库1");
        payload["pages_fetched"]!.GetValue<int>().Should().Be(2);
        payload["truncated"]!.GetValue<bool>().Should().BeFalse();
    }

    /// <summary>多语言映射取单值：优先 zh_cn；空库给 found=false + 可执行提示。</summary>
    [Fact]
    public async Task ListDataAssets_Should_Pick_Single_Language_Value()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>();
        client.Setup(c => c.GetDataAssetPageListAsync(
                It.IsAny<string>(), It.IsAny<DataAssetListQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new DataAssetPageListResult
            {
                Items = [SampleAsset("asset_1", "财务制度库", "Finance Docs")],
                HasMore = false,
            }));

        var payload = Payload(await CreateTools(client).ListDataAssetsAsync(
            Args(("app_id", "app_1")), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!;
        item!["label"]!.GetValue<string>().Should().Be("财务制度库", "zh_cn 优先于 en_us");
        item["description"]!.GetValue<string>().Should().Be("财务制度库的描述");
        item["connect_status"]!.GetValue<string>().Should().Be("connected");
        item["tags"]!.AsArray()[0]!.GetValue<string>().Should().Be("内部制度");
    }

    /// <summary>空列表给 found=false + 可执行提示（而不是空数组）。</summary>
    [Fact]
    public async Task ListDataAssets_Empty_Should_Return_FoundSentinel()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>();
        client.Setup(c => c.GetDataAssetPageListAsync(
                It.IsAny<string>(), It.IsAny<DataAssetListQuery?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new DataAssetPageListResult { HasMore = false }));

        var payload = Payload(await CreateTools(client).ListDataAssetsAsync(
            Args(("app_id", "app_1")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeFalse();
        payload["message"]!.GetValue<string>()
            .Should().Contain("keyword", "要提示去掉收窄条件重试，而不是直接下结论");
    }

    /// <summary>get_data_asset：命中给 found=true + 连接事实；未命中给指回来源工具的哨兵。</summary>
    [Fact]
    public async Task GetDataAsset_Should_Project_Detail_Or_NotFound()
    {
        var hit = new Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>();
        hit.Setup(c => c.GetDataAssetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new DataAssetOopsResult
            {
                DataAsset = SampleAsset("asset_1", "财务制度库", connectStatus: "failed"),
            }));

        var payload = Payload(await CreateTools(hit).GetDataAssetAsync(
            Args(("app_id", "app_1"), ("data_asset_id", "asset_1")), CancellationToken.None));

        payload["found"]!.GetValue<bool>().Should().BeTrue();
        payload["connect_status"]!.GetValue<string>().Should().Be("failed");
        payload["notice"]!.GetValue<string>().Should().Contain("knowledge.search", "内容问答必须指路 knowledge 域");

        var miss = new Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>();
        miss.Setup(c => c.GetDataAssetAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(), It.IsAny<bool?>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new DataAssetOopsResult { DataAsset = null }));

        var notFound = Payload(await CreateTools(miss).GetDataAssetAsync(
            Args(("app_id", "app_1"), ("data_asset_id", "asset_ghost")), CancellationToken.None));

        notFound["found"]!.GetValue<bool>().Should().BeFalse();
        notFound["message"]!.GetValue<string>().Should().Contain("list_data_assets", "必须指回取 ID 的那个工具");
    }

    /// <summary>list_data_asset_tags：投影 tag_id/name；空列表给哨兵。</summary>
    [Fact]
    public async Task ListDataAssetTags_Should_Project_Tags()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1AilyDataKnowledge>();
        client.Setup(c => c.GetDataAssetTagPageListAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string[]?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Ok(new DataAssetTagPageListResult
            {
                Items = [new DataAssetTag { DataAssetTagId = "tag_1", Name = "内部制度" }],
                HasMore = false,
            }));

        var payload = Payload(await CreateTools(client).ListDataAssetTagsAsync(
            Args(("app_id", "app_1")), CancellationToken.None));

        var item = payload["items"]!.AsArray()[0]!;
        item!["data_asset_tag_id"]!.GetValue<string>().Should().Be("tag_1");
        item["name"]!.GetValue<string>().Should().Be("内部制度");
    }

    /// <summary>软缺席：必须给结构化错误，不得抛异常。</summary>
    [Fact]
    public async Task Missing_Client_Should_Return_Structured_Error()
    {
        var tools = CreateTools();

        foreach (var result in new[]
        {
            await tools.ListDataAssetsAsync(Args(("app_id", "app_1")), CancellationToken.None),
            await tools.GetDataAssetAsync(Args(("app_id", "app_1"), ("data_asset_id", "a")), CancellationToken.None),
            await tools.ListDataAssetTagsAsync(Args(("app_id", "app_1")), CancellationToken.None),
        })
        {
            result.Text.Should().Contain("未注册", "文案要说清是宿主未启用该 API，而不是模型参数错");
            result.Text.Should().Contain("tool_error", "必须是结构化错误（第一行紧凑 JSON）");
        }
    }
}
