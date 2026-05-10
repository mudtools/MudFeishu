// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 网络连接测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class NetworkTestController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<NetworkTestController> _logger;

    public NetworkTestController(IHttpClientFactory httpClientFactory, ILogger<NetworkTestController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// 测试飞书API连接
    /// </summary>
    [HttpGet("test-feishu-connection")]
    public async Task<IActionResult> TestFeishuConnection()
    {
        var result = new Dictionary<string, object>();

        try
        {
            _logger.LogInformation("开始测试飞书API连接...");

            var client = _httpClientFactory.CreateClient("feishu-default");

            // 测试基本连接
            _logger.LogInformation("测试基本连接到飞书API...");
            var startTime = DateTime.UtcNow;
            var response = await client.GetAsync("/open-apis/auth/v3/tenant_access_token/internal");
            var elapsedTime = DateTime.UtcNow - startTime;

            result.Add("基本连接测试", new
            {
                成功 = response.IsSuccessStatusCode,
                状态码 = (int)response.StatusCode,
                响应时间毫秒 = elapsedTime.TotalMilliseconds,
                内容长度 = response.Content.Headers.ContentLength,
                ContentType = response.Content.Headers.ContentType
            });

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("响应内容: {Content}", content);
            result.Add("响应内容", content);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP请求异常");
            result.Add("HttpRequestException", new
            {
                Message = ex.Message,
                StatusCode = ex.StatusCode?.ToString(),
                InnerException = ex.InnerException?.Message
            });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "任务取消异常");
            result.Add("TaskCanceledException", new
            {
                Message = ex.Message,
                IsTimeout = !ex.CancellationToken.IsCancellationRequested
            });
        }
        catch (System.Threading.ThreadAbortException ex)
        {
            _logger.LogError(ex, "线程中止异常");
            result.Add("ThreadAbortException", new
            {
                Message = ex.Message,
                可能原因 = "网络中断、服务器关闭连接或SSL握手失败"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未知异常");
            result.Add("Exception", new
            {
                Type = ex.GetType().Name,
                Message = ex.Message,
                StackTrace = ex.StackTrace
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// 测试网络环境
    /// </summary>
    [HttpGet("test-network-environment")]
    public IActionResult TestNetworkEnvironment()
    {
        var result = new Dictionary<string, object>();

        try
        {
            // 测试DNS解析
            _logger.LogInformation("测试DNS解析...");
            var dnsTask = System.Net.Dns.GetHostEntryAsync("open.feishu.cn");
            var hostEntry = dnsTask.Result;

            result.Add("DNS解析", new
            {
                主机名 = hostEntry.HostName,
                IP地址列表 = hostEntry.AddressList.Select(ip => ip.ToString()).ToList()
            });

            // 测试SSL连接
            _logger.LogInformation("测试SSL连接...");
            var client = new System.Net.Sockets.TcpClient();
            var connectTask = client.ConnectAsync("open.feishu.cn", 443);
            connectTask.Wait(TimeSpan.FromSeconds(5));

            result.Add("TCP连接", new
            {
                成功 = client.Connected,
                端口 = 443,
                本地端点 = client.Client.LocalEndPoint?.ToString(),
                远程端点 = client.Client.RemoteEndPoint?.ToString()
            });

            client.Close();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "网络环境测试失败");
            result.Add("错误", new
            {
                Type = ex.GetType().Name,
                Message = ex.Message
            });
        }

        return Ok(result);
    }
}
