// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Messages;
using Mud.Feishu.DataModels.Search;
using Mud.Feishu.DataModels.Spreadsheets;
using Mud.Feishu.DataModels.Wiki;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// Search/IM/Wiki/Docx/Sheets 分域映射单测（Phase 1 §7）：
/// search_in 扁平化、RFC3339 时间戳转换、绑定层注入参数、白名单投影与截断。
/// </summary>
public class DomainMappingTests
{
    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ───────────────────── search.doc_wiki ─────────────────────

    [Fact]
    public async Task Search_SearchInDoc_ShouldBuildDocFilterOnly()
    {
        SearchDocWikiRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>();
        client
            .Setup(c => c.SearchDocWikiAsync(It.IsAny<SearchDocWikiRequest>(), It.IsAny<CancellationToken>()))
            .Callback((SearchDocWikiRequest request, CancellationToken _) => captured = request)
            .ReturnsAsync(new FeishuApiResult<SearchDocWikiResult>
            {
                Code = 0,
                Data = new SearchDocWikiResult
                {
                    Total = 1,
                    ResUnits = [new DocResUnit
                    {
                        TitleHighlighted = "采购<em>流程</em>",
                        EntityType = "DOC",
                        ResultMeta = new DocMeta { Url = "https://example.feishu.cn/docs/x", OwnerName = "张三", Token = "doxcn001" },
                    }],
                },
            });

        var tools = new SearchTools(client.Object, Options.Create(NewOptions()));
        var result = await tools.SearchAsync(
            Args(("query", "采购流程"), ("search_in", "doc"), ("folder_tokens", new[] { "fldA" })),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.DocFilter.Should().NotBeNull("search_in=doc 仅下发 DocFilter（官方「两 filter 至少一个」约束）");
        captured.DocFilter!.FolderTokens.Should().Equal(["fldA"]);
        captured.WikiFilter.Should().BeNull();
        captured.PageSize.Should().Be(PageSizes.Search, "page_size 为绑定层钳制的隐藏参数");
        captured.Query.Should().Be("采购流程");

        using var document = JsonDocument.Parse(result.ToString()!);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("title").GetString().Should().Contain("采购");
        item.GetProperty("url").GetString().Should().Contain("docs");
        item.GetProperty("owner").GetString().Should().Be("张三");
        item.GetProperty("doc_type").GetString().Should().Be("DOC");
        item.GetProperty("token").GetString().Should().Be("doxcn001", "token 供 docx.get_raw_content 两步链使用");
    }

    [Fact]
    public async Task Search_SearchInWikiOrBoth_ShouldBuildWikiFilter()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>();
        var captured = new List<SearchDocWikiRequest>();
        client
            .Setup(c => c.SearchDocWikiAsync(It.IsAny<SearchDocWikiRequest>(), It.IsAny<CancellationToken>()))
            .Callback((SearchDocWikiRequest request, CancellationToken _) => captured.Add(request))
            .ReturnsAsync(new FeishuApiResult<SearchDocWikiResult>
            {
                Code = 0,
                Data = new SearchDocWikiResult { ResUnits = [] },
            });

        var tools = new SearchTools(client.Object, Options.Create(NewOptions()));

        await tools.SearchAsync(Args(("query", "q"), ("search_in", "wiki"), ("space_ids", new[] { "sp1" })), CancellationToken.None);
        await tools.SearchAsync(Args(("query", "q"), ("search_in", "both")), CancellationToken.None);

        captured[0].WikiFilter.Should().NotBeNull();
        captured[0].WikiFilter!.SpaceIds.Should().Equal(["sp1"]);
        captured[0].DocFilter.Should().BeNull();
        captured[1].DocFilter.Should().NotBeNull("both 两者都下发");
        captured[1].WikiFilter.Should().NotBeNull("both 两者都下发（空过滤对象）");
    }

    [Fact]
    public async Task Search_InvalidSearchIn_ShouldBackfillStructuredError()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>();
        var tools = new SearchTools(client.Object, Options.Create(NewOptions()));

        var result = await tools.SearchAsync(Args(("query", "q"), ("search_in", "all")), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] search.doc_wiki").And.Contain("doc/wiki/both");
        client.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Search_QueryOverThirtyChars_ShouldBackfillStructuredError()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2SearchDocWiki>();
        var tools = new SearchTools(client.Object, Options.Create(NewOptions()));

