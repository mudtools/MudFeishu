// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;
using Mud.Feishu;
using Mud.Feishu.DataModels.Drive.Folder;

namespace FeishuFileServer.Services;

/// <summary>
/// 文件夹服务实现
/// 提供文件夹的创建、更新、删除、查询和内容管理功能的具体实现
/// </summary>
public class FolderService : IFolderService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuTenantV1DriveFolder _driveFolder;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly ILogger<FolderService> _logger;

    /// <summary>
    /// 初始化文件夹服务实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="driveFolder">飞书云盘文件夹API</param>
    /// <param name="feishuDriveService">飞书云盘服务</param>
    /// <param name="logger">日志记录器</param>
    public FolderService(
        FeishuFileDbContext dbContext,
        IFeishuTenantV1DriveFolder driveFolder,
        IFeishuDriveService feishuDriveService,
        ILogger<FolderService> logger)
    {
        _dbContext = dbContext;
        _driveFolder = driveFolder;
        _feishuDriveService = feishuDriveService;
        _logger = logger;
    }

    /// <summary>
    /// 创建文件夹
    /// 在飞书云盘创建文件夹，并在本地数据库创建记录
    /// </summary>
    /// <param name="request">创建请求</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹响应</returns>
    public async Task<FolderResponse> CreateFolderAsync(FolderCreateRequest request, int? userId = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating folder {FolderName} in parent {ParentFolderToken} by user {UserId}",
            request.Name, request.ParentFolderToken, userId);

        var createRequest = new CreateFolderRequest
        {
            Name = request.Name,
            FolderToken = request.ParentFolderToken ?? string.Empty
        };

        var result = await _driveFolder.CreateFolderAsync(createRequest, cancellationToken);

        if (result?.Data == null)
        {
            throw new Exception($"Failed to create folder: {result?.Msg}");
        }

        var folderRecord = new FolderRecord
        {
            FolderToken = result.Data.Token,
            FolderName = request.Name,
            ParentFolderToken = request.ParentFolderToken,
            CreatedTime = DateTime.UtcNow,
            UserId = userId
        };

        _dbContext.FolderRecords.Add(folderRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Folder created successfully: {FolderToken}", folderRecord.FolderToken);

        return MapToFolderResponse(folderRecord);
    }

    /// <summary>
    /// 更新文件夹
    /// 更新文件夹名称
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="request">更新请求</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹响应</returns>
    public async Task<FolderResponse> UpdateFolderAsync(string folderToken, FolderUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        if (folderRecord == null)
        {
            throw new KeyNotFoundException($"Folder with token {folderToken} not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            folderRecord.FolderName = request.Name;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Folder updated: {FolderToken}", folderToken);

        return MapToFolderResponse(folderRecord);
    }

    /// <summary>
    /// 删除文件夹
    /// 将文件夹移到回收站（软删除），同时标记子文件夹和文件为已删除
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        if (folderRecord == null)
        {
            throw new KeyNotFoundException($"Folder with token {folderToken} not found");
        }

        var subFolders = await _dbContext.FolderRecords
            .Where(f => f.ParentFolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        var deletedTime = DateTime.UtcNow;
        foreach (var subFolder in subFolders)
        {
            subFolder.IsDeleted = true;
            subFolder.DeletedTime = deletedTime;
        }

        var files = await _dbContext.FileRecords
            .Where(f => f.FolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            file.IsDeleted = true;
            file.DeletedTime = deletedTime;
        }

        folderRecord.IsDeleted = true;
        folderRecord.DeletedTime = deletedTime;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Folder moved to recycle bin: {FolderToken}", folderToken);
    }

    /// <summary>
    /// 获取文件夹信息
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹信息</returns>
    public async Task<FolderResponse?> GetFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var folderRecord = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        return folderRecord == null ? null : MapToFolderResponse(folderRecord);
    }

    /// <summary>
    /// 获取文件夹列表
    /// 支持按父文件夹和用户筛选
    /// 当 parentFolderToken 为 null 时返回所有文件夹，以便前端构建树形结构
    /// </summary>
    /// <param name="parentFolderToken">父文件夹令牌</param>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹列表响应</returns>
    public async Task<FolderListResponse> GetFoldersAsync(string? parentFolderToken = null, int? userId = null, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FolderRecords.Where(f => !f.IsDeleted);

        if (!string.IsNullOrEmpty(parentFolderToken))
        {
            // 只返回指定父文件夹下的直接子文件夹
            query = query.Where(f => f.ParentFolderToken == parentFolderToken);
        }
        // 当 parentFolderToken 为 null 时，返回所有文件夹（不过滤 ParentFolderToken）
        // 这样前端 buildTree 函数可以构建完整的树形结构

        if (userId.HasValue)
        {
            query = query.Where(f => f.UserId == userId);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var folders = await query
            .OrderByDescending(f => f.CreatedTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new FolderListResponse
        {
            Folders = folders.Select(MapToFolderResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 获取文件夹内容
    /// 获取文件夹内的子文件夹和文件列表
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件夹内容响应</returns>
    public async Task<FolderContentsResponse> GetFolderContentsAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        var folders = await _dbContext.FolderRecords
            .Where(f => f.ParentFolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        var files = await _dbContext.FileRecords
            .Where(f => f.FolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        return new FolderContentsResponse
        {
            Folders = folders.Select(MapToFolderResponse).ToList(),
            Files = files.Select(MapToFileInfoResponse).ToList()
        };
    }

    /// <summary>
    /// 将文件夹记录实体映射为文件夹响应DTO
    /// </summary>
    /// <param name="record">文件夹记录实体</param>
    /// <returns>文件夹响应DTO</returns>
    private static FolderResponse MapToFolderResponse(FolderRecord record)
    {
        return new FolderResponse
        {
            Id = record.Id,
            FolderToken = record.FolderToken,
            FolderName = record.FolderName,
            ParentFolderToken = record.ParentFolderToken,
            CreatedTime = record.CreatedTime,
            IsDeleted = record.IsDeleted
        };
    }

    /// <summary>
    /// 将文件记录实体映射为文件信息响应DTO
    /// </summary>
    /// <param name="record">文件记录实体</param>
    /// <returns>文件信息响应DTO</returns>
    private static FileInfoResponse MapToFileInfoResponse(FileRecord record)
    {
        return new FileInfoResponse
        {
            Id = record.Id,
            FileToken = record.FileToken,
            FolderToken = record.FolderToken,
            VersionToken = record.VersionToken,
            FileName = record.FileName,
            FileSize = record.FileSize,
            MimeType = record.MimeType,
            FileMD5 = record.FileMD5,
            UploadTime = record.UploadTime,
            IsDeleted = record.IsDeleted
        };
    }
}
