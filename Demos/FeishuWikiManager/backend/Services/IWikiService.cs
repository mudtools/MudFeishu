using FeishuWikiManager.Models;
using Mud.Feishu.DataModels.Wiki;

namespace FeishuWikiManager.Services;

public interface IWikiService
{
    Task<PagedResponse<SpaceViewModel>> GetSpacesAsync(int pageSize = 20, string? pageToken = null);
    Task<SpaceViewModel?> GetSpaceInfoAsync(string spaceId);
    Task<SpaceViewModel?> CreateSpaceAsync(string name, string? description = null);
    Task<PagedResponse<NodeTreeViewModel>> GetNodeTreeAsync(string spaceId, string? parentNodeToken = null, int pageSize = 50, string? pageToken = null);
    Task<NodeTreeViewModel?> GetNodeInfoAsync(string nodeToken);
    Task<NodeTreeViewModel?> CreateNodeAsync(string spaceId, CreateDocumentRequest request);
    Task<bool> UpdateNodeTitleAsync(string spaceId, string nodeToken, string title);
    Task<NodeTreeViewModel?> MoveNodeAsync(string spaceId, string nodeToken, string? targetParentToken = null);
    Task<PagedResponse<WikiSearchResult>> SearchAsync(string query, string? spaceId = null, int pageSize = 20, string? pageToken = null);
    Task<List<FavoriteNode>> GetFavoritesAsync(string userId);
    Task AddFavoriteAsync(string userId, string spaceId, string nodeToken, string title, string? objToken = null, string? objType = null);
    Task RemoveFavoriteAsync(string userId, string nodeToken);
}
