// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuWikiManager.Data;
using FeishuWikiManager.Models;
using Microsoft.EntityFrameworkCore;
using Mud.Feishu;
using Mud.Feishu.DataModels.Wiki;

namespace FeishuWikiManager.Services;

public class WikiService : IWikiService
{
    private readonly IFeishuUserV2Wiki _wikiApi;
    private readonly IFeishuUserV2WikiNodes _nodeApi;
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WikiService> _logger;

    public WikiService(
        IFeishuUserV2Wiki wikiApi,
        IFeishuUserV2WikiNodes nodeApi,
        AppDbContext dbContext,
        ILogger<WikiService> logger)
    {
        _wikiApi = wikiApi;
        _nodeApi = nodeApi;
        _dbContext = dbContext;
        _logger = logger;
    }


    public async Task<PagedResponse<SpaceViewModel>> GetSpacesAsync(int pageSize = 20, string? pageToken = null)
    {
        var result = await _wikiApi.GetSpacesPageListAsync(pageSize, pageToken);

        if (result?.Data?.Items == null)
        {
            return new PagedResponse<SpaceViewModel>();
        }

        var spaces = result.Data.Items.Select(s => new SpaceViewModel
        {
            SpaceId = s.SpaceId ?? string.Empty,
            Name = s.Name ?? string.Empty,
            Description = s.Description,
            SpaceType = s.SpaceType ?? string.Empty,
            Visibility = s.Visibility ?? string.Empty
        }).ToList();

        return new PagedResponse<SpaceViewModel>
        {
            Items = spaces,
            HasMore = result.Data.HasMore,
            PageToken = result.Data.PageToken
        };
    }

    public async Task<SpaceViewModel?> GetSpaceInfoAsync(string spaceId)
    {
        var result = await _wikiApi.GetSpaceInfoAsync(spaceId, "zh");

        if (result?.Data?.Space == null)
        {
            return null;
        }

        var space = result.Data.Space;
        return new SpaceViewModel
        {
            SpaceId = space.SpaceId ?? string.Empty,
            Name = space.Name ?? string.Empty,
            Description = space.Description,
            SpaceType = space.SpaceType ?? string.Empty,
            Visibility = space.Visibility ?? string.Empty
        };
    }

    public async Task<SpaceViewModel?> CreateSpaceAsync(string name, string? description = null)
    {
        var request = new CreateSpaceRequest
        {
            Name = name,
            Description = description
        };

        var result = await _wikiApi.CreateSpaceAsync(request);

        if (result?.Data?.Space == null)
        {
            _logger.LogWarning("创建知识空间失败: {Msg}", result?.Msg);
            return null;
        }

        var space = result.Data.Space;
        return new SpaceViewModel
        {
            SpaceId = space.SpaceId ?? string.Empty,
            Name = space.Name ?? string.Empty,
            Description = space.Description,
            SpaceType = space.SpaceType ?? string.Empty,
            Visibility = space.Visibility ?? string.Empty
        };
    }

    public async Task<PagedResponse<NodeTreeViewModel>> GetNodeTreeAsync(
        string spaceId,
        string? parentNodeToken = null,
        int pageSize = 50,
        string? pageToken = null)
    {
        var result = await _nodeApi.GetSpaceNodesPageListAsync(spaceId, parentNodeToken, pageSize, pageToken);

        if (result?.Data?.Items == null)
        {
            return new PagedResponse<NodeTreeViewModel>();
        }

        var nodes = result.Data.Items.Select(n => new NodeTreeViewModel
        {
            NodeToken = n.NodeToken ?? string.Empty,
            ObjToken = n.ObjToken ?? string.Empty,
            ObjType = n.ObjType ?? string.Empty,
            Title = n.Title ?? string.Empty,
            ParentNodeToken = n.ParentNodeToken,
            HasChildren = n.HasChild ?? false,
            Creator = n.Creator,
            CreateTime = !string.IsNullOrEmpty(n.ObjCreateTime) ? DateTime.TryParse(n.ObjCreateTime, out var ct) ? ct : null : null,
            EditTime = !string.IsNullOrEmpty(n.ObjEditTime) ? DateTime.TryParse(n.ObjEditTime, out var et) ? et : null : null
        }).ToList();

        return new PagedResponse<NodeTreeViewModel>
        {
            Items = nodes,
            HasMore = result.Data.HasMore,
            PageToken = result.Data.PageToken
        };
    }

