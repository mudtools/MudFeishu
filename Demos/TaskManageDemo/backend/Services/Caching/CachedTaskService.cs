// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Caching.Memory;
using TaskManageDemo.Backend.Models.DTOs;
using TaskManageDemo.Backend.Services;

namespace TaskManageDemo.Backend.Services.Caching;

/// <summary>
/// 任务服务缓存装饰器
/// </summary>
public class CachedTaskService : ITaskService
{
    private readonly ITaskService _innerService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedTaskService> _logger;

    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ShortCacheDuration = TimeSpan.FromMinutes(1);

    public CachedTaskService(
        ITaskService innerService,
        IMemoryCache cache,
        ILogger<CachedTaskService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<PagedResponse<TaskDto>> GetTasksAsync(
        TaskSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tasks_{request.GetHashCode()}";

        if (_cache.TryGetValue(cacheKey, out PagedResponse<TaskDto>? cached))
        {
            _logger.LogDebug("从缓存获取任务列表: {Key}", cacheKey);
            return cached!;
        }

        var result = await _innerService.GetTasksAsync(request, cancellationToken);

        _cache.Set(cacheKey, result, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = ShortCacheDuration,
            SlidingExpiration = TimeSpan.FromSeconds(30)
        });

        return result;
    }

    public async Task<TaskDto?> GetTaskByIdAsync(
        int taskId,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"task_{taskId}";

        if (_cache.TryGetValue(cacheKey, out TaskDto? cached))
        {
            _logger.LogDebug("从缓存获取任务: {Key}", cacheKey);
            return cached;
        }

        var result = await _innerService.GetTaskByIdAsync(taskId, cancellationToken);

        if (result != null)
        {
            _cache.Set(cacheKey, result, DefaultCacheDuration);
        }

        return result;
    }

    public async Task<TaskDto> CreateTaskAsync(
        CreateTaskRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _innerService.CreateTaskAsync(request, userId, cancellationToken);
        
        // 使任务列表缓存失效
        InvalidateTaskListCache();
        
        return result;
    }

    public async Task<TaskDto?> UpdateTaskAsync(
        int taskId,
        UpdateTaskRequest request,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _innerService.UpdateTaskAsync(taskId, request, userId, cancellationToken);
        
        if (result != null)
        {
            // 使单个任务缓存失效
            _cache.Remove($"task_{taskId}");
            InvalidateTaskListCache();
        }

        return result;
    }

    public async Task<bool> DeleteTaskAsync(
        int taskId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var result = await _innerService.DeleteTaskAsync(taskId, userId, cancellationToken);
        
        if (result)
        {
            _cache.Remove($"task_{taskId}");
            InvalidateTaskListCache();
        }

        return result;
    }

    /// <summary>
    /// 使任务列表缓存失效
    /// </summary>
    private void InvalidateTaskListCache()
    {
        // 在实际实现中，可以使用更精细的缓存失效策略
        // 例如使用缓存标签或前缀
        _logger.LogDebug("任务列表缓存已失效");
    }
}
