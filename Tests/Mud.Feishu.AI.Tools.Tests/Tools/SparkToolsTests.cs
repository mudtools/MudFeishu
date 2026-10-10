// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Internal;
using Mud.Feishu.DataModels.Spark;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// Spark 妙搭域工具（R7 / A6）：10 个工具的请求映射、upsert→Prefer 请求头、filter 必填与分页。
/// </summary>
/// <remarks>
/// <para>
/// <b>本域最容易错的三点</b>：① <c>table_id</c> 实际是<b>表名</b>（table_name）；
/// ② <c>upsert</c> 在官方 SDK 里<b>没有 DTO 字段</b>，只能经请求头
/// <c>Prefer: resolution=merge-duplicates</c> 表达（用例直接断言请求头）；
/// ③ <c>update_table_records</c> 的 <c>filter</c> 必填（无条件 PATCH 会更新全表）。
/// </para>
/// <para>
/// 第二组断言是「可见范围」的<b>严格解析</b>：未知键一律拒绝——静默丢弃会让模型以为设置已生效。
/// </para>
/// </remarks>
public class SparkToolsTests
{
    private static FeishuAgentOptions AgentOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static SparkAppTools CreateAppTools(
        Mock<Mud.Feishu.IFeishuTenantV1SparkApp>? tenantApp = null,
        Mock<Mud.Feishu.IFeishuUserV1SparkApp>? userApp = null)
        => new(Options.Create(AgentOptions()), tenantApp?.Object, userApp?.Object);

    private static SparkTableTools CreateTableTools(Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>? table = null)
        => new(Options.Create(AgentOptions()), table?.Object);

    // ───────────────────── 契约面 ─────────────────────

    [Fact]
    public void ListApps_Should_Be_Tenant_Read()
    {
        var contract = FeishuToolContracts.ByToolName[FeishuToolNames.SparkListApps];

        contract.Identity.Should().Be("tenant", "spark.list_apps 映射租户令牌客户端（IFeishuTenantV1SparkApp）");
        contract.IsWrite.Should().BeFalse();
        contract.RequiredScopes.Should().BeEquivalentTo(new[] { "spark:app:readonly" });
    }

    [Fact]
    public void CreateApp_Should_Be_Write_And_Require_Authorizer()
    {
        var contract = FeishuToolContracts.ByToolName[FeishuToolNames.SparkCreateApp];

        contract.IsWrite.Should().BeTrue("创建应用属写面：须 WriteAllowList + 授权门禁");
        contract.Identity.Should().Be("user");
        FeishuToolNames.IsWriteTool(FeishuToolNames.SparkCreateApp).Should().BeTrue();
    }

    [Fact]
    public void EveryWriteTool_Should_ExposeDryRun()
    {
        foreach (var toolName in new[]
                 {
                     FeishuToolNames.SparkCreateApp,
                     FeishuToolNames.SparkPatchApp,
                     FeishuToolNames.SparkUpdateAppVisibility,
                     FeishuToolNames.SparkAddTableRecords,
                     FeishuToolNames.SparkUpdateTableRecords,
                 })
        {
            FeishuToolSchemas.SchemaByToolName[toolName].Should().Contain(
                "dry_run", $"{toolName} 是写工具，必须可预演（DryRunContractGuards 的契约要求）");
        }
    }

    [Fact]
    public void PaginatedTools_Should_Expose_FetchAll_InsteadOf_PageSize()
    {
        foreach (var toolName in new[]
                 {
                     FeishuToolNames.SparkListApps,
                     FeishuToolNames.SparkListTables,
                     FeishuToolNames.SparkQueryTableRecords,
                 })
        {
            var schema = FeishuToolSchemas.SchemaByToolName[toolName];
            schema.Should().Contain("fetch_all", $"{toolName} 是分页工具（B1：预算进）");
            schema.Should().Contain("max_items");
            schema.Should().NotContain("\"page_size\"", $"{toolName} 不得暴露 page_size（B1：页不进）");
        }
    }

    // ───────────────────── 应用面 ─────────────────────

    [Fact]
    public async Task GetAppAnalytics_Should_Pass_TimeRange()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1SparkApp>();
        client.Setup(c => c.GetAppAnalyticsOverviewAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetAnalyticsOverviewResult>
            {
                Code = 0,
                Data = new GetAnalyticsOverviewResult
                {
                    ActiveUsers = new AnalyticsMetric { Value = "12", Diff = "+2", Ratio = 0.2 },
                },
            });

        var result = await CreateAppTools(tenantApp: client).GetAppAnalyticsOverviewAsync(
            Args(("app_id", "app_1"), ("start_time", "1705312800"), ("end_time", "1705399200")),
            CancellationToken.None);

