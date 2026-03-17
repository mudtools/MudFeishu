using FeishuWikiManager.Models;
using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeishuWikiManager.Controllers;

[ApiController]
[Route("api/wiki")]
[Authorize]
public class WikiController : ControllerBase
{
    private readonly IWikiService _wikiService;
    private readonly ILogger<WikiController> _logger;

    public WikiController(IWikiService wikiService, ILogger<WikiController> logger)
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取知识空间列表失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"获取知识空间列表失败: {ex.Message}"
            });
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
                return NotFound(new ApiResponse<SpaceViewModel>
                {
                    Success = false,
                    Message = "知识空间不存在"
                });
            }
            return Ok(new ApiResponse<SpaceViewModel>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取知识空间详情失败: {SpaceId}", spaceId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"获取知识空间详情失败: {ex.Message}"
            });
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
                return BadRequest(new ApiResponse<SpaceViewModel>
                {
                    Success = false,
                    Message = "创建知识空间失败"
                });
            }

            _logger.LogInformation("创建知识空间成功: {SpaceId}", result.SpaceId);
            return Ok(new ApiResponse<SpaceViewModel>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建知识空间失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"创建知识空间失败: {ex.Message}"
            });
        }
    }
}
