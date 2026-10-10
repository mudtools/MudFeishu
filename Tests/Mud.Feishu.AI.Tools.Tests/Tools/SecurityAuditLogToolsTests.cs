// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// F-1 P0 用例：Security 行为审计日志（<c>security.query_audit_logs</c>）执行链行为。
/// </summary>
/// <remarks>
/// <para>
/// 除常规四条（投影 / 空哨兵 / 本地校验 / 软缺席）外，本组额外锁两件事：
/// ① <b>PII 最小披露</b>——Mac 地址与企业编号<b>不得</b>出现在结果里（它们是"能拿到但不该给模型"的字段）；
/// ② <b>分页信封语义</b>——空 <c>page_token</c> 不写入（否则模型会照抄空 token 续页 → 重复拉第一页）。
/// </para>
/// </remarks>
public class SecurityAuditLogToolsTests
{
    private static SecurityAuditLogTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1SecurityAuditLog>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    private static Mock<Mud.Feishu.IFeishuTenantV1SecurityAuditLog> ClientWith(GetAuditLogDataResult data)
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1SecurityAuditLog>();
        client
            .Setup(c => c.GetAuditLogDataAsync(It.IsAny<GetAuditLogDataQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<GetAuditLogDataResult> { Code = 0, Data = data });
        return client;
    }

    [Fact]
    public async Task QueryAuditLogs_ShouldProjectWhitelistedFields_AndDropDeviceFingerprints()
    {
        var client = ClientWith(new GetAuditLogDataResult
        {
            HasMore = false,
            Items =
            [
                new SecurityAuditInfo
                {
                    EventId = "7254062411181719572",
                    UniqueId = "7254062413199179796",
                    EventName = "space_edit_doc",
                    EventModule = 1,
                    OperatorType = 1,
                    OperatorValue = "ou_operator",
                    EventTime = 1688968015,
                    Ip = "203.0.113.7",
                    DepartmentIds = ["od_dept"],
                    OperatorTenant = "F686619755",
                    Objects =
                    [
                        new SecurityAuditObjectEntity { ObjectType = "106", ObjectValue = "doxcnX", ObjectName = "季度规划" },
                    ],
                    AuditDetail = new SecurityAuditDetail
                    {
                        City = "杭州",
                        DeviceModel = "MacBookPro18,3",
                        Os = "macOS",
                        Mc = "AA:BB:CC:DD:EE:FF",
                    },
                },
            ],
        });
        var tools = CreateTools(client);

        var result = await tools.QueryAuditLogsAsync(Args(("event_name", "space_edit_doc")), CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("space_edit_doc");
        text.Should().Contain("ou_operator");
        text.Should().Contain("杭州", "地理位置是合规排查的必要事实");

        // PII 最小披露（有意丢弃的字段）
        text.Should().NotContain("AA:BB:CC:DD:EE:FF", "Mac 地址是最强设备指纹——能拿到但不应给模型");
        text.Should().NotContain("F686619755", "操作人企业编号对本类排查无增量信息");

        // 分页信封：has_more 恒在；page_token 为空时**不写入**（DP-B4 语义）
        text.Should().Contain("\"has_more\":false");
        text.Should().NotContain("\"page_token\"", "空 token 不写入——否则模型会照抄空 token 续页（重复拉第一页）");
    }

    [Fact]
    public async Task QueryAuditLogs_ShouldReturnNotFoundSentinel_WhenEmpty()
    {
        var tools = CreateTools(ClientWith(new GetAuditLogDataResult { HasMore = false, Items = [] }));

        var result = await tools.QueryAuditLogsAsync(Args(), CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("\"found\":false", "空结果必须给显式哨兵（F-5）");
        text.Should().Contain("未查询到审计日志");
    }

    [Theory]
    [InlineData(0, 0, "latest 必须晚于 oldest")]
    [InlineData(0, 3600 * 24 * 31, "相差不能超过 30 天")]
    public async Task QueryAuditLogs_ShouldRejectInvalidTimeRange_WithoutCallingDownstream(
        int oldest, int latest, string expectedFragment)
    {
        var client = ClientWith(new GetAuditLogDataResult { Items = [] });
        var tools = CreateTools(client);

        var result = await tools.QueryAuditLogsAsync(
            Args(("oldest", oldest), ("latest", latest)),
            CancellationToken.None);

        result.Error.Should().NotBeNull("平台硬约束必须在**本地**拒绝（F-8：不消耗下游调用）");
        result.Error!.Subtype.Should().Be("invalid_args");
        result.ToString().Should().Contain(expectedFragment);
        client.Verify(
            c => c.GetAuditLogDataAsync(It.IsAny<GetAuditLogDataQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task QueryAuditLogs_ShouldRejectOperatorValueWithoutType()
    {
        var client = ClientWith(new GetAuditLogDataResult { Items = [] });
        var tools = CreateTools(client);

        var result = await tools.QueryAuditLogsAsync(
            Args(("operator_value", "ou_operator")),
            CancellationToken.None);

        result.Error.Should().NotBeNull("operator_value 必须与 operator_type 成对（平台隐式约束）");
        result.ToString().Should().Contain("operator_type");
        client.Verify(
            c => c.GetAuditLogDataAsync(It.IsAny<GetAuditLogDataQuery>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task QueryAuditLogs_ShouldFailFast_WhenClientIsAbsent()
    {
        var tools = CreateTools();

        var result = await tools.QueryAuditLogsAsync(Args(), CancellationToken.None);

        result.Error.Should().BeNull("软缺席走文本错误出口（既有执行器同口径）");
        result.ToString().Should().Contain("IFeishuTenantV1SecurityAuditLog", "必须点名缺失的依赖");
        result.ToString().Should().Contain("security.query_audit_logs", "必须点明是哪个工具不可用");
    }
}
