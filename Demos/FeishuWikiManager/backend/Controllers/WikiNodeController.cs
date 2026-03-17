using FeishuWikiManager.Models;
using FeishuWikiManager.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FeishuWikiManager.Controllers;

[ApiController]
[Route("api/wiki/nodes")]
[Authorize]
public class WikiNodeController : ControllerBase
{
    private readonly IWikiService _wikiService;
    private readonly IUserService _userService;
    private readonly ILogger<WikiNodeController> _logger;

    public WikiNodeController(
        IWikiService wikiService,
        IUserService userService,
        ILogger<WikiNodeController> logger)
    {
        _wikiService = wikiService;
        _userService = userService;
        _logger = logger;
    }

    [HttpGet("tree/{spaceId}")]
    public async Task<IActionResult> GetNodeTree(
        string spaceId,
        [FromQuery] string? parentNodeToken = null,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? pageToken = null)
    {
        try
        {
            var result = await _wikiService.GetNodeTreeAsync(spaceId, parentNodeToken, pageSize, pageToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取节点树失败: {SpaceId}", spaceId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"获取节点树失败: {ex.Message}"
            });
        }
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetNodeInfo(string token)
    {
        try
        {
            var result = await _wikiService.GetNodeInfoAsync(token);
            if (result == null)
            {
                return NotFound(new ApiResponse<NodeTreeViewModel>
                {
                    Success = false,
                    Message = "节点不存在"
                });
            }
            return Ok(new ApiResponse<NodeTreeViewModel>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取节点信息失败: {Token}", token);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"获取节点信息失败: {ex.Message}"
            });
        }
    }

    [HttpPost("{spaceId}")]
    public async Task<IActionResult> CreateNode(string spaceId, [FromBody] CreateDocumentRequest request)
    {
        try
        {
            request.SpaceId = spaceId;
            var result = await _wikiService.CreateNodeAsync(spaceId, request);
            if (result == null)
            {
                return BadRequest(new ApiResponse<NodeTreeViewModel>
                {
                    Success = false,
                    Message = "创建节点失败"
                });
            }

            _logger.LogInformation("创建节点成功: {NodeToken}", result.NodeToken);
            return Ok(new ApiResponse<NodeTreeViewModel>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建节点失败: {SpaceId}", spaceId);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"创建节点失败: {ex.Message}"
            });
        }
    }

    [HttpPut("{spaceId}/{nodeToken}/title")]
    public async Task<IActionResult> UpdateNodeTitle(
        string spaceId,
        string nodeToken,
        [FromBody] UpdateTitleRequest request)
    {
        try
        {
            var success = await _wikiService.UpdateNodeTitleAsync(spaceId, nodeToken, request.Title);
            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "更新节点标题失败"
                });
            }
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "更新成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新节点标题失败: {NodeToken}", nodeToken);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"更新节点标题失败: {ex.Message}"
            });
        }
    }

    [HttpPost("{spaceId}/{nodeToken}/move")]
    public async Task<IActionResult> MoveNode(
        string spaceId,
        string nodeToken,
        [FromBody] MoveNodeRequest request)
    {
        try
        {
            var result = await _wikiService.MoveNodeAsync(spaceId, nodeToken, request.TargetParentToken);
            if (result == null)
            {
                return BadRequest(new ApiResponse<NodeTreeViewModel>
                {
                    Success = false,
                    Message = "移动节点失败"
                });
            }
            return Ok(new ApiResponse<NodeTreeViewModel>
            {
                Success = true,
                Data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移动节点失败: {NodeToken}", nodeToken);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"移动节点失败: {ex.Message}"
            });
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        try
        {
            var result = await _wikiService.SearchAsync(request.Query, request.SpaceId, request.PageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索失败: {Query}", request.Query);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"搜索失败: {ex.Message}"
            });
        }
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (string.IsNullOrEmpty(openId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByOpenIdAsync(openId);
            if (user == null)
            {
                return Unauthorized();
            }

            var favorites = await _wikiService.GetFavoritesAsync(user.Id);
            return Ok(new ApiResponse<List<FavoriteNode>>
            {
                Success = true,
                Data = favorites
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取收藏列表失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"获取收藏列表失败: {ex.Message}"
            });
        }
    }

    [HttpPost("favorites")]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (string.IsNullOrEmpty(openId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByOpenIdAsync(openId);
            if (user == null)
            {
                return Unauthorized();
            }

            await _wikiService.AddFavoriteAsync(
                user.Id,
                request.SpaceId,
                request.NodeToken,
                request.Title,
                request.ObjToken,
                request.ObjType
            );

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "收藏成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加收藏失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"添加收藏失败: {ex.Message}"
            });
        }
    }

    [HttpDelete("favorites/{nodeToken}")]
    public async Task<IActionResult> RemoveFavorite(string nodeToken)
    {
        try
        {
            var openId = User.FindFirst("open_id")?.Value;
            if (string.IsNullOrEmpty(openId))
            {
                return Unauthorized();
            }

            var user = await _userService.GetUserByOpenIdAsync(openId);
            if (user == null)
            {
                return Unauthorized();
            }

            await _wikiService.RemoveFavoriteAsync(user.Id, nodeToken);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Message = "取消收藏成功"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消收藏失败");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                Message = $"取消收藏失败: {ex.Message}"
            });
        }
    }
}
