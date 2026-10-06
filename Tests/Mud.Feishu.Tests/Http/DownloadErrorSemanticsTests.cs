// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Mud.Feishu.Tests.Http;

/// <summary>
/// API-1：锁定「<c>[Get]</c> + <c>Task&lt;byte[]?&gt;</c>」下载方法的错误语义。
/// </summary>
/// <remarks>
/// <para>
/// 全 SDK 统一返回 <c>FeishuApiResult&lt;T&gt;?</c>（承载 <c>Code/Msg/Data</c>），但存在 <b>16 个</b>
/// <c>Task&lt;byte[]?&gt;</c> 下载方法完全丢失该错误通道，因此其行为必须被<b>显式锁定</b>：
/// <c>IFeishuV1Message_Tenant.GetMessageFile</c>、<c>DownFileAsync</c>、<c>DownImageAsync</c>、
/// <c>IFeishuV1DriveFiles.DownloadFileAsync</c>、<c>DownloadExportFileAsync</c>、
/// <c>IFeishuV1DriveMedia.DownloadFileAsync</c>、<c>IFeishuV1Board.DownloadWhiteboardImageAsync</c>、
/// <c>IFeishuV1AttendanceUserSettings_Tenant.DownloadFileAsync</c>、
/// <c>IFeishuV1LingoFile.DownloadFileAsync</c>、
/// <c>IFeishuV1HelpDeskTicket_Tenant.GetTicketImageAsync</c>、
/// <c>IFeishuV1HelpDeskFaq_Tenant.GetFaqImageAsync</c>、
/// <c>IFeishuV1MinutesMinute.GetMinuteTranscriptAsync</c>、
/// <c>IFeishuV1SparkAppStorage_User.DownloadStorageAsync</c>、
/// <c>IFeishuV1VideoConferencingExports.DownloadExportAsync</c>、
/// <c>IFeishuV1AcsUser_Tenant.DownloadUserFaceAsync</c>、
/// <c>IFeishuV1AcsAccessRecord_Tenant.DownloadAccessRecordAccessPhotoAsync</c>。
/// </para>
/// <para>
/// ⚠️<b>R5 / B-2 修订</b>：原注释写「存在 <b>9 个</b>」并只列了 10 个名字，<b>计数与清单双重不符</b>
/// （漏掉 Lingo / Minutes / Spark / Acs 两个 / HelpDeskFaq 共 6 个）。
/// 现状以 <c>BinaryDownloadToolExposureContractTests</c> 的实测口径为准（<b>16 个/ 13 个接口</b>）。
/// </para>
/// <para>
/// <b>与 AI 工具面的关系（R5 / B-2 核心结论）</b>：这 16 个下载方法<b>全部未暴露为 AI 工具</b>——
/// 其所属 13 个接口的 <c>[FeishuTool]</c> 计数<b>均为 0</b>，因此二进制<b>当前不可能</b>进入工具
/// JSON 结果。该不变量由 <c>BinaryDownloadToolExposureContractTests</c> 机械锁定；
/// 本测试则锁定其<b>错误语义</b>（非 2xx 抛 <c>ApiException</c>，错误体进入异常）。
/// </para>
/// <para>
/// 生成器对可空 <c>byte[]</c> 返回类型走「下载分支」，直接调用
/// <c>IHttpRequestExecutor.DownloadAsync</c>（不经 JSON 反序列化，<c>ResponseDescriptor</c> 未启用
/// <c>AllowAnyStatusCode</c>），故本测试直接在执行器层面锁定该契约，等价于对这 16 个方法的语义锁定。
/// </para>
/// </remarks>
public class DownloadErrorSemanticsTests
{
    private const string ClientName = "feishu-default";
    private const string RequestUrl = "https://open.feishu.cn/open-apis/im/v1/files/file_456a92d6";

    [Fact]
    public async Task DownloadAsync_ShouldThrowApiException_WhenServerReturnsNon2xx()
    {
        var errorBody = """{"code":230020,"msg":"file not found"}"""u8.ToArray();
        using var provider = BuildProvider(HttpStatusCode.NotFound, errorBody, "application/json");
        var (executor, client) = ResolveExecutorAndClient(provider);

        using var request = new HttpRequestMessage(HttpMethod.Get, RequestUrl);

        var exception = await Assert.ThrowsAsync<ApiException>(() => executor.DownloadAsync(request, client));

        exception.StatusCode.Should().Be(HttpStatusCode.NotFound);
        exception.Content.Should().Contain("230020",
            "错误响应体必须进入异常，调用方才能区分「失败」与「成功但内容为空」");
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnRawBytes_WhenServerReturnsBinaryContent()
    {
        var payload = new byte[] { 0x01, 0x02, 0x03, 0x04 };
        using var provider = BuildProvider(HttpStatusCode.OK, payload, "application/octet-stream");
        var (executor, client) = ResolveExecutorAndClient(provider);

        using var request = new HttpRequestMessage(HttpMethod.Get, RequestUrl);

        var bytes = await executor.DownloadAsync(request, client);

        bytes.Should().Equal(payload, "成功路径必须原样返回二进制内容，不经任何反序列化");
    }

    [Fact]
    public async Task DownloadAsync_ShouldReturnErrorJsonBytes_WhenServerReturnsHttp200WithJsonErrorBody()
    {
        // 残余风险锁定：飞书部分业务错误以 HTTP 200 + JSON 错误体返回。
        // 此时执行器（及 9 个下载方法）会把错误 JSON 当作文件内容返回 —— 本用例把该事实固化，
        // 使任何行为变化都必须是有意识的决策，而不是静默漂移。
        // 调用方落盘前应按 Content-Type 自检，见 documents/ErrorHandling.md。
        var errorJson = """{"code":99991672,"msg":"invalid access token"}"""u8.ToArray();
        using var provider = BuildProvider(HttpStatusCode.OK, errorJson, "application/json");
        var (executor, client) = ResolveExecutorAndClient(provider);

        using var request = new HttpRequestMessage(HttpMethod.Get, RequestUrl);

        var bytes = await executor.DownloadAsync(request, client);

        bytes.Should().NotBeNull();
        System.Text.Encoding.UTF8.GetString(bytes!).Should().Contain("99991672",
            "HTTP 200 + JSON 错误体不会被识别为错误，调用方必须自行校验 Content-Type / code");
    }

    private static (IHttpRequestExecutor Executor, IBaseHttpClient Client) ResolveExecutorAndClient(ServiceProvider provider)
        => (provider.GetRequiredService<IHttpRequestExecutor>(), provider.GetRequiredService<IBaseHttpClient>());

    private static ServiceProvider BuildProvider(HttpStatusCode statusCode, byte[] content, string contentType)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddFeishuApp(new List<FeishuAppConfig>
        {
            new()
            {
                AppKey = "default",
                AppId = "cli_download_test_1234567890",
                AppSecret = "download_test_secret_1234",
                IsDefault = true
            }
        });

        // 用桩处理器替换传输层：请求仍走真实的命名客户端 / 增强客户端 / 执行器链路。
        services.AddHttpClient(ClientName)
            .ConfigurePrimaryHttpMessageHandler(() => new StubHandler(statusCode, content, contentType));

        return services.BuildServiceProvider();
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly byte[] _content;
        private readonly string _contentType;

        public StubHandler(HttpStatusCode statusCode, byte[] content, string contentType)
        {
            _statusCode = statusCode;
            _content = content;
            _contentType = contentType;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var content = new ByteArrayContent(_content);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(_contentType);

            return Task.FromResult(new HttpResponseMessage(_statusCode)
            {
                Content = content,
                RequestMessage = request
            });
        }
    }
}
