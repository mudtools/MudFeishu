// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.OkrV2;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// OKR 指标面（<c>okr.get_objective_indicator</c> / <c>okr.get_key_result_indicator</c> /
/// <c>okr.update_indicator</c>）的链路用例。
/// </summary>
/// <remarks>
/// <para>
/// <b>读面在 <see cref="OkrTools"/>、写面在 <see cref="OkrWriteTools"/>，共用同一份指标投影</b>
/// （<c>OkrTools.ProjectIndicator</c>）——写回执与读结果若字段口径不一致，模型下一步 PATCH 就会
/// 拿不到 <c>indicator_id</c>。这里用同一条断言链锁住两侧。
/// </para>
/// <para>
/// <b>平台没有「按 indicator_id 反查指标」的接口</b>，实体归属（Objective 不能改起始值/目标值/单位）
/// 只有读过之后才知道，故工具层只做<b>无需读即可判定</b>的本地拒绝：数值范围、整数闭集、
/// 单位成对、<c>indicator_status</c> 与非手动状态计算方式的冲突。
/// </para>
/// </remarks>
public class OkrIndicatorToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV2OkrIndicator> _indicatorClient = new();

    private OkrTools CreateReadTools()
        => new(
            null, null, null, null, null, null,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            _indicatorClient.Object);

    private OkrWriteTools CreateWriteTools()
        => new(
            null, null, null,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }),
            _indicatorClient.Object);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static FeishuApiResult<IndicatorResult> IndicatorResponse(Indicator indicator)
        => new() { Code = 0, Data = new IndicatorResult { Indicator = indicator } };

    private static Indicator SampleIndicator() => new()
    {
        Id = "ind_1",
        EntityType = 1,
        EntityId = "obj_1",
        Owner = new Owner { OwnerType = "user", UserId = "ou_owner" },
        IndicatorStatus = 0,
        StatusCalculateType = 0,
        CurrentValueCalculateType = 0,
        StartValue = 0,
        TargetValue = 100,
        CurrentValue = 42.5,
        Unit = new IndicatorUnit { UnitType = 0, UnitValue = "PERCENT" },
        UpdateTime = "1730000000000",
    };

    // ───────────────────── 契约事实 ─────────────────────

    [Theory]
    [InlineData("okr.get_objective_indicator", "GET", "/open-apis/okr/v2/objectives/{objective_id}/indicators", "okr:okr.content:readonly")]
    [InlineData("okr.get_key_result_indicator", "GET", "/open-apis/okr/v2/key_results/{key_result_id}/indicators", "okr:okr.content:readonly")]
    [InlineData("okr.update_indicator", "PATCH", "/open-apis/okr/v2/indicators/{indicator_id}", "okr:okr.content:writeonly")]
    public void Contracts_ShouldCarrySdkMethodAndRoute(string toolName, string httpMethod, string route, string scope)
    {
        var contract = FeishuToolContracts.ByToolName[toolName];

        contract.HttpMethod.Should().Be(httpMethod);
        contract.Route.Should().Be(route, "PATCH 走 indicators/{indicator_id}——与目标/关键结果两条读路径不同层");
        contract.Identity.Should().Be("tenant");
        contract.Risk.Should().Be(httpMethod == "GET" ? FeishuToolRisk.Read : FeishuToolRisk.Write);
        contract.IsWrite.Should().Be(httpMethod != "GET");
        contract.RequiredScopes.Should().Contain(scope);
    }

    // ───────────────────── 读面：indicator_id 的唯一来源 ─────────────────────

    [Fact]
    public async Task GetObjectiveIndicator_ShouldPassObjectiveId_AndProjectIndicatorWhitelist()
    {
        string? capturedObjectiveId = null;
        _indicatorClient
            .Setup(c => c.GetObjectiveIndicatorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string objectiveId, string _, string __, CancellationToken ___) => capturedObjectiveId = objectiveId)
            .ReturnsAsync(IndicatorResponse(SampleIndicator()));

        var text = (await CreateReadTools().GetObjectiveIndicatorAsync(
            Args(("objective_id", "obj_1")), CancellationToken.None)).ToString()!;

        capturedObjectiveId.Should().Be("obj_1", "objective_id 经 [Path] 下发");
        using var document = JsonDocument.Parse(text);
        var root = document.RootElement;
        root.GetProperty("indicator_id").GetString().Should().Be("ind_1",
            "indicator_id 是 okr.update_indicator 的唯一入口，投影里丢它即断链");
        root.GetProperty("entity_id").GetString().Should().Be("obj_1");
        root.GetProperty("entity_type").GetInt32().Should().Be(1);
        root.GetProperty("owner_id").GetString().Should().Be("ou_owner", "归属者只投影 user_id，不透出 owner 原始对象");
        root.GetProperty("current_value").GetDouble().Should().Be(42.5);
        root.GetProperty("target_value").GetDouble().Should().Be(100);
        root.GetProperty("unit_type").GetInt32().Should().Be(0);
        root.GetProperty("unit_value").GetString().Should().Be("PERCENT");
        root.GetProperty("status_calculate_type").GetInt32().Should().Be(0);
        root.GetProperty("current_value_calculate_type").GetInt32().Should().Be(0);
    }

    [Fact]
    public async Task GetKeyResultIndicator_ShouldPassKeyResultId_AndShareTheReadProjection()
    {
        string? capturedKeyResultId = null;
        _indicatorClient
            .Setup(c => c.GetKeyResultIndicatorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string keyResultId, string _, string __, CancellationToken ___) => capturedKeyResultId = keyResultId)
            .ReturnsAsync(IndicatorResponse(SampleIndicator()));

        var read = (await CreateReadTools().GetKeyResultIndicatorAsync(
            Args(("key_result_id", "kr_1")), CancellationToken.None)).ToString()!;

        capturedKeyResultId.Should().Be("kr_1");
        using var document = JsonDocument.Parse(read);
        document.RootElement.GetProperty("indicator_id").GetString().Should().Be("ind_1");
    }

    [Fact]
    public async Task GetObjectiveIndicator_WhenPlatformOmitsIndicator_ShouldExplainTheMissingFace()
    {
        _indicatorClient
            .Setup(c => c.GetObjectiveIndicatorAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<IndicatorResult> { Code = 0, Data = new IndicatorResult() });

        var text = (await CreateReadTools().GetObjectiveIndicatorAsync(
            Args(("objective_id", "obj_1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("平台未返回指标", "指标面与进展面是两套面——空结果必须给出这个区分，否则模型反复重读");
        text.Should().Contain("进展记录");
    }

    [Fact]
    public async Task IndicatorReadSurface_WhenIndicatorClientMissing_ShouldNameTheSoftDependency()
    {
        var tools = new OkrTools(
            null, null, null, null, null, null,
            Options.Create(new FeishuAgentOptions { Instructions = "test" }));

        var text = (await tools.GetObjectiveIndicatorAsync(
            Args(("objective_id", "obj_1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.get_objective_indicator");
        text.Should().Contain("IFeishuTenantV2OkrIndicator", "指标侧是软依赖：缺席只影响这三枚工具，须说清该启用什么");
        _indicatorClient.VerifyNoOtherCalls();
    }

    // ───────────────────── 写面：okr.update_indicator ─────────────────────

    [Fact]
    public async Task UpdateIndicator_ShouldPatchValuesCalculationAndUnit_AndReturnReadShape()
    {
        string? capturedIndicatorId = null;
        PatchIndicatorRequest? captured = null;

        _indicatorClient
            .Setup(c => c.UpdateIndicatorAsync(
                It.IsAny<string>(), It.IsAny<PatchIndicatorRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((string indicatorId, PatchIndicatorRequest request, string _, string __, CancellationToken ___) =>
            {
                capturedIndicatorId = indicatorId;
                captured = request;
            })
            .ReturnsAsync(IndicatorResponse(SampleIndicator()));

        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(
                ("indicator_id", "ind_1"),
                ("current_value", "42.5"),
                ("start_value", "0"),
                ("target_value", "100"),
                ("current_value_calculate_type", 0),
                ("status_calculate_type", 0),
                ("indicator_status", 1),
                ("unit_type", 0),
                ("unit_value", "PERCENT")),
            CancellationToken.None)).ToString()!;

        capturedIndicatorId.Should().Be("ind_1", "指标按 indicator_id 定位（PATCH 端点不带实体类型）");
        captured!.CurrentValue.Should().Be(42.5, "字符串入参在执行器内按不变文化解析（MUDFT020：解包映射不覆盖 double）");
        captured.StartValue.Should().Be(0);
        captured.TargetValue.Should().Be(100);
        captured.CurrentValueCalculateType.Should().Be(0);
        captured.StatusCalculateType.Should().Be(0);
        captured.IndicatorStatus.Should().Be(1);
        captured.Unit!.UnitType.Should().Be(0);
        captured.Unit.UnitValue.Should().Be("PERCENT");

        // 写回执与读结果同一份投影——模型据此继续下一步读写，字段口径不得分叉。
        using var document = JsonDocument.Parse(text);
        document.RootElement.GetProperty("indicator_id").GetString().Should().Be("ind_1");
        document.RootElement.GetProperty("unit_value").GetString().Should().Be("PERCENT");
    }

    [Fact]
    public async Task UpdateIndicator_WhenCustomUnitExceedsFiveChars_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("unit_type", 1), ("unit_value", "MAN-HOUR")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("不超过 5 个字符");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_WhenUnitProvidedHalf_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("unit_type", 0)), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("成对提供", "只给 unit_type 是半个请求体——平台会静默保留旧单位值");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_WhenPublicUnitValueUnknown_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("unit_type", 0), ("unit_value", "KM")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("PERCENT/NONE/YUAN/DOLLAR");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_WhenNoFieldToUpdate_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("至少提供一个要更新的字段", "空 PATCH 会原样返回成功——模型会误判已更新");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_WhenValueOutOfRange_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("current_value", "1000000000000")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("-99999999999 ~ 99999999999", "越界值平台报错不带字段名，本地给出可自我纠正的文案");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_WhenCalculateTypeNotInClosedSet_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("status_calculate_type", 5)), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("status_calculate_type 仅支持 0 / 1 / 2");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_WhenStatusSuppliedWithAutoCalculation_ShouldReject_WithoutCallingDownstream()
    {
        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("indicator_status", 1), ("status_calculate_type", 1)),
            CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("status_calculate_type=0", "indicator_status 只在手动计算方式下可改——本地拦省一次平台无字段名报错");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_DryRun_StillValidates_AndDoesNotEchoTheValue()
    {
        var invalid = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("current_value", "not-a-number"), ("dry_run", true)),
            CancellationToken.None)).ToString()!;
        invalid.Should().Contain("[tool_error] okr.update_indicator", "校验先于预演：预演通过、实调失败毫无价值");

        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("current_value", "42.5"), ("dry_run", true)),
            CancellationToken.None)).ToString()!;

        text.Should().StartWith("[dry_run]");
        text.Should().Contain("PATCH /open-apis/okr/v2/indicators/{indicator_id}");
        text.Should().Contain("不支持幂等键", "PATCH 无幂等键——重复调用即重复写入，必须提示");
        text.Should().Contain("先读指标", "实体归属约束（Objective 不支持起始值/目标值/单位）只有读过才知道");
        text.Should().NotContain("42.5", "预演摘要不回显取值原文");
        _indicatorClient.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateIndicator_WhenPlatformFails_ShouldReturnStructuredErrorWithCode()
    {
        _indicatorClient
            .Setup(c => c.UpdateIndicatorAsync(
                It.IsAny<string>(), It.IsAny<PatchIndicatorRequest>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<IndicatorResult> { Code = 90001, Msg = "no permission" });

        var text = (await CreateWriteTools().UpdateIndicatorAsync(
            Args(("indicator_id", "ind_1"), ("current_value", "1")), CancellationToken.None)).ToString()!;

        text.Should().Contain("[tool_error] okr.update_indicator");
        text.Should().Contain("90001");
    }
}