        result.ToString().Should().Contain("\"value\":\"12\"");
        client.Verify(
            c => c.GetAppAnalyticsOverviewAsync("app_1", "1705312800", "1705399200", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateApp_DryRun_Should_Not_Call_Downstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkApp>();

        var result = await CreateAppTools(userApp: client).CreateAppAsync(
            Args(("name", "周报助手"), ("desc", "自动生成周报"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[dry_run]");
        text.Should().Contain("/open-apis/spark/v1/apps");
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateApp_Should_Pass_Name_And_Description()
    {
        CreateAppRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkApp>();
        client.Setup(c => c.CreateAppAsync(It.IsAny<CreateAppRequest>(), It.IsAny<CancellationToken>()))
            .Callback<CreateAppRequest, CancellationToken>((r, _) => captured = r)
            .ReturnsAsync(new FeishuApiResult<AppResult>
            {
                Code = 0,
                Data = new AppResult { App = new App { AppId = "app_1", Name = "周报助手" } },
            });

        var result = await CreateAppTools(userApp: client).CreateAppAsync(
            Args(("name", "周报助手"), ("desc", "自动生成周报")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("周报助手");
        captured.Description.Should().Be("自动生成周报");
        result.ToString().Should().Contain("\"app_id\":\"app_1\"", "创建成功后必须回填 app_id（闭环首步的产物）");
    }

    [Fact]
    public async Task PatchApp_WithoutAnyField_Should_Return_StructuredError()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkApp>();

        var result = await CreateAppTools(userApp: client).PatchAppAsync(
            Args(("app_id", "app_1")), CancellationToken.None);

        result.ToString().Should().Contain("至少需要提供 name / desc / icon_url 之一");
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAppVisibility_RangeWithoutSubjects_Should_Reject()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkApp>();

        var result = await CreateAppTools(userApp: client).UpdateAppVisibilityAsync(
            Args(("app_id", "app_1"), ("visibility_json", "{\"scope\":\"Range\"}")),
            CancellationToken.None);

        result.ToString().Should().Contain("scope=Range 时 users / departments / chats 至少提供一个");
        client.Invocations.Should().BeEmpty("语义不完整的可用范围不得下发（避免「无人可用」的静默结果）");
    }

    [Fact]
    public async Task UpdateAppVisibility_Should_Reject_Unknown_Keys()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkApp>();

        var result = await CreateAppTools(userApp: client).UpdateAppVisibilityAsync(
            Args(("app_id", "app_1"), ("visibility_json", "{\"scope\":\"Tenant\",\"ghost\":true}")),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("未知字段 'ghost'", "未知键必须显式拒绝——静默丢弃会让模型以为设置已生效");
        text.Should().Contain("scope / users / departments / chats / apply_config / require_login");
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAppVisibility_Should_Pass_Parsed_Scopes()
    {
        UpdateAppVisibilityRequest? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkApp>();
        client.Setup(c => c.UpdateAppVisibilityAsync(
                It.IsAny<string>(), It.IsAny<UpdateAppVisibilityRequest>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, UpdateAppVisibilityRequest, string?, CancellationToken>((_, r, _, _) => captured = r)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateAppTools(userApp: client).UpdateAppVisibilityAsync(
            Args(
                ("app_id", "app_1"),
                ("visibility_json",
                    "{\"scope\":\"Range\",\"departments\":[\"od_1\"],\"require_login\":true}")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Scope.Should().Be("Range");
        captured.Departments.Should().BeEquivalentTo(new[] { "od_1" });
        captured.RequireLogin.Should().BeTrue();
        result.ToString().Should().Contain("\"updated\":true");
    }

    // ───────────────────── 数据表面 ─────────────────────

    [Fact]
    public async Task ListTables_Should_Project_Columns()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>();
        client.Setup(c => c.GetTableListAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetTableListResult>
            {
                Code = 0,
                Data = new GetTableListResult
                {
                    Items = [new AppTable { Name = "submissions", Columns = [new AppTableColumn { Name = "age", DataType = "number" }] }],
                },
            });

        var result = await CreateTableTools(table: client).GetTableListAsync(
            Args(("app_id", "app_1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("\"name\":\"submissions\"");
        text.Should().Contain("\"data_type\":\"number\"");
    }

    [Fact]
    public async Task QueryTableRecords_Should_Page_With_Bound_PageSize()
    {
        GetTableRecordListQuery? captured = null;
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>();
        client.Setup(c => c.GetTableRecordListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetTableRecordListQuery>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, GetTableRecordListQuery, CancellationToken>((_, _, q, _) => captured = q)
            .ReturnsAsync(new FeishuApiResult<GetTableRecordListResult>
            {
                Code = 0,
                Data = new GetTableRecordListResult
                {
                    Items = "[{\"name\":\"张三\"}]",
                    Total = 1,
                    HasMore = false,
                },
            });

        var result = await CreateTableTools(table: client).GetTableRecordListAsync(
            Args(("app_id", "app_1"), ("table_id", "submissions"), ("filter", "age=gt.10")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.PageSize.Should().Be(PageSizes.SparkRecords, "页大小是绑定层常量（模型只传 fetch_all/max_items）");
        captured.Filter.Should().Be("age=gt.10", "filter 是 PostgREST 语法，透传给下游");
        // GetTableRecordListResult.Items 是「数组序列化后的 JSON 字符串」——必须解析成真数组回填
        result.ToString().Should().Contain("\"items\":[{\"name\":\"张三\"}]");
        result.ToString().Should().Contain("\"total\":1");
    }

    [Fact]
    public async Task QueryTableRecords_FetchAll_Should_Aggregate_Until_HasMoreFalse()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>();
        client.Setup(c => c.GetTableRecordListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<GetTableRecordListQuery>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, string _, GetTableRecordListQuery q, CancellationToken _) =>
                new FeishuApiResult<GetTableRecordListResult>
                {
                    Code = 0,
                    Data = new GetTableRecordListResult
                    {
                        Items = q.PageToken is null ? "[{\"n\":\"a\"},{\"n\":\"b\"}]" : "[{\"n\":\"c\"}]",
                        Total = 3,
                        HasMore = q.PageToken is null,
                        PageToken = q.PageToken is null ? "tok2" : null,
                    },
                });

        var result = await CreateTableTools(table: client).GetTableRecordListAsync(
            Args(("app_id", "app_1"), ("table_id", "submissions"), ("fetch_all", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("\"total_fetched\":3", "fetch_all 必须在预算内循环翻页并合并三页结果");
        text.Should().Contain("\"pages_fetched\":2");
        text.Should().Contain("\"truncated\":false");
    }

    [Fact]
    public async Task AddTableRecords_Upsert_Should_Map_To_Prefer_Header()
    {
        string? capturedPrefer = null;
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>();
        client.Setup(c => c.PostTableRecordsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<PostTableRecordsRequest>(),
                It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<string?>(),
                It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, PostTableRecordsRequest, string?, string?, string?, string?, string?,
                CancellationToken>((_, _, _, _, _, _, _, prefer, _) => capturedPrefer = prefer)
            .ReturnsAsync(new FeishuApiResult<UpsertTableRecordsResult>
            {
                Code = 0,
                Data = new UpsertTableRecordsResult { RecordIds = ["rec1"] },
            });

        await CreateTableTools(table: client).PostTableRecordsAsync(
            Args(
                ("app_id", "app_1"),
                ("table_id", "submissions"),
                ("records_json", "[{\"name\":\"张三\"}]"),
                ("upsert", true)),
            CancellationToken.None);

        capturedPrefer.Should().Be("resolution=merge-duplicates",
            "upsert 在官方 SDK 里没有 DTO 字段——唯一表达方式是请求头 Prefer: resolution=merge-duplicates");
    }

    [Fact]
    public async Task AddTableRecords_Should_Reject_NonArray_And_OverLimit()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>();

        var notArray = await CreateTableTools(table: client).PostTableRecordsAsync(
            Args(("app_id", "app_1"), ("table_id", "t"), ("records_json", "{\"name\":\"张三\"}")),
            CancellationToken.None);
        notArray.ToString().Should().Contain("必须是 JSON 数组");

        var tooMany = await CreateTableTools(table: client).PostTableRecordsAsync(
            Args(
                ("app_id", "app_1"),
                ("table_id", "t"),
                ("records_json", "[" + string.Join(",", Enumerable.Repeat("{\"n\":1}", 501)) + "]")),
            CancellationToken.None);
        tooMany.ToString().Should().Contain("最多 500 条");

        client.Invocations.Should().BeEmpty("参数形态错误必须在本地拦下（不触下游）");
    }

    [Fact]
    public async Task UpdateTableRecords_Should_Require_Filter()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>();

        var result = await CreateTableTools(table: client).PatchTableRecordsAsync(
            Args(("app_id", "app_1"), ("table_id", "t"), ("filter", "  "), ("record_json", "{\"age\":20}")),
            CancellationToken.None);

        // 参数层双保险：Schema 声明 filter 必填 ⇒ 空白值在**解包层**即被判为缺失；
        // 执行器另有"filter 不得为空"的校验（防全表误更新，见 PatchTableRecordsAsync 的实现）。
        var text = result.ToString()!;
        text.Should().Contain("[tool_error] spark.update_table_records");
        text.Should().Contain("filter");
        client.Invocations.Should().BeEmpty("无条件 PATCH 会更新全表——本地拦下是最后一道防线");
    }

    [Fact]
    public async Task UpdateTableRecords_DryRun_Should_Not_Call_Downstream()
    {
        var client = new Mock<Mud.Feishu.IFeishuUserV1SparkAppTable>();

        var result = await CreateTableTools(table: client).PatchTableRecordsAsync(
            Args(
                ("app_id", "app_1"),
                ("table_id", "t"),
                ("filter", "age=gt.10"),
                ("record_json", "{\"age\":20}"),
                ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().Contain("[dry_run]");
        client.Invocations.Should().BeEmpty();
    }

    [Fact]
    public async Task ClientAbsent_Should_FailFast_With_Actionable_Error()
    {
        var result = await CreateTableTools().GetTableListAsync(
            Args(("app_id", "app_1")), CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[tool_error] spark.list_tables");
        text.Should().Contain("IFeishuUserV1SparkAppTable");
    }
}
