// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Bitable;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// Bitable 分域映射单测（Phase 1 §7）：filter 简化文法 → <c>RecordQueryFilterInfo</c>、
/// page_size 钳制、page_token 透传、<c>code != 0</c> 回填可读错误、白名单投影。
/// </summary>
public class BitableToolsTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableAppTable> _appTableClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableField> _fieldClient = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableRecord> _recordClient = new();

    private BitableTools CreateTools(int maxResultLength = 4000) => new(
        _appTableClient.Object,
        _fieldClient.Object,
        _recordClient.Object,
        Options.Create(new FeishuAgentOptions { Instructions = "test", MaxToolResultLength = maxResultLength }));

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    // ───────────────────── bitable.list_tables ─────────────────────

    [Fact]
    public async Task ListTables_ShouldProjectWhitelist_AndPassPaging()
    {
        _appTableClient
            .Setup(c => c.GetAppTablePageListAsync("bascnXxx", PageSizes.BitableTables, "tok1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<AppTableBaseInfo>
            {
                Code = 0,
                Data = new ApiPageListResult<AppTableBaseInfo>
                {
                    HasMore = true,
                    PageToken = "tok2",
                    Items = [new AppTableBaseInfo { TableId = "tbl001", Name = "采购表", Revision = 3 }],
                },
            });

        var result = await CreateTools().ListTablesAsync(
            Args(("app_token", "bascnXxx"), ("page_token", "tok1")), CancellationToken.None);

        using var document = JsonDocument.Parse(result);
        var root = document.RootElement;
        root.GetProperty("has_more").GetBoolean().Should().BeTrue();
        root.GetProperty("page_token").GetString().Should().Be("tok2", "page_token 透传支撑多页追问");
        var item = root.GetProperty("items")[0];
        item.GetProperty("table_id").GetString().Should().Be("tbl001");
        item.GetProperty("name").GetString().Should().Be("采购表");
        item.GetProperty("revision").GetInt32().Should().Be(3);
        item.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["table_id", "name", "revision"], "字段白名单 table_id/name/revision（§3.3.2）");
    }

    [Fact]
    public async Task ListTables_ShouldBackfillReadableError_WhenCodeNotZero()
    {
        _appTableClient
            .Setup(c => c.GetAppTablePageListAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListResult<AppTableBaseInfo> { Code = 99991663, Msg = "token 无效" });

        var result = await CreateTools().ListTablesAsync(Args(("app_token", "bad")), CancellationToken.None);

        result.Should().StartWith("[tool_error] bitable.list_tables")
            .And.Contain("99991663")
            .And.Contain("token 无效", "code != 0 转可读文本回填模型，非裸异常（总体设计 §4 不变式）");
    }

    // ───────────────────── bitable.list_fields ─────────────────────

    [Fact]
    public async Task ListFields_ShouldProjectWhitelist()
    {
        _fieldClient
            .Setup(c => c.GetFieldsPageListAsync(
                "bascnXxx", "tbl001", It.IsAny<string>(), It.IsAny<bool?>(), PageSizes.BitableFields, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppTableFieldInfo>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppTableFieldInfo>
                {
                    Items = [new AppTableFieldInfo { FieldId = "fld001", FieldName = "状态", Type = 2, IsPrimary = false, UiType = "SingleSelect" }],
                },
            });

        var result = await CreateTools().ListFieldsAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl001")), CancellationToken.None);

        using var document = JsonDocument.Parse(result);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("field_id").GetString().Should().Be("fld001");
        item.GetProperty("name").GetString().Should().Be("状态");
        item.GetProperty("type").GetInt32().Should().Be(2);
    }

    // ───────────────────── bitable.query_records ─────────────────────

    [Fact]
    public async Task QueryRecords_ShouldMapSimplifiedFilterToRecordQueryFilterInfo()
    {
        QueryRecordsRequest? captured = null;
        _recordClient
            .Setup(c => c.QueryRecordsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueryRecordsRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, QueryRecordsRequest request, int ___, string ____, string _____, CancellationToken ______)
                => captured = request)
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppTableRecord>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppTableRecord>
                {
                    Total = 1,
                    Items = [new AppTableRecord { RecordId = "rec001", Fields = new Dictionary<string, object?> { ["状态"] = "done" } }],
                },
            });

        var result = await CreateTools().QueryRecordsAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl001"), ("filter", "状态 = \"done\" and owner contains 张三")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Filter.Should().NotBeNull("filter 简化文法映射为官方过滤结构");
        captured.Filter!.Conjunction.Should().Be("and");
        captured.Filter.Conditions.Should().HaveCount(2);
        captured.Filter.Conditions![0].FieldName.Should().Be("状态");
        captured.Filter.Conditions![0].Operator.Should().Be("is");
        captured.Filter.Conditions![0].Value.Should().Equal(["done"]);
        captured.Filter.Conditions![1].Operator.Should().Be("contains");

        using var document = JsonDocument.Parse(result);
        var item = document.RootElement.GetProperty("items")[0];
        item.GetProperty("record_id").GetString().Should().Be("rec001");
        item.GetProperty("fields").GetProperty("状态").GetString().Should().Be("done");
        document.RootElement.GetProperty("total").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task QueryRecords_ShouldFilterColumns_WhenFieldNamesProvided()
    {
        _recordClient
            .Setup(c => c.QueryRecordsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueryRecordsRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppTableRecord>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppTableRecord>
                {
                    Items =
                    [
                        new AppTableRecord
                        {
                            RecordId = "rec001",
                            Fields = new Dictionary<string, object?>
                            {
                                ["名称"] = "采购单",
                                ["金额"] = "100",
                            },
                        },
                    ],
                },
            });

        var result = await CreateTools().QueryRecordsAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl001"), ("field_names", new[] { "名称" })),
            CancellationToken.None);

        using var document = JsonDocument.Parse(result);
        var fields = document.RootElement.GetProperty("items")[0].GetProperty("fields");
        fields.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            ["名称"], "field_names 列过滤在绑定层落地");
    }

    [Fact]
    public async Task QueryRecords_ShouldRejectInvalidFilter_WithStructuredError_WithoutDownstreamCall()
    {
        var result = await CreateTools().QueryRecordsAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl001"), ("filter", "a = 1 or b = 2")),
            CancellationToken.None);

        result.Should().StartWith("[tool_error] bitable.query_records").And.Contain("filter 语法不支持");
        _recordClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task QueryRecords_ShouldPassPageSizeClamp_AndPageToken()
    {
        int? capturedPageSize = null;
        string? capturedPageToken = null;
        _recordClient
            .Setup(c => c.QueryRecordsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueryRecordsRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, QueryRecordsRequest ___, int pageSize, string pageToken, string ____, CancellationToken _____) =>
            {
                capturedPageSize = pageSize;
                capturedPageToken = pageToken;
            })
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppTableRecord>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppTableRecord> { Items = [] },
            });

        await CreateTools().QueryRecordsAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl001"), ("page_token", "tok7")),
            CancellationToken.None);

        capturedPageSize.Should().Be(PageSizes.BitableRecords, "page_size 为绑定层隐藏参数（默认 20）");
        capturedPageToken.Should().Be("tok7");
    }

    [Fact]
    public async Task QueryRecords_ShouldTruncateOversizedResult_WithMarker()
    {
        _recordClient
            .Setup(c => c.QueryRecordsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueryRecordsRequest>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppTableRecord>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppTableRecord>
                {
                    Items = [new AppTableRecord
                    {
                        RecordId = "rec001",
                        Fields = new Dictionary<string, object?> { ["备注"] = new string('长', 5000) },
                    }],
                },
            });

        var result = await CreateTools().QueryRecordsAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl001")), CancellationToken.None);

        result.Length.Should().BeLessThan(5000);
        result.Should().Contain(ToolResultText.TruncatedMarker, "超 MaxToolResultLength 截断并标记 truncated（防超窗）");
    }

    [Fact]
    public async Task QueryRecords_MissingRequiredArgument_ShouldBackfillStructuredError()
    {
        var result = await CreateTools().QueryRecordsAsync(Args(("app_token", "bascnXxx")), CancellationToken.None);

        result.Should().StartWith("[tool_error] bitable.query_records").And.Contain("table_id");
    }
}