        var result = await tools.SearchAsync(Args(("query", new string('长', 31))), CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] search.doc_wiki").And.Contain("30");
    }

    // ───────────────────── im.get_history_messages ─────────────────────

    [Fact]
    public async Task ImHistory_ShouldConvertRfc3339ToUnixSeconds_AndInjectHiddenParams()
    {
        string? capturedStart = null;
        string? capturedEnd = null;
        string? capturedContainerType = null;
        string? capturedSortType = null;
        int? capturedPageSize = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        client
            .Setup(c => c.GetHistoryMessageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string containerType, string _, string start, string end, string sortType, int pageSize, string __, CancellationToken ___) =>
            {
                capturedContainerType = containerType;
                capturedStart = start;
                capturedEnd = end;
                capturedSortType = sortType;
                capturedPageSize = pageSize;
            })
            .ReturnsAsync(new FeishuApiPageListResult<HistoryMessageData>
            {
                Code = 0,
                Data = new ApiPageListResult<HistoryMessageData>
                {
                    Items = [new HistoryMessageData
                    {
                        MessageId = "om_1",
                        CreateTime = "1758902400000",
                        Sender = new MessageSender { Id = "ou_sender" },
                        MsgType = "text",
                        Body = new MessageBody { Content = "{\"text\":\"你好\"}" },
                    }],
                },
            });

        var tools = new ImTools(client.Object, new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object, Options.Create(NewOptions()));
        var result = await tools.GetHistoryAsync(
            Args(("chat_id", "oc001"), ("start_time", "2026-09-27T00:00:00+08:00"), ("end_time", "2026-09-27T12:00:00Z")),
            CancellationToken.None);

        capturedContainerType.Should().Be("chat", "container_id_type 固定 chat（绑定层注入，隐藏）");
        capturedSortType.Should().Be("ByCreateTimeDesc", "取最近消息（绑定层注入，隐藏）");
        capturedPageSize.Should().Be(PageSizes.History);
        capturedStart.Should().Be("1790438400", "RFC3339 → 秒级时间戳（§3.3.3）");
        capturedEnd.Should().Be("1790510400");

        using var document = JsonDocument.Parse(result.ToString()!);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("message_id").GetString().Should().Be("om_1");
        item.GetProperty("sender_id").GetString().Should().Be("ou_sender");
        item.GetProperty("message_type").GetString().Should().Be("text");
        item.GetProperty("content").GetString().Should().Contain("你好");
    }

    [Fact]
    public async Task ImHistory_InvalidRfc3339_ShouldBackfillStructuredError()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        var tools = new ImTools(client.Object, new Mock<Mud.Feishu.IFeishuTenantV1ChatGroupMember>().Object, Options.Create(NewOptions()));

        var result = await tools.GetHistoryAsync(
            Args(("chat_id", "oc001"), ("start_time", "2026/09/27 00:00")),
            CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] im.get_history_messages").And.Contain("RFC3339");
        client.VerifyNoOtherCalls();
    }

    // ───────────────────── wiki.* ─────────────────────

    [Fact]
    public async Task WikiGetNode_ShouldProjectNodeWhitelist_AndDefaultObjType()
    {
        string? capturedObjType = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV2WikiNodes>();
        client
            .Setup(c => c.GetNodeSpaceInfoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string objType, CancellationToken __) => capturedObjType = objType)
            .ReturnsAsync(new FeishuApiResult<SpaceNodeResult>
            {
                Code = 0,
                Data = new SpaceNodeResult
                {
                    Node = new SpaceNodeInfo
                    {
                        NodeToken = "wikcnNode",
                        Title = "采购知识",
                        ObjType = "docx",
                        ObjToken = "doxcnDoc",
                        ParentNodeToken = "wikcnParent",
                        HasChild = false,
                    },
                },
            });

        var tools = new WikiTools(client.Object, Options.Create(NewOptions()));
        var result = await tools.GetNodeAsync(Args(("token", "wikcnNode")), CancellationToken.None);

        capturedObjType.Should().Be("wiki", "obj_type 默认 wiki（§3.3.2）");
        using var document = JsonDocument.Parse(result.ToString()!);
        var node = document.RootElement.GetProperty("node");
        node.GetProperty("node_token").GetString().Should().Be("wikcnNode");
        node.GetProperty("obj_token").GetString().Should().Be("doxcnDoc", "obj_token 供 docx.get_raw_content 两步链使用");
        node.GetProperty("title").GetString().Should().Be("采购知识");
        node.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["node_token", "title", "obj_type", "obj_token", "parent_node_token", "has_child"]);
    }

    [Fact]
    public async Task WikiListNodes_ShouldPassPaging()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV2WikiNodes>();
        client
            .Setup(c => c.GetSpaceNodesPageListAsync("sp1", "wikcnParent", PageSizes.WikiNodes, "tok1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<SpaceNodeInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<SpaceNodeInfo>
                {
                    HasMore = false,
                    Items = [new SpaceNodeInfo { NodeToken = "wikcnChild", Title = "子节点", ObjType = "docx" }],
                },
            });

        var tools = new WikiTools(client.Object, Options.Create(NewOptions()));
        var result = await tools.ListNodesAsync(
            Args(("space_id", "sp1"), ("parent_node_token", "wikcnParent"), ("page_token", "tok1")),
            CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("items")[0].GetProperty("node_token").GetString().Should().Be("wikcnChild");
    }

    // ───────────────────── docx.get_raw_content ─────────────────────

    [Fact]
    public async Task DocxRawContent_ShouldReturnPlainText_TruncatedWhenOversized()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1Docx>();
        client
            .Setup(c => c.GetDocumentRawContentAsync("doxcn001", It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<Mud.Feishu.DataModels.Docx.DocumentRawContentResult>
            {
                Code = 0,
                Data = new Mud.Feishu.DataModels.Docx.DocumentRawContentResult { Content = new string('文', 600) },
            });

        var tools = new DocxTools(client.Object, Options.Create(NewOptions(maxResultLength: 200)));
        var result = await tools.GetRawContentAsync(Args(("document_id", "doxcn001")), CancellationToken.None);

        result.ToString().Should().Contain(ToolResultText.TruncatedMarker, "正文按 MaxToolResultLength 截断并标记");
    }

    // ───────────────────── sheets.* ─────────────────────

    [Fact]
    public async Task SheetsListSheets_ShouldProjectWhitelist()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV3Spreadsheets>();
        client
            .Setup(c => c.GetSpreadsheetSheetsByTokenAsync("shtcn001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetSpreadsheetSheetsResult>
            {
                Code = 0,
                Data = new GetSpreadsheetSheetsResult
                {
                    Sheets = [new SheetDetailInfo { SheetId = "ShtXxx", Title = "价格表", Index = 0 }],
                },
            });

        var tools = new SheetsTools(client.Object, new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetData>().Object, Options.Create(NewOptions()));
        var result = await tools.ListSheetsAsync(Args(("spreadsheet_token", "shtcn001")), CancellationToken.None);

        using var document = JsonDocument.Parse(result.ToString()!);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("sheet_id").GetString().Should().Be("ShtXxx");
        item.GetProperty("title").GetString().Should().Be("价格表");
        item.GetProperty("index").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task SheetsGetRangeValues_ShouldPassRangeAndRenderOption_AndProjectGrid()
    {
        string? capturedRenderOption = null;
        var client = new Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetData>();
        client
            .Setup(c => c.GetRangeDataAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string renderOption, string ___, string ____, CancellationToken _____) => capturedRenderOption = renderOption)
            .ReturnsAsync(new FeishuApiResult<GetRangeDataResult>
            {
                Code = 0,
                Data = new GetRangeDataResult
                {
                    ValueRange = new RangeValuesInfo
                    {
                        Range = "ShtXxx!A1:B2",
                        Values = [["品名", "价格"], ["键盘", 199]],
                    },
                },
            });

        var tools = new SheetsTools(
            new Mock<Mud.Feishu.IFeishuTenantV3Spreadsheets>().Object,
            client.Object,
            Options.Create(NewOptions()));
        var result = await tools.GetRangeValuesAsync(
            Args(("spreadsheet_token", "shtcn001"), ("range", "ShtXxx!A1:B2"), ("value_render_option", "ToString")),
            CancellationToken.None);

        capturedRenderOption.Should().Be("ToString");
        using var document = JsonDocument.Parse(result.ToString()!);
        document.RootElement.GetProperty("range").GetString().Should().Be("ShtXxx!A1:B2", "range 直通");
        var row = document.RootElement.GetProperty("values")[1];
        row[0].GetString().Should().Be("键盘");
        row[1].GetInt32().Should().Be(199);
    }

    private static FeishuAgentOptions NewOptions(int maxResultLength = 4000) => new()
    {
        Instructions = "test",
        MaxToolResultLength = maxResultLength,
    };
}
