// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.DataModels.Security;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// F-1 收官用例：security 应用 API 调用日志（<c>security.list_openapi_logs</c>）。
/// </summary>
/// <remarks>
/// 锁两件本工具最要紧的事：
/// ① <b>原始载荷一律不带出</b>——<c>payload</c> / <c>response</c> / <c>query_param</c> 可能含业务数据或个人信息，
///    只给"路径 + 方法 + 状态码"的最小证据集（这是本工具能否进模型上下文的前提）；
/// ② <b>时间窗本地拦</b>——超过 30 天直接在本地拒绝，一次网络请求都不该发出。
/// </remarks>
public class SecurityOpenApiLogToolsTests
{
    private static SecurityOpenApiLogTools CreateTools(Mock<Mud.Feishu.IFeishuTenantV1SecurityOpenApiLog>? client = null)
        => new(Options.Create(new FeishuAgentOptions { Instructions = "test" }), client?.Object);

    [Fact]
    public async Task ListOpenApiLogs_ShouldDropRawPayloads()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1SecurityOpenApiLog>();
        client
            .Setup(c => c.ListOpenApiLogDataAsync(It.IsAny<ListOpenApiLogDataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ListOpenApiLogDataResult>
            {
                Code = 0,
                Data = new ListOpenApiLogDataResult
                {
                    HasMore = true,
                    PageToken = "next",
                    Items =
                    [
                        new SecurityOpenApiLog
                        {
                            Id = "log-1",
                            ApiKey = "im/v1/messages",
                            EventTime = 1705312800,
                            AppId = "cli_a1b2c3",
                            Ip = "10.0.0.1",
                            LogDetail = new SecurityOpenApiLogDetail
                            {
                                Path = "/open-apis/im/v1/messages",
                                Method = "POST",
                                StatusCode = 200,
                                Payload = "{\"secret_token\":\"TOP_SECRET\"}",
                                Response = "{\"user_mobile\":\"13800000000\"}",
                                QueryParam = "token=TOP_SECRET",
                            },
                        },
                    ],
                },
            });

        var result = await CreateTools(client).ListOpenApiLogsAsync(
            new Dictionary<string, object?> { ["api_key"] = "im/v1/messages" },
            CancellationToken.None);

        result.Error.Should().BeNull();
        var text = result.ToString();
        text.Should().Contain("/open-apis/im/v1/messages", "定位问题必须能拿到接口路径");
        text.Should().Contain("\"status_code\":200");
        text.Should().NotContain("TOP_SECRET", "原始 payload / query_param 一律不得带出（可能含凭据或业务数据）");
        text.Should().NotContain("13800000000", "原始 response 一律不得带出（可能含个人信息）");

        client.Verify(
            c => c.ListOpenApiLogDataAsync(
                It.Is<ListOpenApiLogDataRequest>(r => r.ApiKeys != null && r.ApiKeys.Length == 1 && r.ApiKeys[0] == "im/v1/messages"),
                It.IsAny<CancellationToken>()),
            Times.Once,
            "api_key 是业务过滤，必须透传");
    }

    [Fact]
    public async Task ListOpenApiLogs_ShouldRejectWindowOver30Days_WithoutNetworkCall()
    {
        var client = new Mock<Mud.Feishu.IFeishuTenantV1SecurityOpenApiLog>();

        var result = await CreateTools(client).ListOpenApiLogsAsync(
            new Dictionary<string, object?>
            {
                ["oldest"] = 1700000000,
                ["latest"] = 1700000000 + (40 * 24 * 60 * 60),
            },
            CancellationToken.None);

        result.ToString().Should().Contain("30 天", "时间窗超限必须本地拦下");
        client.Verify(
            c => c.ListOpenApiLogDataAsync(It.IsAny<ListOpenApiLogDataRequest>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "本地拦截的意义就是不浪费一次网络往返");
    }
}
