// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Models;
using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuWikiManager.Controllers;

[Route("api/wiki")]
[Authorize]
public class WikiController : BaseController
{
    private readonly IWikiService _wikiService;
    private readonly ILogger<WikiController> _logger;

    public WikiController(
        IWikiService wikiService,
        ILogger<WikiController> logger)
    {
        _wikiService = wikiService;
        _logger = logger;
    }

    [HttpGet("spaces")]
    public async Task<IActionResult> GetSpaces(
        [FromQuery] int pageSize = 20,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _wikiService.GetSpacesAsync(pageSize, pageToken);
            return PagedSuccess(result.Items, result.HasMore, result.PageToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取知识空间列表失败");
            return ServerError("获取知识空间列表失败", ex);
        }
    }

    [HttpGet("spaces/{spaceId}")]
    public async Task<IActionResult> GetSpaceInfo(string spaceId)
    {
        try
        {
            var result = await _wikiService.GetSpaceInfoAsync(spaceId);
            if (result == null)
            {
                return NotFoundResult("知识空间不存在");
            }
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取知识空间详情失败: {SpaceId}", spaceId);
            return ServerError("获取知识空间详情失败", ex);
        }
    }

    [HttpPost("spaces")]
    public async Task<IActionResult> CreateSpace([FromBody] CreateSpaceRequestModel request)
    {
        try
        {
            var result = await _wikiService.CreateSpaceAsync(request.Title, request.Description);
            if (result == null)
            {
                return BadRequestResult("创建知识空间失败");
            }

            _logger.LogInformation("创建知识空间成功: {SpaceId}", result.SpaceId);
            return Success(result, "创建知识空间成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建知识空间失败");
            return ServerError("创建知识空间失败", ex);
        }
    }
}
