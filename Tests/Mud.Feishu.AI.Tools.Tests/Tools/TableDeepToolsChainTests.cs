// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;
using Mud.Feishu.DataModels.Bitable;
using Mud.Feishu.DataModels.Spreadsheets;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// T-2 多维表格与电子表格<b>真实调用链路</b>用例（bitable 19 = 7 读 + 12 写；sheets 21 = 6 读 + 15 写）：
/// 断言 method/path 契约与 SDK 实参（路径/查询/请求体）、失败码回填与参数非法 → 结构化错误负例。
/// </summary>
/// <remarks>
/// 验收标准沿用 R3：<b>"工具存在"不算通过</b>——每条工具都必须有抓取实参的链路用例。
/// </remarks>
public class TableDeepToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableForm> _form = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableDashboard> _dashboard = new();
    private readonly Mock<Mud.Feishu.Interfaces.IFeishuTenantV1BitableWorkflow> _workflow = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV2BitableRole> _role = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV1BitableRecord> _record = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV2SpreadsheetConditionFormat> _condFormat = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV2SpreadsheetDataValidation> _dataValidation = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV2SpreadsheetProtected> _protectedRange = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetFilter> _filter = new();
    private readonly Mock<Mud.Feishu.IFeishuTenantV3SpreadsheetFloatImage> _floatImage = new();

    private BitableDeepTools CreateBitableTools()
        => new(_form.Object, _dashboard.Object, _workflow.Object, _role.Object, Options.Create(NewOptions()));

    private SheetsDeepTools CreateSheetsTools()
        => new(
            _condFormat.Object,
            _dataValidation.Object,
            _protectedRange.Object,
            _filter.Object,
            _floatImage.Object,
            Options.Create(NewOptions()));

    private BitableWriteTools CreateBatchCreateTools()
        => new(_record.Object, Options.Create(NewOptions()));

    private BitableWriteRecordOps CreateBatchUpdateTools()
        => new(_record.Object, Options.Create(NewOptions()));

    private static FeishuAgentOptions NewOptions() => new() { Instructions = "test" };

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static JsonObject Payload(ToolResult result)
        => JsonNode.Parse(result.ToString()!)!.AsObject();

    // ───────────────────── 契约事实（method / path / risk / scope） ─────────────────────

    /// <summary>
    /// method/path 的真相源是编译期契约（R4 §2.1）：40 条 T-2 工具逐条锁定 http/route/风险/作用域——
    /// 执行器与契约同源，此表是「声明面 ↔ SDK 符号」的机械对账点。
    /// </summary>
    [Theory]
    [InlineData("bitable.form_get", "GET", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}", "Read")]
    [InlineData("bitable.form_question_list", "GET", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}/fields", "Read")]
    [InlineData("bitable.form_update", "PATCH", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}", "Write")]
    [InlineData("bitable.form_upgrade", "POST", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}/upgrade", "Write")]
    [InlineData("bitable.form_question_update", "PATCH", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}/fields/{field_id}", "Write")]
    [InlineData("bitable.dashboard_list", "GET", "/open-apis/bitable/v1/apps/{app_token}/dashboards", "Read")]
    [InlineData("bitable.dashboard_copy", "POST", "/open-apis/bitable/v1/apps/{app_token}/dashboards/{block_id}/copy", "Write")]
    [InlineData("bitable.workflow_list", "GET", "/open-apis/bitable/v1/apps/{app_token}/workflows", "Read")]
    [InlineData("bitable.workflow_block_list", "GET", "/open-apis/bitable/v1/apps/{app_token}/block_workflows", "Read")]
    [InlineData("bitable.workflow_update", "PUT", "/open-apis/bitable/v1/apps/{app_token}/workflows/{workflow_id}", "Write")]
    [InlineData("bitable.role_create", "POST", "/open-apis/base/v2/apps/{app_token}/roles", "Write")]
    [InlineData("bitable.role_update", "PUT", "/open-apis/base/v2/apps/{app_token}/roles/{role_id}", "Write")]
    [InlineData("bitable.role_delete", "DELETE", "/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}", "HighRiskWrite")]
    [InlineData("bitable.role_list", "GET", "/open-apis/base/v2/apps/{app_token}/roles", "Read")]
    [InlineData("bitable.role_member_add", "POST", "/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members", "Write")]
    [InlineData("bitable.role_member_list", "GET", "/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members", "Read")]
    [InlineData("bitable.role_member_remove", "DELETE", "/open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members/{member_id}", "HighRiskWrite")]
    [InlineData("bitable.record_batch_create", "POST", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/batch_create", "Write")]
    [InlineData("bitable.record_batch_update", "POST", "/open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/batch_update", "Write")]
    [InlineData("sheets.cond_format_get", "GET", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats", "Read")]
    [InlineData("sheets.cond_format_create", "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_create", "Write")]
    [InlineData("sheets.cond_format_update", "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_update", "Write")]
    [InlineData("sheets.cond_format_delete", "DELETE", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_delete", "HighRiskWrite")]
    [InlineData("sheets.data_validation_get", "GET", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation", "Read")]
    [InlineData("sheets.data_validation_create", "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation", "Write")]
    [InlineData("sheets.data_validation_update", "PUT", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation/{sheetId}", "Write")]
    [InlineData("sheets.data_validation_delete", "DELETE", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation", "HighRiskWrite")]
    [InlineData("sheets.protected_get", "GET", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_get", "Read")]
    [InlineData("sheets.protected_create", "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_dimension", "Write")]
    [InlineData("sheets.protected_update", "POST", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_update", "Write")]
    [InlineData("sheets.protected_delete", "DELETE", "/open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/protected_range_batch_del", "HighRiskWrite")]
    [InlineData("sheets.filter_get", "GET", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter", "Read")]
    [InlineData("sheets.filter_create", "POST", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter", "Write")]
    [InlineData("sheets.filter_update", "PUT", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter", "Write")]
    [InlineData("sheets.filter_delete", "DELETE", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/filter", "HighRiskWrite")]
    [InlineData("sheets.float_image_get", "GET", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}", "Read")]
    [InlineData("sheets.float_image_query", "GET", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/query", "Read")]
    [InlineData("sheets.float_image_create", "POST", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images", "Write")]
    [InlineData("sheets.float_image_update", "PATCH", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}", "Write")]
    [InlineData("sheets.float_image_delete", "DELETE", "/open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}", "HighRiskWrite")]
    public void Contracts_ShouldCarrySdkMethodAndRoute(string toolName, string httpMethod, string route, string risk)
    {
        FeishuToolContracts.ByToolName.Should().ContainKey(toolName);
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod, "契约的 http 与 SDK 符号派生一致");
        contract.Route.Should().Be(route);
        contract.Identity.Should().Be("tenant");
        contract.Risk.ToString().Should().Be(risk);
        contract.IsWrite.Should().Be(httpMethod != "GET", "读工具仅 GET；写工具为 POST/PUT/PATCH/DELETE");

        var scopePrefix = toolName.StartsWith("bitable.", StringComparison.Ordinal) ? "bitable:app" : "sheets:spreadsheet";
        contract.RequiredScopes.Should().Contain(
            httpMethod == "GET" ? $"{scopePrefix}:readonly" : scopePrefix,
            "读写作用域按域前缀唯一裁定（scope-authority 单源）");
    }

    // ───────────────────── bitable 只读（7） ─────────────────────

    [Fact]
    public async Task FormGet_ShouldPassAllPathIds_AndProjectForm()
    {
        string? capturedAppToken = null;
        string? capturedTableId = null;
        string? capturedFormId = null;

        _form
            .Setup(c => c.GetFormAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string appToken, string tableId, string formId, CancellationToken _) =>
            {
                capturedAppToken = appToken;
                capturedTableId = tableId;
                capturedFormId = formId;
            })
            .ReturnsAsync(new FeishuApiResult<FormResult>
            {
                Code = 0,
                Data = new FormResult
                {
                    Form = new AppTableFormInfo
                    {
                        Name = "报名表",
                        Description = "收集报名信息",
                        Shared = true,
                        SharedLimit = "tenant_editable",
                        SubmitLimitOnce = false,
                        SharedUrl = "https://example.feishu.cn/share/form",
                    },
                },
            });

        var result = await CreateBitableTools().GetFormAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl1"), ("form_id", "vew1")), CancellationToken.None);

        capturedAppToken.Should().Be("bascnXxx", "app_token 经 [Path] 下发");
        capturedTableId.Should().Be("tbl1");
        capturedFormId.Should().Be("vew1");

        var payload = Payload(result);
        var form = payload["form"]!;
        form["name"]!.GetValue<string>().Should().Be("报名表");
        form["description"]!.GetValue<string>().Should().Be("收集报名信息");
        form["shared"]!.GetValue<bool>().Should().BeTrue();
        form["shared_limit"]!.GetValue<string>().Should().Be("tenant_editable");
        form["submit_limit_once"]!.GetValue<bool>().Should().BeFalse();
        form["shared_url"]!.GetValue<string>().Should().Be("https://example.feishu.cn/share/form");
    }

    [Fact]
    public async Task FormQuestionList_ShouldPassPageSizeAndToken_AndProjectPagedItems()
    {
        int capturedPageSize = 0;
        string? capturedPageToken = null;

        _form
            .Setup(c => c.GetFormFieldsPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string ___, int pageSize, string? pageToken, CancellationToken ____) =>
            {
                capturedPageSize = pageSize;
                capturedPageToken = pageToken;
            })
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppTableFormFieldInfo>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppTableFormFieldInfo>
                {
                    Items =
                    [
                        new AppTableFormFieldInfo
                        {
                            FieldId = "fld1",
                            Title = "姓名",
                            Description = "填写真实姓名",
                            Required = true,
                            Visible = true,
                        },
                    ],
                    HasMore = true,
                    PageToken = "pt_next",
                    Total = 7,
                },
            });

        var result = await CreateBitableTools().GetFormFieldsPageListAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl1"), ("form_id", "vew1"), ("page_token", "pt_prev")),
            CancellationToken.None);

        capturedPageSize.Should().Be(PageSizes.BitableFormQuestions, "page_size 用统一常量（执行器不硬编码）");
        capturedPageToken.Should().Be("pt_prev", "page_token 原文直通支撑多页追问");

        var payload = Payload(result);
        payload["has_more"]!.GetValue<bool>().Should().BeTrue();
        payload["page_token"]!.GetValue<string>().Should().Be("pt_next");
        var item = payload["items"]!.AsArray()[0]!;
        item["field_id"]!.GetValue<string>().Should().Be("fld1");
        item["title"]!.GetValue<string>().Should().Be("姓名");
        item["required"]!.GetValue<bool>().Should().BeTrue();
        item["visible"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task DashboardList_ShouldPassPageSize_AndProjectItems()
    {
        int capturedPageSize = 0;

        _dashboard
            .Setup(c => c.GetDashboardPageListAsync(
                It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, int pageSize, string? __, CancellationToken ___) => capturedPageSize = pageSize)
            .ReturnsAsync(new FeishuApiPageListResult<AppDashboard>
            {
                Code = 0,
                Data = new ApiPageListResult<AppDashboard>
                {
                    Items = [new AppDashboard { BlockId = "blk1", Name = "销售看板" }],
                    HasMore = false,
                },
            });

        var result = await CreateBitableTools().GetDashboardPageListAsync(
            Args(("app_token", "bascnXxx")), CancellationToken.None);

        capturedPageSize.Should().Be(PageSizes.BitableDashboards);

        var payload = Payload(result);
        var item = payload["items"]!.AsArray()[0]!;
        item["block_id"]!.GetValue<string>().Should().Be("blk1");
        item["name"]!.GetValue<string>().Should().Be("销售看板");
    }

    [Fact]
    public async Task WorkflowList_ShouldCallListEndpoint_NotBlockEndpoint()
    {
        string? capturedAppToken = null;

        _workflow
            .Setup(c => c.GetAppWorkflowListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string appToken, CancellationToken _) => capturedAppToken = appToken)
            .ReturnsAsync(new FeishuApiResult<GetAppWorkflowListResult>
            {
                Code = 0,
                Data = new GetAppWorkflowListResult
                {
                    Workflows = [new DisplayWorkflow { WorkflowId = "wkf1", Status = "Enable", Title = "自动编号" }],
                },
            });

        var result = await CreateBitableTools().GetAppWorkflowListAsync(
            Args(("app_token", "bascnXxx")), CancellationToken.None);

        capturedAppToken.Should().Be("bascnXxx");
        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["workflow_id"]!.GetValue<string>().Should().Be("wkf1");
        item["status"]!.GetValue<string>().Should().Be("Enable");
        item["title"]!.GetValue<string>().Should().Be("自动编号");

        _workflow.Verify(
            c => c.GetAppBlockWorkflowListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "workflow_list 与 workflow_block_list 是两个独立端点，不得互相串调");
    }

    [Fact]
    public async Task WorkflowBlockList_ShouldCallBlockEndpoint_NotListEndpoint()
    {
        string? capturedAppToken = null;

        _workflow
            .Setup(c => c.GetAppBlockWorkflowListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string appToken, CancellationToken _) => capturedAppToken = appToken)
            .ReturnsAsync(new FeishuApiResult<GetAppWorkflowListResult>
            {
                Code = 0,
                Data = new GetAppWorkflowListResult
                {
                    Workflows = [new DisplayWorkflow { WorkflowId = "wkf2", Status = "Disable", Title = "表格联动" }],
                },
            });

        var result = await CreateBitableTools().GetAppBlockWorkflowListAsync(
            Args(("app_token", "bascnXxx")), CancellationToken.None);

        capturedAppToken.Should().Be("bascnXxx");
        Payload(result)["items"]!.AsArray()[0]!["workflow_id"]!.GetValue<string>().Should().Be("wkf2");

        _workflow.Verify(
            c => c.GetAppWorkflowListAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RoleList_ShouldPassPageSize_AndProjectItems()
    {
        int capturedPageSize = 0;

        _role
            .Setup(c => c.GetRolesPageListAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, int pageSize, string? __, CancellationToken ___) => capturedPageSize = pageSize)
            .ReturnsAsync(new FeishuApiPageListTotalResult<AppRoleInfo>
            {
                Code = 0,
                Data = new ApiPageListTotalResult<AppRoleInfo>
                {
                    Items = [new AppRoleInfo { RoleId = "rol1", RoleName = "只读角色" }],
                    HasMore = false,
                    Total = 1,
                },
            });

        var result = await CreateBitableTools().GetRolesPageListAsync(
            Args(("app_token", "bascnXxx")), CancellationToken.None);

        capturedPageSize.Should().Be(PageSizes.BitableRoles);

        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["role_id"]!.GetValue<string>().Should().Be("rol1");
        item["role_name"]!.GetValue<string>().Should().Be("只读角色");
    }

    [Fact]
    public async Task RoleMemberList_ShouldPassRoleIdAndPageSize_AndProjectItems()
    {
        string? capturedRoleId = null;
        int capturedPageSize = 0;

        _role
            .Setup(c => c.GetRoleMembersPageListAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string roleId, int pageSize, string? __, CancellationToken ___) =>
            {
                capturedRoleId = roleId;
                capturedPageSize = pageSize;
            })
            .ReturnsAsync(new FeishuApiPageListResult<AppRoleMember>
            {
                Code = 0,
                Data = new ApiPageListResult<AppRoleMember>
                {
                    Items = [new AppRoleMember { MemberType = "openid", MemberId = "ou_1", MemberName = "张三" }],
                    HasMore = false,
                },
            });

        var result = await CreateBitableTools().GetRoleMembersPageListAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol1")), CancellationToken.None);

        capturedRoleId.Should().Be("rol1", "role_id 经 [Path] 下发");
        capturedPageSize.Should().Be(PageSizes.BitableRoleMembers);

        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["member_type"]!.GetValue<string>().Should().Be("openid");
        item["member_id"]!.GetValue<string>().Should().Be("ou_1");
        item["member_name"]!.GetValue<string>().Should().Be("张三");
    }

    // ───────────────────── bitable 写（19） ─────────────────────

    [Fact]
    public async Task FormUpdate_ShouldMapOptionalFieldsIntoSinglePatch()
    {
        UpdateFormRequest? captured = null;

        _form
            .Setup(c => c.UpdateFormAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateFormRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string ___, UpdateFormRequest request, CancellationToken ____) => captured = request)
            .ReturnsAsync(new FeishuApiResult<FormResult>
            {
                Code = 0,
                Data = new FormResult
                {
                    Form = new AppTableFormInfo { Name = "新名称", Shared = true, SharedUrl = "https://example.feishu.cn/share/form" },
                },
            });

        var result = await CreateBitableTools().UpdateFormAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("form_id", "vew1"),
                ("name", "新名称"),
                ("shared", true),
                ("shared_limit", "tenant_editable")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Name.Should().Be("新名称");
        captured.Shared.Should().Be(true);
        captured.SharedLimit.Should().Be("tenant_editable");
        captured.Description.Should().BeNull("未提供的字段保持 null（PATCH 语义：不覆盖）");
        captured.SubmitLimitOnce.Should().BeNull();

        var form = Payload(result)["form"]!;
        form["name"]!.GetValue<string>().Should().Be("新名称");
        form["shared"]!.GetValue<bool>().Should().BeTrue();
        form["shared_url"]!.GetValue<string>().Should().Be("https://example.feishu.cn/share/form");
    }

    [Fact]
    public async Task FormUpdate_WithoutAnyField_ShouldReturnStructuredError_WithoutCallingDownstream()
    {
        var result = await CreateBitableTools().UpdateFormAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl1"), ("form_id", "vew1")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] bitable.form_update");
        result.ToString().Should().Contain("至少提供一个待更新字段");
        _form.Verify(
            c => c.UpdateFormAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateFormRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never,
            "参数校验失败必须在下发前拦截");
    }

    [Fact]
    public async Task FormUpdate_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var result = await CreateBitableTools().UpdateFormAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl1"), ("form_id", "vew1"), ("name", "新名称"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("PATCH /open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}");
        text.Should().Contain("幂等键：omitted", "官方表单更新无幂等键参数");
        text.Should().NotContain("新名称", "干跑摘要不回显字段原文（只回长度/模板）");
        _form.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FormUpgrade_ShouldPassNameAndDisplayMode_AndBackfillFormId()
    {
        UpgradeFormRequest? captured = null;

        _form
            .Setup(c => c.UpgradeFormAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpgradeFormRequest>(),
                It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string ___, UpgradeFormRequest request, CancellationToken ____) => captured = request)
            .ReturnsAsync(new FeishuApiResult<UpgradeFormResult>
            {
                Code = 0,
                Data = new UpgradeFormResult { Form = new UpgradedForm { Id = "vew_new" } },
            });

        var result = await CreateBitableTools().UpgradeFormAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("form_id", "vew1"),
                ("form_name", "报名表 II"),
                ("display_mode", "one_question_per_page")),
            CancellationToken.None);

        captured!.FormName.Should().Be("报名表 II");
        captured.DisplayMode.Should().Be("one_question_per_page");
        Payload(result)["form_id"]!.GetValue<string>().Should().Be("vew_new", "回填升级后的新表单 ID");
    }

    [Fact]
    public async Task FormUpgrade_WithInvalidDisplayMode_ShouldReturnStructuredError()
    {
        var result = await CreateBitableTools().UpgradeFormAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("form_id", "vew1"),
                ("form_name", "报名表"),
                ("display_mode", "paged")),
            CancellationToken.None);

        result.ToString().Should().Contain("display_mode 必须为 'traditional' 或 'one_question_per_page'");
        _form.Verify(
            c => c.UpgradeFormAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpgradeFormRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FormUpgrade_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var result = await CreateBitableTools().UpgradeFormAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("form_id", "vew1"),
                ("form_name", "报名表 II"),
                ("display_mode", "traditional"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/forms/{form_id}/upgrade");
        text.Should().Contain("幂等键：omitted");
        text.Should().NotContain("报名表 II");
        _form.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FormQuestionUpdate_ShouldPassFieldIdPath_AndBackfillFieldIdFromArgs()
    {
        string? capturedFieldId = null;
        UpdateFormFieldRequest? captured = null;

        _form
            .Setup(c => c.UpdateFormFieldAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UpdateFormFieldRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string ___, string fieldId, UpdateFormFieldRequest request, CancellationToken ____) =>
            {
                capturedFieldId = fieldId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<UpdateFormFieldResult>
            {
                Code = 0,
                Data = new UpdateFormFieldResult
                {
                    Fields = new AppTableFormFieldData { Title = "新标题", Required = true, Visible = false },
                },
            });

        var result = await CreateBitableTools().UpdateFormFieldAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("form_id", "vew1"),
                ("field_id", "fld9"),
                ("title", "新标题"),
                ("required", true)),
            CancellationToken.None);

        capturedFieldId.Should().Be("fld9", "field_id 经 [Path] 下发");
        captured!.Title.Should().Be("新标题");
        captured.Required.Should().Be(true);
        captured.Visible.Should().BeNull("未提供的字段保持 null");

        var payload = Payload(result);
        payload["field_id"]!.GetValue<string>().Should().Be("fld9", "官方回包不带 field_id，投影回填入参");
        payload["title"]!.GetValue<string>().Should().Be("新标题");
        payload["required"]!.GetValue<bool>().Should().BeTrue();
        payload["visible"]!.GetValue<bool>().Should().BeFalse();
    }

    [Fact]
    public async Task FormQuestionUpdate_WithoutAnyField_ShouldReturnStructuredError()
    {
        var result = await CreateBitableTools().UpdateFormFieldAsync(
            Args(("app_token", "bascnXxx"), ("table_id", "tbl1"), ("form_id", "vew1"), ("field_id", "fld9")),
            CancellationToken.None);

        result.ToString().Should().Contain("至少提供一个待更新字段（pre_field_id / title / description / required / visible）");
        _form.Verify(
            c => c.UpdateFormFieldAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UpdateFormFieldRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DashboardCopy_ShouldPassBlockIdAndName_AndBackfillNewIds()
    {
        string? capturedBlockId = null;
        CopyDashboardRequest? captured = null;

        _dashboard
            .Setup(c => c.CopyDashboardAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CopyDashboardRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string blockId, CopyDashboardRequest request, CancellationToken __) =>
            {
                capturedBlockId = blockId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<CopyDashboardResult>
            {
                Code = 0,
                Data = new CopyDashboardResult { BlockId = "blk_new", Name = "副本看板" },
            });

        var result = await CreateBitableTools().CopyDashboardAsync(
            Args(("app_token", "bascnXxx"), ("block_id", "blk1"), ("name", "副本看板")), CancellationToken.None);

        capturedBlockId.Should().Be("blk1", "block_id 经 [Path] 下发");
        captured!.Name.Should().Be("副本看板");

        var payload = Payload(result);
        payload["block_id"]!.GetValue<string>().Should().Be("blk_new");
        payload["name"]!.GetValue<string>().Should().Be("副本看板");
    }

    [Fact]
    public async Task WorkflowUpdate_ShouldPassWorkflowIdAndStatus_AndReturnReceipt()
    {
        string? capturedWorkflowId = null;
        UpdateAppWorkflowRequest? captured = null;

        _workflow
            .Setup(c => c.UpdateAppWorkflowAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateAppWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string workflowId, UpdateAppWorkflowRequest request, CancellationToken __) =>
            {
                capturedWorkflowId = workflowId;
                captured = request;
            })
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateBitableTools().UpdateAppWorkflowAsync(
            Args(("app_token", "bascnXxx"), ("workflow_id", "wkf9"), ("status", "Enable")), CancellationToken.None);

        capturedWorkflowId.Should().Be("wkf9", "workflow_id 经 [Path] 下发");
        captured!.Status.Should().Be("Enable");

        var payload = Payload(result);
        payload["workflow_id"]!.GetValue<string>().Should().Be("wkf9");
        payload["status"]!.GetValue<string>().Should().Be("Enable");
        payload["updated"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task WorkflowUpdate_WithInvalidStatus_ShouldReturnStructuredError()
    {
        var result = await CreateBitableTools().UpdateAppWorkflowAsync(
            Args(("app_token", "bascnXxx"), ("workflow_id", "wkf9"), ("status", "enabled")), CancellationToken.None);

        result.ToString().Should().Contain("status 必须为 'Enable'（启用）或 'Disable'（停用）");
        _workflow.Verify(
            c => c.UpdateAppWorkflowAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateAppWorkflowRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WorkflowUpdate_WhenApiReturnsErrorCode_ShouldSurfaceCode()
    {
        _workflow
            .Setup(c => c.UpdateAppWorkflowAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateAppWorkflowRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 1254045, Msg = "workflow not found" });

        var result = await CreateBitableTools().UpdateAppWorkflowAsync(
            Args(("app_token", "bascnXxx"), ("workflow_id", "wkf9"), ("status", "Enable")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] bitable.workflow_update");
        result.ToString().Should().Contain(
            "1254045",
            "失败回填必须带飞书业务 code（错误分类消费）");
    }

    [Fact]
    public async Task RoleCreate_ShouldParseTableRolesJson_AndLeaveBlockRolesNull()
    {
        CreateRoleRequest? captured = null;

        _role
            .Setup(c => c.CreateRoleAsync(It.IsAny<string>(), It.IsAny<CreateRoleRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateRoleRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<RoleOpsResult>
            {
                Code = 0,
                Data = new RoleOpsResult { Role = new AppRoleInfo { RoleId = "rol1", RoleName = "只读角色" } },
            });

        var result = await CreateBitableTools().CreateRoleAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("role_name", "只读角色"),
                ("table_roles", "[{\"table_perm\":1,\"table_name\":\"采购表\",\"table_id\":\"tbl_1\"}]")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.RoleName.Should().Be("只读角色");
        captured.TableRoles.Should().HaveCount(1);
        captured.TableRoles[0].TableId.Should().Be("tbl_1", "table_roles JSON 逐字段反序列化");
        captured.TableRoles[0].TablePerm.Should().Be(1);
        captured.BlockRoles.Should().BeNull("未提供 block_roles 时不得写入空数组");

        var payload = Payload(result);
        payload["role_id"]!.GetValue<string>().Should().Be("rol1");
        payload["role_name"]!.GetValue<string>().Should().Be("只读角色");
    }

    [Fact]
    public async Task RoleCreate_WithInvalidTableRolesJson_ShouldReturnStructuredError()
    {
        var result = await CreateBitableTools().CreateRoleAsync(
            Args(("app_token", "bascnXxx"), ("role_name", "只读角色"), ("table_roles", "not-json")),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] bitable.role_create");
        result.ToString().Should().Contain("table_roles 解析失败");
        _role.Verify(
            c => c.CreateRoleAsync(It.IsAny<string>(), It.IsAny<CreateRoleRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RoleCreate_DryRun_ShouldNotCallDownstream_AndReturnBaseV2Route()
    {
        var result = await CreateBitableTools().CreateRoleAsync(
            Args(("app_token", "bascnXxx"), ("role_name", "只读角色"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/base/v2/apps/{app_token}/roles", "角色写面走 base/v2 而非 bitable/v1");
        text.Should().Contain("幂等键：omitted");
        text.Should().NotContain("只读角色");
        _role.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RoleUpdate_ShouldPassRoleIdAndName()
    {
        string? capturedRoleId = null;
        UpdateRoleRequest? captured = null;

        _role
            .Setup(c => c.UpdateRoleAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRoleRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string roleId, UpdateRoleRequest request, CancellationToken __) =>
            {
                capturedRoleId = roleId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<RoleOpsResult>
            {
                Code = 0,
                Data = new RoleOpsResult { Role = new AppRoleInfo { RoleId = "rol1", RoleName = "改名角色" } },
            });

        var result = await CreateBitableTools().UpdateRoleAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol1"), ("role_name", "改名角色")), CancellationToken.None);

        capturedRoleId.Should().Be("rol1", "role_id 经 [Path] 下发");
        captured!.RoleName.Should().Be("改名角色", "官方要求即便不改名也须回传 role_name");
        Payload(result)["role_id"]!.GetValue<string>().Should().Be("rol1");
    }

    [Fact]
    public async Task RoleDelete_ShouldReturnReceipt_WhenDeleted()
    {
        string? capturedRoleId = null;

        _role
            .Setup(c => c.DeleteRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string roleId, CancellationToken __) => capturedRoleId = roleId)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateBitableTools().DeleteRoleAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol9")), CancellationToken.None);

        capturedRoleId.Should().Be("rol9");
        var payload = Payload(result);
        payload["role_id"]!.GetValue<string>().Should().Be("rol9");
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task RoleDelete_WhenApiReturnsErrorCode_ShouldSurfaceCode()
    {
        _role
            .Setup(c => c.DeleteRoleAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 1254300, Msg = "role not found" });

        var result = await CreateBitableTools().DeleteRoleAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol9")), CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] bitable.role_delete");
        result.ToString().Should().Contain("1254300", "失败回填必须带飞书业务 code（错误分类消费）");
    }

    [Fact]
    public async Task RoleDelete_DryRun_ShouldNotCallDownstream_AndReturnBitableV1Route()
    {
        var result = await CreateBitableTools().DeleteRoleAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol_secret"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain(
            "DELETE /open-apis/bitable/v1/apps/{app_token}/roles/{role_id}",
            "删除不走 base/v2——官方删除角色接口为 bitable/v1（与 create/update/list 不同层）");
        text.Should().Contain("幂等键：omitted");
        text.Should().NotContain("rol_secret");
        _role.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RoleMemberAdd_ShouldDefaultMemberIdTypeToOpenId()
    {
        string? capturedMemberIdType = null;

        _role
            .Setup(c => c.AddRoleMemberAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AddRoleMemberRequest>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, AddRoleMemberRequest ___, string memberIdType, CancellationToken ____) =>
                capturedMemberIdType = memberIdType)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateBitableTools().AddRoleMemberAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol1"), ("member_id", "ou_1")), CancellationToken.None);

        capturedMemberIdType.Should().Be("open_id", "member_id_type 缺省为 open_id");
        var payload = Payload(result);
        payload["member_id"]!.GetValue<string>().Should().Be("ou_1");
        payload["added"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task RoleMemberAdd_ShouldPassExplicitMemberIdType()
    {
        string? capturedMemberIdType = null;

        _role
            .Setup(c => c.AddRoleMemberAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AddRoleMemberRequest>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, AddRoleMemberRequest ___, string memberIdType, CancellationToken ____) =>
                capturedMemberIdType = memberIdType)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        await CreateBitableTools().AddRoleMemberAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol1"), ("member_id", "oc_1"), ("member_id_type", "chat_id")),
            CancellationToken.None);

        capturedMemberIdType.Should().Be("chat_id", "显式 member_id_type 必须直通平台查询参数");
    }

    [Fact]
    public async Task RoleMemberRemove_ShouldPassExplicitMemberIdType_AndReturnReceipt()
    {
        string? capturedMemberId = null;
        string? capturedMemberIdType = null;

        _role
            .Setup(c => c.DeleteRoleMemberAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string memberId, string memberIdType, CancellationToken ___) =>
            {
                capturedMemberId = memberId;
                capturedMemberIdType = memberIdType;
            })
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateBitableTools().DeleteRoleMemberAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("role_id", "rol1"),
                ("member_id", "u_1"),
                ("member_id_type", "user_id")),
            CancellationToken.None);

        capturedMemberId.Should().Be("u_1", "member_id 经 [Path] 下发");
        capturedMemberIdType.Should().Be("user_id");

        var payload = Payload(result);
        payload["member_id"]!.GetValue<string>().Should().Be("u_1");
        payload["removed"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task RoleMemberRemove_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var result = await CreateBitableTools().DeleteRoleMemberAsync(
            Args(("app_token", "bascnXxx"), ("role_id", "rol1"), ("member_id", "ou_secret"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("DELETE /open-apis/bitable/v1/apps/{app_token}/roles/{role_id}/members/{member_id}");
        text.Should().Contain("幂等键：omitted");
        text.Should().NotContain("ou_secret");
        _role.VerifyNoOtherCalls();
    }

    // ───────────────────── bitable 批量记录写（2） ─────────────────────

    [Fact]
    public async Task RecordBatchCreate_ShouldParseRecordsJson_AndPassClientToken()
    {
        AddRecordsRequest? captured = null;
        string? capturedClientToken = null;

        _record
            .Setup(c => c.AddRecordsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<AddRecordsRequest>(),
                It.IsAny<string?>(), It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, AddRecordsRequest request, string? clientToken, bool? ___, string ____, CancellationToken _____) =>
            {
                captured = request;
                capturedClientToken = clientToken;
            })
            .ReturnsAsync(new FeishuApiResult<RecordsOpsResult>
            {
                Code = 0,
                Data = new RecordsOpsResult
                {
                    Records =
                    [
                        new AppTableRecordInfo { RecordId = "rec_1" },
                        new AppTableRecordInfo { RecordId = "rec_2" },
                        new AppTableRecordInfo { RecordId = null },
                    ],
                },
            });

        var result = await CreateBatchCreateTools().AddRecordsAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("records", "[{\"fields\":{\"任务\":\"写周报\"}}]"),
                ("idempotency_key", "bill-9")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Records.Should().HaveCount(1);
        var fields = (JsonElement)captured.Records[0].Fields;
        fields.GetProperty("任务").GetString().Should().Be("写周报", "records JSON 原文反序列化进请求体");
        capturedClientToken.Should().Be("bill-9", "idempotency_key 必须直通 platform client_token（平台原生幂等）");

        var payload = Payload(result);
        payload["record_ids"]!.AsArray().Should().HaveCount(2, "空 record_id 不得进入回填列表");
        payload["record_ids"]!.AsArray()[0]!.GetValue<string>().Should().Be("rec_1");
        payload["count"]!.GetValue<int>().Should().Be(2);
    }

    [Fact]
    public async Task RecordBatchCreate_DryRun_ShouldEchoIdempotencyProvided_AndNotEchoContent()
    {
        var result = await CreateBatchCreateTools().AddRecordsAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("records", "[{\"fields\":{\"任务\":\"绝密周报\"}}]"),
                ("idempotency_key", "bill-9"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("POST /open-apis/bitable/v1/apps/{app_token}/tables/{table_id}/records/batch_create");
        text.Should().Contain("幂等键：provided", "batch_create 支持 client_token（预演不占坑）");
        text.Should().NotContain("bill-9", "摘要不得回显幂等键原文");
        text.Should().NotContain("绝密周报", "dry_run 不得成为回显通道");
        _record.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RecordBatchUpdate_ShouldPassParsedRecordsWithoutClientToken()
    {
        UpdateRecordsRequest? captured = null;

        _record
            .Setup(c => c.UpdateRecordsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRecordsRequest>(),
                It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, UpdateRecordsRequest request, bool? ___, string ____, CancellationToken _____) =>
                captured = request)
            .ReturnsAsync(new FeishuApiResult<RecordsOpsResult>
            {
                Code = 0,
                Data = new RecordsOpsResult { Records = [new AppTableRecordInfo { RecordId = "rec_1" }] },
            });

        var result = await CreateBatchUpdateTools().UpdateRecordsAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("records", "[{\"record_id\":\"rec_1\",\"fields\":{\"状态\":\"done\"}}]")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Records.Should().HaveCount(1);
        captured.Records[0].RecordId.Should().Be("rec_1");

        var payload = Payload(result);
        payload["record_ids"]!.AsArray()[0]!.GetValue<string>().Should().Be("rec_1");
        payload["count"]!.GetValue<int>().Should().Be(1);
    }

    [Fact]
    public async Task RecordBatchUpdate_WithoutRecordId_ShouldReturnStructuredError_EvenInDryRun()
    {
        var result = await CreateBatchUpdateTools().UpdateRecordsAsync(
            Args(
                ("app_token", "bascnXxx"),
                ("table_id", "tbl1"),
                ("records", "[{\"fields\":{\"状态\":\"done\"}}]"),
                ("dry_run", true)),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] bitable.record_batch_update");
        result.ToString().Should().Contain("records 每项须携带非空 record_id");
        _record.Verify(
            c => c.UpdateRecordsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateRecordsRequest>(),
                It.IsAny<bool?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "逐项校验先于 dry_run：缺 record_id 的项无法定位目标记录，预演也不该放过");
    }

    // ───────────────────── sheets 只读（6） ─────────────────────

    [Fact]
    public async Task ConditionFormatsGet_ShouldPassSheetIds_AndProjectRules()
    {
        string? capturedSheetIds = null;

        _condFormat
            .Setup(c => c.GetConditionFormatsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string sheetIds, CancellationToken __) => capturedSheetIds = sheetIds)
            .ReturnsAsync(new FeishuApiResult<GetConditionFormatsResult>
            {
                Code = 0,
                Data = new GetConditionFormatsResult
                {
                    SheetConditionFormats =
                    [
                        new SheetConditionFormatData
                        {
                            SheetId = "s1",
                            ConditionFormat = new ConditionFormatData
                            {
                                CfId = "cf_1",
                                RuleType = "text_contains",
                                Ranges = ["A1:A9"],
                            },
                        },
                    ],
                },
            });

        var result = await CreateSheetsTools().GetConditionFormatsAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_ids", "s1,s2")), CancellationToken.None);

        capturedSheetIds.Should().Be("s1,s2", "sheet_ids 原文直通平台查询参数");
        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["sheet_id"]!.GetValue<string>().Should().Be("s1");
        item["cf_id"]!.GetValue<string>().Should().Be("cf_1");
        item["rule_type"]!.GetValue<string>().Should().Be("text_contains");
        item["ranges"]!.AsArray()[0]!.GetValue<string>().Should().Be("A1:A9");
    }

    [Fact]
    public async Task DataValidationGet_ShouldPassRangeAndType_AndProjectRules()
    {
        string? capturedRange = null;
        string? capturedType = null;

        _dataValidation
            .Setup(c => c.GetDataValidationsAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string range, string dataValidationType, CancellationToken __) =>
            {
                capturedRange = range;
                capturedType = dataValidationType;
            })
            .ReturnsAsync(new FeishuApiResult<GetDataValidationsResult>
            {
                Code = 0,
                Data = new GetDataValidationsResult
                {
                    SheetId = "s1",
                    DataValidations =
                    [
                        new SheetDataValidationResult
                        {
                            Ranges = ["A1:A9"],
                            DataValidationType = "list",
                            ConditionValues = ["高", "中", "低"],
                        },
                    ],
                },
            });

        var result = await CreateSheetsTools().GetDataValidationsAsync(
            Args(("spreadsheet_token", "ss_1"), ("range", "s1!A1:A9"), ("data_validation_type", "list")),
            CancellationToken.None);

        capturedRange.Should().Be("s1!A1:A9");
        capturedType.Should().Be("list");

        var payload = Payload(result);
        payload["sheet_id"]!.GetValue<string>().Should().Be("s1");
        var item = payload["items"]!.AsArray()[0]!;
        item["ranges"]!.AsArray()[0]!.GetValue<string>().Should().Be("A1:A9");
        item["data_validation_type"]!.GetValue<string>().Should().Be("list");
        item["condition_values"]!.AsArray()[1]!.GetValue<string>().Should().Be("中");
    }

    [Fact]
    public async Task ProtectedGet_ShouldDefaultMemberTypeToOpenId_AndProjectDimension()
    {
        string? capturedProtectIds = null;
        string? capturedMemberType = null;

        _protectedRange
            .Setup(c => c.GetProtectedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string protectIds, string? memberType, CancellationToken __) =>
            {
                capturedProtectIds = protectIds;
                capturedMemberType = memberType;
            })
            .ReturnsAsync(new FeishuApiResult<GetProtectedResult>
            {
                Code = 0,
                Data = new GetProtectedResult
                {
                    ProtectedRanges =
                    [
                        new SheetResultProtectedInfo
                        {
                            SheetId = "s1",
                            ProtectId = "pr_1",
                            Dimension = new SheetRangeDimension
                            {
                                SheetId = "s1",
                                MajorDimension = "ROWS",
                                StartIndex = 0,
                                EndIndex = 5,
                            },
                            LockInfo = "locked",
                        },
                    ],
                },
            });

        var result = await CreateSheetsTools().GetProtectedAsync(
            Args(("spreadsheet_token", "ss_1"), ("protect_ids", "pr_1,pr_2")), CancellationToken.None);

        capturedProtectIds.Should().Be("pr_1,pr_2", "protectIds 原文直通平台查询参数");
        capturedMemberType.Should().Be("open_id", "memberType 缺省为 open_id");

        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["protect_id"]!.GetValue<string>().Should().Be("pr_1");
        item["lock_info"]!.GetValue<string>().Should().Be("locked");
        var dimension = item["dimension"]!;
        dimension["major_dimension"]!.GetValue<string>().Should().Be("ROWS");
        dimension["start_index"]!.GetValue<int>().Should().Be(0);
        dimension["end_index"]!.GetValue<int>().Should().Be(5);
    }

    [Fact]
    public async Task ProtectedGet_ShouldPassExplicitMemberType()
    {
        string? capturedMemberType = null;

        _protectedRange
            .Setup(c => c.GetProtectedAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string? memberType, CancellationToken ___) => capturedMemberType = memberType)
            .ReturnsAsync(new FeishuApiResult<GetProtectedResult>
            {
                Code = 0,
                Data = new GetProtectedResult { ProtectedRanges = [] },
            });

        await CreateSheetsTools().GetProtectedAsync(
            Args(("spreadsheet_token", "ss_1"), ("protect_ids", "pr_1"), ("member_type", "userid")),
            CancellationToken.None);

        capturedMemberType.Should().Be("userid", "显式 member_type 必须直通平台查询参数");
    }

    [Fact]
    public async Task FilterGet_ShouldProjectFilterInfos()
    {
        string? capturedSheetId = null;

        _filter
            .Setup(c => c.GetFilterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string sheetId, CancellationToken __) => capturedSheetId = sheetId)
            .ReturnsAsync(new FeishuApiResult<GetFilterResult>
            {
                Code = 0,
                Data = new GetFilterResult
                {
                    SheetFilterInfo = new SheetRangeFilterInfo
                    {
                        Range = "A1:H9",
                        FilteredOutRows = [2],
                        FilterInfos =
                        [
                            new SheetFilterInfo
                            {
                                Col = "B",
                                Conditions =
                                [
                                    new SheetFilterCondition
                                    {
                                        FilterType = "text_contains",
                                        CompareType = "equals",
                                        Expected = ["x"],
                                    },
                                ],
                            },
                        ],
                    },
                },
            });

        var result = await CreateSheetsTools().GetFilterAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_id", "s1")), CancellationToken.None);

        capturedSheetId.Should().Be("s1", "sheet_id 经 [Path] 下发");
        var payload = Payload(result);
        payload["range"]!.GetValue<string>().Should().Be("A1:H9");
        payload["filtered_out_rows"]!.AsArray()[0]!.GetValue<int>().Should().Be(2);
        var info = payload["filter_infos"]!.AsArray()[0]!;
        info["col"]!.GetValue<string>().Should().Be("B");
        var condition = info["conditions"]!.AsArray()[0]!;
        condition["filter_type"]!.GetValue<string>().Should().Be("text_contains");
        condition["compare_type"]!.GetValue<string>().Should().Be("equals");
        condition["expected"]!.AsArray()[0]!.GetValue<string>().Should().Be("x");
    }

    [Fact]
    public async Task FloatImageGet_ShouldProjectSingleImage()
    {
        string? capturedFloatImageId = null;

        _floatImage
            .Setup(c => c.GetFloatImageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string floatImageId, CancellationToken ___) => capturedFloatImageId = floatImageId)
            .ReturnsAsync(new FeishuApiResult<FloatImageOpsResult>
            {
                Code = 0,
                Data = new FloatImageOpsResult
                {
                    FloatImage = new SheetFloatImageData
                    {
                        FloatImageId = "fi_1",
                        FloatImageToken = "tok_1",
                        Range = "A1:B2",
                        Width = 320f,
                        Height = 240f,
                        OffsetX = 1f,
                        OffsetY = 2f,
                    },
                },
            });

        var result = await CreateSheetsTools().GetFloatImageAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_id", "s1"), ("float_image_id", "fi_1")),
            CancellationToken.None);

        capturedFloatImageId.Should().Be("fi_1", "float_image_id 经 [Path] 下发");
        var image = Payload(result)["float_image"]!;
        image["float_image_id"]!.GetValue<string>().Should().Be("fi_1");
        image["float_image_token"]!.GetValue<string>().Should().Be("tok_1");
        image["range"]!.GetValue<string>().Should().Be("A1:B2");
        image["width"]!.GetValue<double>().Should().Be(320d);
        image["height"]!.GetValue<double>().Should().Be(240d);
    }

    [Fact]
    public async Task FloatImageQuery_ShouldProjectAllItems()
    {
        _floatImage
            .Setup(c => c.GetFloatImagesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetFloatImagesResult>
            {
                Code = 0,
                Data = new GetFloatImagesResult
                {
                    Items =
                    [
                        new SheetFloatImageData { FloatImageId = "fi_1" },
                        new SheetFloatImageData { FloatImageId = "fi_2" },
                    ],
                },
            });

        var result = await CreateSheetsTools().GetFloatImagesAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_id", "s1")), CancellationToken.None);

        var items = Payload(result)["items"]!.AsArray();
        items.Should().HaveCount(2);
        items[1]!["float_image_id"]!.GetValue<string>().Should().Be("fi_2");
    }

    // ───────────────────── sheets 条件格式写（3） ─────────────────────

    [Fact]
    public async Task ConditionFormatCreate_ShouldParseRules_AndProjectResponses()
    {
        CreateConditionFormatRequest? captured = null;

        _condFormat
            .Setup(c => c.CreateConditionFormatsAsync(
                It.IsAny<string>(), It.IsAny<CreateConditionFormatRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateConditionFormatRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<ConditionFormatOpsResult>
            {
                Code = 0,
                Data = new ConditionFormatOpsResult
                {
                    Responses = [new CreateConditionFormatResponse { SheetId = "s1", CfId = "cf_1", ResCode = 0 }],
                },
            });

        var result = await CreateSheetsTools().CreateConditionFormatsAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("condition_formats", "[{\"sheet_id\":\"s1\",\"condition_format\":{\"ranges\":[\"A1:A9\"],\"rule_type\":\"text_contains\",\"attrs\":[{\"operator\":\"contains\",\"text\":\"x\"}]}}]")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.SheetConditionFormats.Should().HaveCount(1);
        captured.SheetConditionFormats[0].SheetId.Should().Be("s1");
        captured.SheetConditionFormats[0].ConditionFormat.RuleType.Should().Be("text_contains");
        captured.SheetConditionFormats[0].ConditionFormat.Attrs[0].Operator.Should().Be("contains");
        captured.SheetConditionFormats[0].ConditionFormat.Attrs[0].Text.Should().Be("x");

        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["cf_id"]!.GetValue<string>().Should().Be("cf_1", "回填新建规则的 cf_id（后续 update/delete 的凭据）");
        item["res_code"]!.GetValue<int>().Should().Be(0);
    }

    [Fact]
    public async Task ConditionFormatUpdate_ShouldPassCfIds()
    {
        UpdateConditionFormatRequest? captured = null;

        _condFormat
            .Setup(c => c.UpdateConditionFormatsAsync(
                It.IsAny<string>(), It.IsAny<UpdateConditionFormatRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, UpdateConditionFormatRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<ConditionFormatOpsResult>
            {
                Code = 0,
                Data = new ConditionFormatOpsResult
                {
                    Responses = [new CreateConditionFormatResponse { SheetId = "s1", CfId = "cf_1", ResCode = 0 }],
                },
            });

        var result = await CreateSheetsTools().UpdateConditionFormatsAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("condition_formats", "[{\"sheet_id\":\"s1\",\"condition_format\":{\"cf_id\":\"cf_1\",\"ranges\":[\"A1:A9\"],\"rule_type\":\"text_contains\"}}]")),
            CancellationToken.None);

        captured!.SheetConditionFormats[0].ConditionFormat.CfId.Should().Be("cf_1", "cf_id 经逐项校验后进入请求体");
        Payload(result)["items"]!.AsArray()[0]!["cf_id"]!.GetValue<string>().Should().Be("cf_1");
    }

    [Fact]
    public async Task ConditionFormatUpdate_WithoutCfId_ShouldReturnStructuredError()
    {
        var result = await CreateSheetsTools().UpdateConditionFormatsAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("condition_formats", "[{\"sheet_id\":\"s1\",\"condition_format\":{\"ranges\":[\"A1:A9\"],\"rule_type\":\"text_contains\"}}]")),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] sheets.cond_format_update");
        result.ToString().Should().Contain("condition_formats 每项的 condition_format.cf_id 必填");
        _condFormat.Verify(
            c => c.UpdateConditionFormatsAsync(
                It.IsAny<string>(), It.IsAny<UpdateConditionFormatRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConditionFormatDelete_ShouldPassSheetCfIds()
    {
        DeleteConditionFormatsRequest? captured = null;

        _condFormat
            .Setup(c => c.DeleteConditionFormatsAsync(
                It.IsAny<string>(), It.IsAny<DeleteConditionFormatsRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, DeleteConditionFormatsRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<ConditionFormatOpsResult>
            {
                Code = 0,
                Data = new ConditionFormatOpsResult
                {
                    Responses = [new CreateConditionFormatResponse { SheetId = "s1", CfId = "cf_1", ResCode = 0 }],
                },
            });

        var result = await CreateSheetsTools().DeleteConditionFormatsAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_cf_ids", "[{\"sheet_id\":\"s1\",\"cf_id\":\"cf_1\"}]")),
            CancellationToken.None);

        captured!.SheetCfIds.Should().HaveCount(1);
        captured.SheetCfIds![0].CfId.Should().Be("cf_1");
        Payload(result)["items"]!.AsArray()[0]!["cf_id"]!.GetValue<string>().Should().Be("cf_1");
    }

    [Fact]
    public async Task ConditionFormatDelete_WithoutCfId_ShouldReturnStructuredError()
    {
        var result = await CreateSheetsTools().DeleteConditionFormatsAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_cf_ids", "[{\"sheet_id\":\"s1\"}]")),
            CancellationToken.None);

        result.ToString().Should().Contain("sheet_cf_ids 每项须同时提供 sheet_id 与 cf_id");
        _condFormat.Verify(
            c => c.DeleteConditionFormatsAsync(
                It.IsAny<string>(), It.IsAny<DeleteConditionFormatsRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConditionFormatDelete_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var result = await CreateSheetsTools().DeleteConditionFormatsAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_cf_ids", "[{\"sheet_id\":\"s1\",\"cf_id\":\"cf_secret\"}]"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain("DELETE /open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/condition_formats/batch_delete");
        text.Should().Contain("幂等键：omitted");
        text.Should().NotContain("cf_secret");
        _condFormat.VerifyNoOtherCalls();
    }

    // ───────────────────── sheets 数据校验写（3） ─────────────────────

    [Fact]
    public async Task DataValidationCreate_ShouldSendListRule_AndReturnReceipt()
    {
        CreateDataValidationRequest? captured = null;

        _dataValidation
            .Setup(c => c.CreateDataValidationAsync(
                It.IsAny<string>(), It.IsAny<CreateDataValidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateDataValidationRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult { Code = 0 });

        var result = await CreateSheetsTools().CreateDataValidationAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("range", "s1!A1:A9"),
                ("condition_values", new[] { "高", "中", "低" })),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Range.Should().Be("s1!A1:A9");
        captured.DataValidationType.Should().Be("list", "data_validation_type 缺省为 list（下拉列表）");
        captured.DataValidation.ConditionValues.Should().Equal("高", "中", "低");
        captured.DataValidation.Options.Should().BeNull("两项选项均未提供时省略 options");

        var payload = Payload(result);
        payload["range"]!.GetValue<string>().Should().Be("s1!A1:A9");
        payload["data_validation_type"]!.GetValue<string>().Should().Be("list");
        payload["created"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task DataValidationCreate_WithOptions_ShouldMapOptions()
    {
        CreateDataValidationRequest? captured = null;

        _dataValidation
            .Setup(c => c.CreateDataValidationAsync(
                It.IsAny<string>(), It.IsAny<CreateDataValidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateDataValidationRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult { Code = 0 });

        await CreateSheetsTools().CreateDataValidationAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("range", "s1!A1:A9"),
                ("condition_values", new[] { "x" }),
                ("multiple_values", true),
                ("highlight_valid_data", true)),
            CancellationToken.None);

        captured!.DataValidation.Options.Should().NotBeNull();
        captured.DataValidation.Options!.MultipleValues.Should().Be(true);
        captured.DataValidation.Options.HighlightValidData.Should().Be(true);
    }

    [Fact]
    public async Task DataValidationCreate_WithEmptyConditionValues_ShouldFailBeforeDownstream()
    {
        var result = await CreateSheetsTools().CreateDataValidationAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("range", "s1!A1:A9"),
                ("condition_values", Array.Empty<string>())),
            CancellationToken.None);

        // 空必填数组在 Unpack（ToolArgs.RequireStringArray）即抛——执行器内层非空检查不可达，
        // 故此处只锁「结构化错误 + 参数名」两件事（参数名由 ToolArgsContractGuards 机械保证）。
        result.ToString().Should().Contain("[tool_error] sheets.data_validation_create");
        result.ToString().Should().Contain("condition_values");
        _dataValidation.Verify(
            c => c.CreateDataValidationAsync(
                It.IsAny<string>(), It.IsAny<CreateDataValidationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DataValidationCreate_WhenApiReturnsErrorCode_ShouldSurfaceCode()
    {
        _dataValidation
            .Setup(c => c.CreateDataValidationAsync(
                It.IsAny<string>(), It.IsAny<CreateDataValidationRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult { Code = 99991663, Msg = "invalid request" });

        var result = await CreateSheetsTools().CreateDataValidationAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("range", "s1!A1:A9"),
                ("condition_values", new[] { "x" })),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] sheets.data_validation_create");
        result.ToString().Should().Contain("99991663", "非泛型 FeishuApiResult 失败也必须回填业务 code");
    }

    [Fact]
    public async Task DataValidationUpdate_ShouldPassSheetIdPathAndRanges_AndEchoRanges()
    {
        string? capturedSheetId = null;
        UpdateDataValidationRequest? captured = null;

        _dataValidation
            .Setup(c => c.UpdateDataValidationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateDataValidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string sheetId, UpdateDataValidationRequest request, CancellationToken __) =>
            {
                capturedSheetId = sheetId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<UpdateDataValidationResult>
            {
                Code = 0,
                Data = new UpdateDataValidationResult
                {
                    SheetId = "s1",
                    DataValidation = new SheetDataValidationInfo { ConditionValues = ["x"] },
                },
            });

        var result = await CreateSheetsTools().UpdateDataValidationAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("ranges", new[] { "s1!A1:A9", "s1!B1:B9" }),
                ("condition_values", new[] { "x" })),
            CancellationToken.None);

        capturedSheetId.Should().Be("s1", "sheet_id 经 [Path] 下发（SDK 参数名 sheetId）");
        captured!.Ranges.Should().Equal("s1!A1:A9", "s1!B1:B9");

        var payload = Payload(result);
        payload["sheet_id"]!.GetValue<string>().Should().Be("s1");
        payload["ranges"]!.AsArray()[1]!.GetValue<string>().Should().Be("s1!B1:B9", "官方回包不带生效区域，ranges 取入参回显");
        payload["condition_values"]!.AsArray()[0]!.GetValue<string>().Should().Be("x");
    }

    [Fact]
    public async Task DataValidationUpdate_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var result = await CreateSheetsTools().UpdateDataValidationAsync(
            Args(
                ("spreadsheet_token", "ss_secret"),
                ("sheet_id", "s1"),
                ("ranges", new[] { "s1!A1:A9" }),
                ("condition_values", new[] { "x" }),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        // 执行器摘要模板手写 {sheet_id}（展示用）；契约路由来自 SDK 特性、为 {sheetId}——两者独立来源，互不覆盖。
        text.Should().Contain("PUT /open-apis/sheets/v2/spreadsheets/{spreadsheet_token}/dataValidation/{sheet_id}");
        text.Should().Contain("幂等键：omitted");
        text.Should().NotContain("ss_secret");
        _dataValidation.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DataValidationUpdate_WithEmptyRanges_ShouldFailBeforeDownstream()
    {
        var result = await CreateSheetsTools().UpdateDataValidationAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("ranges", Array.Empty<string>()),
                ("condition_values", new[] { "x" })),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] sheets.data_validation_update");
        result.ToString().Should().Contain("ranges");
        _dataValidation.Verify(
            c => c.UpdateDataValidationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateDataValidationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task DataValidationDelete_ShouldPassRanges_AndProjectResults()
    {
        DeleteDataValidationRequest? captured = null;

        _dataValidation
            .Setup(c => c.DeleteDataValidationAsync(
                It.IsAny<string>(), It.IsAny<DeleteDataValidationRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, DeleteDataValidationRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<DeleteDataValidationResult>
            {
                Code = 0,
                Data = new DeleteDataValidationResult
                {
                    RangeResults = [new DeleteRangeResult { Range = "s1!A1:A9", Success = true, UpdatedCells = 9 }],
                },
            });

        var result = await CreateSheetsTools().DeleteDataValidationAsync(
            Args(("spreadsheet_token", "ss_1"), ("ranges", new[] { "s1!A1:A9" })),
            CancellationToken.None);

        captured!.DataValidationRanges.Should().HaveCount(1);
        captured.DataValidationRanges[0].Range.Should().Be("s1!A1:A9");

        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["range"]!.GetValue<string>().Should().Be("s1!A1:A9");
        item["success"]!.GetValue<bool>().Should().BeTrue();
        item["updated_cells"]!.GetValue<int>().Should().Be(9);
    }

    [Fact]
    public async Task DataValidationDelete_WithEmptyRanges_ShouldFailBeforeDownstream()
    {
        var result = await CreateSheetsTools().DeleteDataValidationAsync(
            Args(("spreadsheet_token", "ss_1"), ("ranges", Array.Empty<string>())),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] sheets.data_validation_delete");
        result.ToString().Should().Contain("ranges");
        _dataValidation.Verify(
            c => c.DeleteDataValidationAsync(
                It.IsAny<string>(), It.IsAny<DeleteDataValidationRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── sheets 保护范围写（3） ─────────────────────

    [Fact]
    public async Task ProtectedCreate_ShouldPassRowsDimension_AndDefaultUserIdType()
    {
        CreateProtectedRequest? captured = null;
        string? capturedUserIdType = null;

        _protectedRange
            .Setup(c => c.CreateProtectedAsync(
                It.IsAny<string>(), It.IsAny<CreateProtectedRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Callback((string _, CreateProtectedRequest request, string? userIdType, CancellationToken __) =>
            {
                captured = request;
                capturedUserIdType = userIdType;
            })
            .ReturnsAsync(new FeishuApiResult<CreateProtectedResult>
            {
                Code = 0,
                Data = new CreateProtectedResult
                {
                    AddProtectedDimension = [new ProtectedDimensionInfo { ProtectId = "pr_1" }],
                },
            });

        var result = await CreateSheetsTools().CreateProtectedAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("major_dimension", "ROWS"),
                ("start_index", 0),
                ("end_index", 5),
                ("users", new[] { "ou_1" })),
            CancellationToken.None);

        capturedUserIdType.Should().Be("open_id", "user_id_type 缺省为 open_id");
        captured.Should().NotBeNull();
        captured!.AddProtectedDimension.Should().HaveCount(1);
        var dimension = captured.AddProtectedDimension[0].Dimension;
        dimension.SheetId.Should().Be("s1");
        dimension.MajorDimension.Should().Be("ROWS");
        dimension.StartIndex.Should().Be(0);
        dimension.EndIndex.Should().Be(5);
        captured.AddProtectedDimension[0].Users.Should().Equal("ou_1");

        Payload(result)["protect_ids"]!.AsArray()[0]!.GetValue<string>().Should().Be("pr_1");
    }

    [Fact]
    public async Task ProtectedCreate_WithInvalidMajorDimension_ShouldReturnStructuredError()
    {
        var result = await CreateSheetsTools().CreateProtectedAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("major_dimension", "SHEETS"),
                ("start_index", 0),
                ("end_index", 5)),
            CancellationToken.None);

        result.ToString().Should().Contain("major_dimension 必须为 'ROWS'（按行）或 'COLUMNS'（按列）");
        _protectedRange.Verify(
            c => c.CreateProtectedAsync(
                It.IsAny<string>(), It.IsAny<CreateProtectedRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(5, 5)]
    [InlineData(-1, 3)]
    public async Task ProtectedCreate_WithInvalidRange_ShouldReturnStructuredError(int startIndex, int endIndex)
    {
        var result = await CreateSheetsTools().CreateProtectedAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("major_dimension", "ROWS"),
                ("start_index", startIndex),
                ("end_index", endIndex)),
            CancellationToken.None);

        result.ToString().Should().Contain("end_index 须大于 start_index");
        _protectedRange.Verify(
            c => c.CreateProtectedAsync(
                It.IsAny<string>(), It.IsAny<CreateProtectedRequest>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "半开区间非法值必须在下发前拦截");
    }

    [Fact]
    public async Task ProtectedUpdate_ShouldBuildEditorsWithDefaultOpenId_AndProjectReplies()
    {
        UpdateProtectedRequest? captured = null;

        _protectedRange
            .Setup(c => c.UpdateProtectedAsync(
                It.IsAny<string>(), It.IsAny<UpdateProtectedRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, UpdateProtectedRequest request, CancellationToken __) => captured = request)
            .ReturnsAsync(new FeishuApiResult<UpdateProtectedResult>
            {
                Code = 0,
                Data = new UpdateProtectedResult
                {
                    Replies = [new SheetProtectedInfo { SheetId = "s1", ProtectId = "pr_1", LockInfo = "locked" }],
                },
            });

        var result = await CreateSheetsTools().UpdateProtectedAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("protect_id", "pr_1"),
                ("add_editors", new[] { "ou_2" })),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Requests.Should().HaveCount(1);
        captured.Requests[0].ProtectId.Should().Be("pr_1");
        captured.Requests[0].Editors.Should().NotBeNull();
        captured.Requests[0].Editors!.AddEditors.Should().HaveCount(1);
        captured.Requests[0].Editors!.AddEditors![0].MemberType.Should().Be("openid", "编辑器成员类型缺省为 openid（官方枚举）");
        captured.Requests[0].Editors!.AddEditors![0].MemberId.Should().Be("ou_2");
        captured.Requests[0].Editors!.DelEditors.Should().BeNull("仅提供 add_editors 时不得写入空删除列表");

        var item = Payload(result)["items"]!.AsArray()[0]!;
        item["protect_id"]!.GetValue<string>().Should().Be("pr_1");
        item["lock_info"]!.GetValue<string>().Should().Be("locked");
    }

    [Fact]
    public async Task ProtectedUpdate_WithoutAnyItem_ShouldReturnStructuredError()
    {
        var result = await CreateSheetsTools().UpdateProtectedAsync(
            Args(("spreadsheet_token", "ss_1"), ("protect_id", "pr_1")),
            CancellationToken.None);

        result.ToString().Should().Contain("至少提供一个待更新项（add_editors / remove_editors / lock_info）");
        _protectedRange.Verify(
            c => c.UpdateProtectedAsync(
                It.IsAny<string>(), It.IsAny<UpdateProtectedRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── sheets 筛选器写（3） ─────────────────────

    [Fact]
    public async Task FilterCreate_ShouldPassRangeColCondition_AndReturnReceipt()
    {
        CreateFilterRequest? captured = null;

        _filter
            .Setup(c => c.CreateFilterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateFilterRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, CreateFilterRequest request, CancellationToken ___) => captured = request)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateSheetsTools().CreateFilterAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("range", "A1:H9"),
                ("col", "B"),
                ("filter_type", "text_contains"),
                ("compare_type", "equals"),
                ("expected", new[] { "x" })),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Range.Should().Be("A1:H9");
        captured.Col.Should().Be("B");
        captured.Condition.FilterType.Should().Be("text_contains");
        captured.Condition.CompareType.Should().Be("equals");
        captured.Condition.Expected.Should().Equal("x");

        var payload = Payload(result);
        payload["sheet_id"]!.GetValue<string>().Should().Be("s1");
        payload["col"]!.GetValue<string>().Should().Be("B");
        payload["created"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task FilterCreate_WithoutExpected_ShouldSendEmptyArray()
    {
        CreateFilterRequest? captured = null;

        _filter
            .Setup(c => c.CreateFilterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateFilterRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, CreateFilterRequest request, CancellationToken ___) => captured = request)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        await CreateSheetsTools().CreateFilterAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("range", "A1:H9"),
                ("col", "B"),
                ("filter_type", "empty")),
            CancellationToken.None);

        captured!.Condition.Expected.Should().BeEmpty("expected 缺省为空数组（非 null——平台契约要求数组）");
    }

    [Fact]
    public async Task FilterCreate_WhenApiReturnsErrorCode_ShouldSurfaceCode()
    {
        _filter
            .Setup(c => c.CreateFilterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateFilterRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 1310213, Msg = "invalid range" });

        var result = await CreateSheetsTools().CreateFilterAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("range", "A1:H9"),
                ("col", "B"),
                ("filter_type", "text_contains")),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] sheets.filter_create");
        result.ToString().Should().Contain("1310213", "失败回填必须带飞书业务 code（错误分类消费）");
    }

    [Fact]
    public async Task FilterUpdate_ShouldReturnReceipt()
    {
        string? capturedSheetId = null;
        UpdateFilterRequest? captured = null;

        _filter
            .Setup(c => c.UpdateFilterAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<UpdateFilterRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string sheetId, UpdateFilterRequest request, CancellationToken __) =>
            {
                capturedSheetId = sheetId;
                captured = request;
            })
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateSheetsTools().UpdateFilterAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("col", "C"),
                ("filter_type", "text_not_contains")),
            CancellationToken.None);

        capturedSheetId.Should().Be("s1", "sheet_id 经 [Path] 下发");
        captured!.Col.Should().Be("C");
        captured.Condition.FilterType.Should().Be("text_not_contains");

        var payload = Payload(result);
        payload["col"]!.GetValue<string>().Should().Be("C");
        payload["updated"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task FilterDelete_ShouldReturnReceipt()
    {
        string? capturedSheetId = null;

        _filter
            .Setup(c => c.DeleteFilterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string sheetId, CancellationToken __) => capturedSheetId = sheetId)
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateSheetsTools().DeleteFilterAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_id", "s1")), CancellationToken.None);

        capturedSheetId.Should().Be("s1");
        var payload = Payload(result);
        payload["sheet_id"]!.GetValue<string>().Should().Be("s1");
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
    }

    // ───────────────────── sheets 浮动图片写（3） ─────────────────────

    [Fact]
    public async Task FloatImageCreate_ShouldParsePixelValues_AndProjectImage()
    {
        CreateFloatImageRequest? captured = null;

        _floatImage
            .Setup(c => c.CreateFloatImageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateFloatImageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, CreateFloatImageRequest request, CancellationToken ___) => captured = request)
            .ReturnsAsync(new FeishuApiResult<FloatImageOpsResult>
            {
                Code = 0,
                Data = new FloatImageOpsResult
                {
                    FloatImage = new SheetFloatImageData
                    {
                        FloatImageId = "fi_1",
                        FloatImageToken = "tok_1",
                        Range = "A1:B2",
                        Width = 320f,
                    },
                },
            });

        var result = await CreateSheetsTools().CreateFloatImageAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("float_image_token", "tok_1"),
                ("range", "A1:B2"),
                ("width", "320"),
                ("height", "240")),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.FloatImageToken.Should().Be("tok_1");
        captured.Range.Should().Be("A1:B2");
        captured.Width.Should().Be(320f, "像素数值字符串经 ParsePixelValue 转 float");
        captured.Height.Should().Be(240f);
        captured.OffsetX.Should().BeNull("未提供的像素字段保持 null");

        var image = Payload(result)["float_image"]!;
        image["float_image_id"]!.GetValue<string>().Should().Be("fi_1");
        image["width"]!.GetValue<double>().Should().Be(320d);
    }

    [Fact]
    public async Task FloatImageCreate_WithNonNumericWidth_ShouldReturnStructuredError()
    {
        var result = await CreateSheetsTools().CreateFloatImageAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("float_image_token", "tok_1"),
                ("range", "A1:B2"),
                ("width", "abc")),
            CancellationToken.None);

        result.ToString().Should().Contain("[tool_error] sheets.float_image_create");
        result.ToString().Should().Contain("width 须为数值字符串");
        result.ToString().Should().Contain("'abc'", "错误文案回显非法原文供模型自查");
        _floatImage.Verify(
            c => c.CreateFloatImageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CreateFloatImageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FloatImageUpdate_ShouldPassFloatImageIdPath_AndBackfillImage()
    {
        string? capturedFloatImageId = null;
        UpdateFloatImageRequest? captured = null;

        _floatImage
            .Setup(c => c.UpdateFloatImageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UpdateFloatImageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((string _, string __, string floatImageId, UpdateFloatImageRequest request, CancellationToken ___) =>
            {
                capturedFloatImageId = floatImageId;
                captured = request;
            })
            .ReturnsAsync(new FeishuApiResult<FloatImageOpsResult>
            {
                Code = 0,
                Data = new FloatImageOpsResult
                {
                    FloatImage = new SheetFloatImageData { FloatImageId = "fi_9", Width = 400f },
                },
            });

        var result = await CreateSheetsTools().UpdateFloatImageAsync(
            Args(
                ("spreadsheet_token", "ss_1"),
                ("sheet_id", "s1"),
                ("float_image_id", "fi_9"),
                ("width", "400")),
            CancellationToken.None);

        capturedFloatImageId.Should().Be("fi_9", "float_image_id 经 [Path] 下发");
        captured!.Width.Should().Be(400f);
        captured.Range.Should().BeNull("未提供的字段保持 null（PATCH 语义：不覆盖）");
        Payload(result)["float_image"]!["width"]!.GetValue<double>().Should().Be(400d);
    }

    [Fact]
    public async Task FloatImageUpdate_WithoutAnyField_ShouldReturnStructuredError()
    {
        var result = await CreateSheetsTools().UpdateFloatImageAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_id", "s1"), ("float_image_id", "fi_9")),
            CancellationToken.None);

        result.ToString().Should().Contain("至少提供一个待更新字段（range / width / height / offset_x / offset_y）");
        _floatImage.Verify(
            c => c.UpdateFloatImageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<UpdateFloatImageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task FloatImageDelete_ShouldReturnReceipt()
    {
        string? capturedToken = null;
        string? capturedSheetId = null;
        string? capturedFloatImageId = null;

        _floatImage
            .Setup(c => c.DeleteFloatImageAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string token, string sheetId, string floatImageId, CancellationToken _) =>
            {
                capturedToken = token;
                capturedSheetId = sheetId;
                capturedFloatImageId = floatImageId;
            })
            .ReturnsAsync(new FeishuNullDataApiResult { Code = 0 });

        var result = await CreateSheetsTools().DeleteFloatImageAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_id", "s1"), ("float_image_id", "fi_9")),
            CancellationToken.None);

        capturedToken.Should().Be("ss_1");
        capturedSheetId.Should().Be("s1");
        capturedFloatImageId.Should().Be("fi_9", "float_image_id 经 [Path] 下发");
        var payload = Payload(result);
        payload["sheet_id"]!.GetValue<string>().Should().Be("s1");
        payload["float_image_id"]!.GetValue<string>().Should().Be("fi_9");
        payload["deleted"]!.GetValue<bool>().Should().BeTrue();
    }

    [Fact]
    public async Task FloatImageDelete_DryRun_ShouldNotCallDownstream_AndReturnRoute()
    {
        var result = await CreateSheetsTools().DeleteFloatImageAsync(
            Args(("spreadsheet_token", "ss_1"), ("sheet_id", "s1"), ("float_image_id", "fi_secret"), ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().StartWith("[dry_run]");
        text.Should().Contain(
            "DELETE /open-apis/sheets/v3/spreadsheets/{spreadsheet_token}/sheets/{sheet_id}/float_images/{float_image_id}");
        text.Should().Contain("幂等键：omitted");
        text.Should().NotContain("fi_secret");
        _floatImage.VerifyNoOtherCalls();
    }
}
