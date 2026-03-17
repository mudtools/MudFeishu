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

[Route("api/wiki/nodes")]
[Authorize]
public class WikiNodeController : BaseController
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
            return PagedSuccess(result.Items, result.HasMore, result.PageToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取节点树失败: {SpaceId}", spaceId);
            return ServerError("获取节点树失败", ex);
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
                return NotFoundResult("节点不存在");
            }
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取节点信息失败: {Token}", token);
            return ServerError("获取节点信息失败", ex);
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
                return BadRequestResult("创建节点失败");
            }

            _logger.LogInformation("创建节点成功: {NodeToken}", result.NodeToken);
            return Success(result, "创建节点成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建节点失败: {SpaceId}", spaceId);
            return ServerError("创建节点失败", ex);
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
                return BadRequestResult("更新节点标题失败");
            }
            return Success("更新成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "更新节点标题失败: {NodeToken}", nodeToken);
            return ServerError("更新节点标题失败", ex);
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
                return BadRequestResult("移动节点失败");
            }
            return Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "移动节点失败: {NodeToken}", nodeToken);
            return ServerError("移动节点失败", ex);
        }
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request)
    {
        try
        {
            var result = await _wikiService.SearchAsync(request.Query, request.SpaceId, request.PageSize);
            return PagedSuccess(result.Items, result.HasMore, result.PageToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "搜索失败: {Query}", request.Query);
            return ServerError("搜索失败", ex);
        }
    }

    [HttpGet("favorites")]
    public async Task<IActionResult> GetFavorites()
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var openId = GetRequiredOpenId();
            var user = await _userService.GetUserByOpenIdAsync(openId);
            if (user == null)
            {
                return UnauthorizedResult();
            }

            _logger.LogInformation("开始获取收藏列表，用户: {UserId}", user.Id);
            var favorites = await _wikiService.GetFavoritesAsync(user.Id);
            stopwatch.Stop();
            _logger.LogInformation("获取收藏列表成功，用户: {UserId}，数量: {Count}，耗时: {ElapsedMs}ms", 
                user.Id, favorites.Count, stopwatch.ElapsedMilliseconds);
            return Success(favorites);
        }
        catch (UnauthorizedAccessException)
        {
            return UnauthorizedResult();
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "获取收藏列表失败，耗时: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
            return ServerError("获取收藏列表失败", ex);
        }
    }

    [HttpPost("favorites")]
    public async Task<IActionResult> AddFavorite([FromBody] AddFavoriteRequest request)
    {
        try
        {
            var openId = GetRequiredOpenId();
            var user = await _userService.GetUserByOpenIdAsync(openId);
            if (user == null)
            {
                return UnauthorizedResult();
            }

            await _wikiService.AddFavoriteAsync(
                user.Id,
                request.SpaceId,
                request.NodeToken,
                request.Title,
                request.ObjToken,
                request.ObjType
            );

            return Success("收藏成功");
        }
        catch (UnauthorizedAccessException)
        {
            return UnauthorizedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "添加收藏失败");
            return ServerError("添加收藏失败", ex);
        }
    }

    [HttpDelete("favorites/{nodeToken}")]
    public async Task<IActionResult> RemoveFavorite(string nodeToken)
    {
        try
        {
            var openId = GetRequiredOpenId();
            var user = await _userService.GetUserByOpenIdAsync(openId);
            if (user == null)
            {
                return UnauthorizedResult();
            }

            await _wikiService.RemoveFavoriteAsync(user.Id, nodeToken);

            return Success("取消收藏成功");
        }
        catch (UnauthorizedAccessException)
        {
            return UnauthorizedResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "取消收藏失败");
            return ServerError("取消收藏失败", ex);
        }
    }
}