    public async Task<NodeTreeViewModel?> GetNodeInfoAsync(string nodeToken)
    {
        var result = await _nodeApi.GetNodeSpaceInfoAsync(nodeToken, "wiki");

        if (result?.Data?.Node == null)
        {
            return null;
        }

        var node = result.Data.Node;
        return new NodeTreeViewModel
        {
            NodeToken = node.NodeToken ?? string.Empty,
            ObjToken = node.ObjToken ?? string.Empty,
            ObjType = node.ObjType ?? string.Empty,
            Title = node.Title ?? string.Empty,
            ParentNodeToken = node.ParentNodeToken,
            HasChildren = node.HasChild ?? false
        };
    }

    public async Task<NodeTreeViewModel?> CreateNodeAsync(string spaceId, CreateDocumentRequest request)
    {
        var nodeRequest = new CreateSpaceNodeRequest
        {
            ObjType = request.ObjType,
            Title = request.Title,
            ParentNodeToken = request.ParentNodeToken,
            NodeType = "origin"
        };

        var result = await _nodeApi.CreateSpaceNodeAsync(spaceId, nodeRequest);

        if (result?.Data?.Node == null)
        {
            _logger.LogWarning("创建节点失败: {Msg}", result?.Msg);
            return null;
        }

        var node = result.Data.Node;
        return new NodeTreeViewModel
        {
            NodeToken = node.NodeToken ?? string.Empty,
            ObjToken = node.ObjToken ?? string.Empty,
            ObjType = node.ObjType ?? string.Empty,
            Title = node.Title ?? string.Empty,
            ParentNodeToken = node.ParentNodeToken,
            HasChildren = node.HasChild ?? false
        };
    }

    public async Task<bool> UpdateNodeTitleAsync(string spaceId, string nodeToken, string title)
    {
        var request = new UpdateTitleSpaceNodeRequest { Title = title };
        var result = await _nodeApi.UpdateTitleSpaceNodeAsync(spaceId, nodeToken, request);
        return result?.Code == 0;
    }

    public async Task<NodeTreeViewModel?> MoveNodeAsync(string spaceId, string nodeToken, string? targetParentToken = null)
    {
        var request = new MoveSpaceNodeRequest
        {
            TargetParentToken = targetParentToken
        };

        var result = await _nodeApi.MoveSpaceNodeAsync(spaceId, nodeToken, request);

        if (result?.Data?.Node == null)
        {
            return null;
        }

        var node = result.Data.Node;
        return new NodeTreeViewModel
        {
            NodeToken = node.NodeToken ?? string.Empty,
            ObjToken = node.ObjToken ?? string.Empty,
            ObjType = node.ObjType ?? string.Empty,
            Title = node.Title ?? string.Empty,
            ParentNodeToken = node.ParentNodeToken,
            HasChildren = node.HasChild ?? false
        };
    }

    public async Task<PagedResponse<WikiSearchResult>> SearchAsync(
        string query,
        string? spaceId = null,
        int pageSize = 20,
        string? pageToken = null)
    {
        var request = new WikiSearchRequest
        {
            Query = query,
            SpaceId = spaceId
        };

        var result = await _nodeApi.SearchPageListAsync(request, pageSize, pageToken);

        if (result?.Data?.Items == null)
        {
            return new PagedResponse<WikiSearchResult>();
        }

        return new PagedResponse<WikiSearchResult>
        {
            Items = result.Data.Items.ToList(),
            HasMore = result.Data.HasMore,
            PageToken = result.Data.PageToken
        };
    }

    public async Task<List<FavoriteNode>> GetFavoritesAsync(string userId)
    {
        return await _dbContext.FavoriteNodes
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task AddFavoriteAsync(
        string userId,
        string spaceId,
        string nodeToken,
        string title,
        string? objToken = null,
        string? objType = null)
    {
        var existing = await _dbContext.FavoriteNodes
            .FirstOrDefaultAsync(f => f.UserId == userId && f.NodeToken == nodeToken);

        if (existing != null)
        {
            return;
        }

        var favorite = new FavoriteNode
        {
            UserId = userId,
            SpaceId = spaceId,
            NodeToken = nodeToken,
            ObjToken = objToken,
            Title = title,
            ObjType = objType
        };

        _dbContext.FavoriteNodes.Add(favorite);
        await _dbContext.SaveChangesAsync();
    }

    public async Task RemoveFavoriteAsync(string userId, string nodeToken)
    {
        var favorite = await _dbContext.FavoriteNodes
            .FirstOrDefaultAsync(f => f.UserId == userId && f.NodeToken == nodeToken);

        if (favorite != null)
        {
            _dbContext.FavoriteNodes.Remove(favorite);
            await _dbContext.SaveChangesAsync();
        }
    }
}
