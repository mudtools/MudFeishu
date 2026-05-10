// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.AspNetCore.Mvc;

namespace Mud.Feishu.Test.Controllers;

/// <summary>
/// 日志测试控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LogTestController : ControllerBase
{
    private readonly ILogger<LogTestController> _logger;

    public LogTestController(ILogger<LogTestController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// 测试日志输出
    /// </summary>
    [HttpGet]
    public IActionResult TestLogs()
    {
        _logger.LogDebug("这是一个Debug级别的日志");
        _logger.LogInformation("这是一个Information级别的日志");
        _logger.LogWarning("这是一个Warning级别的日志");
        _logger.LogError("这是一个Error级别的日志");

        return Ok(new
        {
            Message = "日志测试完成",
            Timestamp = DateTime.Now
        });
    }
}
