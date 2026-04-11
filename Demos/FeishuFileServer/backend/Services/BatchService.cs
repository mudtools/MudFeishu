using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace FeishuFileServer.Services;

/// <summary>
/// 批量操作服务实现
/// 提供批量删除、移动、复制、下载功能
/// </summary>
public class BatchService : IBatchService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFileService _fileService;
    private readonly ILogger<BatchService> _logger;

    /// <summary>
    /// 初始化批量操作服务实例
    /// </summary>
    public BatchService(
        FeishuFileDbContext dbContext,
        IFileService fileService,
        ILogger<BatchService> logger)
    {
        _dbContext = dbContext;
        _fileService = fileService;
        _logger = logger;
    }

    /// <summary>
    /// 批量删除文件和文件夹
    /// </summary>
    public async Task<BatchOperationResponse> BatchDeleteAsync(BatchDeleteRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var response = new BatchOperationResponse();
        var deletedTime = DateTime.UtcNow;

        foreach (var fileToken in request.FileTokens)
        {
            try
            {
                var file = await _dbContext.FileRecords
                    .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

                if (file != null && file.UserId == userId)
                {
                    file.IsDeleted = true;
                    file.DeletedTime = deletedTime;
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = fileToken,
                        Name = file?.FileName ?? fileToken,
                        Message = file == null ? "文件不存在" : "无权限删除"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除文件失败: {FileToken}", fileToken);
                response.Errors.Add(new BatchOperationError
                {
                    Token = fileToken,
                    Name = fileToken,
                    Message = ex.Message
                });
                response.FailedCount++;
            }
        }

        foreach (var folderToken in request.FolderTokens)
        {
            try
            {
                var folder = await _dbContext.FolderRecords
                    .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

                if (folder != null && folder.UserId == userId)
                {
                    folder.IsDeleted = true;
                    folder.DeletedTime = deletedTime;
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = folderToken,
                        Name = folder?.FolderName ?? folderToken,
                        Message = folder == null ? "文件夹不存在" : "无权限删除"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除文件夹失败: {FolderToken}", folderToken);
                response.Errors.Add(new BatchOperationError
                {
                    Token = folderToken,
                    Name = folderToken,
                    Message = ex.Message
                });
                response.FailedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        response.Success = response.SuccessCount > 0;

        _logger.LogInformation("批量删除完成: 成功 {SuccessCount}, 失败 {FailedCount}", response.SuccessCount, response.FailedCount);

        return response;
    }

    /// <summary>
    /// 批量移动文件和文件夹
    /// </summary>
    public async Task<BatchOperationResponse> BatchMoveAsync(BatchMoveRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var response = new BatchOperationResponse();

        foreach (var fileToken in request.FileTokens)
        {
            try
            {
                var file = await _dbContext.FileRecords
                    .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

                if (file != null && file.UserId == userId)
                {
                    file.FolderToken = request.TargetFolderToken;
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = fileToken,
                        Name = file?.FileName ?? fileToken,
                        Message = file == null ? "文件不存在" : "无权限移动"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量移动文件失败: {FileToken}", fileToken);
                response.Errors.Add(new BatchOperationError { Token = fileToken, Name = fileToken, Message = ex.Message });
                response.FailedCount++;
            }
        }

        foreach (var folderToken in request.FolderTokens)
        {
            try
            {
                var folder = await _dbContext.FolderRecords
                    .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

                if (folder != null && folder.UserId == userId)
                {
                    if (folderToken == request.TargetFolderToken)
                    {
                        response.Errors.Add(new BatchOperationError
                        {
                            Token = folderToken,
                            Name = folder.FolderName,
                            Message = "不能将文件夹移动到自身"
                        });
                        response.FailedCount++;
                    }
                    else
                    {
                        folder.ParentFolderToken = request.TargetFolderToken;
                        response.SuccessCount++;
                    }
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = folderToken,
                        Name = folder?.FolderName ?? folderToken,
                        Message = folder == null ? "文件夹不存在" : "无权限移动"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量移动文件夹失败: {FolderToken}", folderToken);
                response.Errors.Add(new BatchOperationError { Token = folderToken, Name = folderToken, Message = ex.Message });
                response.FailedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        response.Success = response.SuccessCount > 0;

        _logger.LogInformation("批量移动完成: 成功 {SuccessCount}, 失败 {FailedCount}", response.SuccessCount, response.FailedCount);

        return response;
    }

    /// <summary>
    /// 批量复制文件和文件夹
    /// </summary>
    public async Task<BatchOperationResponse> BatchCopyAsync(BatchCopyRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var response = new BatchOperationResponse();

        foreach (var fileToken in request.FileTokens)
        {
            try
            {
                var file = await _dbContext.FileRecords
                    .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

                if (file != null)
                {
                    var newFile = new FileRecord
                    {
                        FileToken = Guid.NewGuid().ToString("N"),
                        FolderToken = request.TargetFolderToken,
                        FileName = file.FileName,
                        FileSize = file.FileSize,
                        MimeType = file.MimeType,
                        FileMD5 = file.FileMD5,
                        UploadTime = DateTime.UtcNow,
                        UserId = userId,
                        VersionToken = file.VersionToken
                    };

                    _dbContext.FileRecords.Add(newFile);
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = fileToken,
                        Name = file?.FileName ?? fileToken,
                        Message = "文件不存在"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量复制文件失败: {FileToken}", fileToken);
                response.Errors.Add(new BatchOperationError { Token = fileToken, Name = fileToken, Message = ex.Message });
                response.FailedCount++;
            }
        }

        foreach (var folderToken in request.FolderTokens)
        {
            try
            {
                var folder = await _dbContext.FolderRecords
                    .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

                if (folder != null)
                {
                    var newFolder = new FolderRecord
                    {
                        FolderToken = Guid.NewGuid().ToString("N"),
                        FolderName = folder.FolderName,
                        ParentFolderToken = request.TargetFolderToken,
                        CreatedTime = DateTime.UtcNow,
                        UserId = userId
                    };

                    _dbContext.FolderRecords.Add(newFolder);
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = folderToken,
                        Name = folder?.FolderName ?? folderToken,
                        Message = "文件夹不存在"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量复制文件夹失败: {FolderToken}", folderToken);
                response.Errors.Add(new BatchOperationError { Token = folderToken, Name = folderToken, Message = ex.Message });
                response.FailedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        response.Success = response.SuccessCount > 0;

        _logger.LogInformation("批量复制完成: 成功 {SuccessCount}, 失败 {FailedCount}", response.SuccessCount, response.FailedCount);

        return response;
    }

    /// <summary>
    /// 批量恢复文件和文件夹
    /// </summary>
    public async Task<BatchOperationResponse> BatchRestoreAsync(BatchRestoreRequest request, int userId, CancellationToken cancellationToken = default)
    {
        var response = new BatchOperationResponse();

        foreach (var fileToken in request.FileTokens)
        {
            try
            {
                var file = await _dbContext.FileRecords
                    .FirstOrDefaultAsync(f => f.FileToken == fileToken && f.IsDeleted, cancellationToken);

                if (file != null && file.UserId == userId)
                {
                    file.IsDeleted = false;
                    file.DeletedTime = null;
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = fileToken,
                        Name = file?.FileName ?? fileToken,
                        Message = file == null ? "文件不在回收站中" : "无权限恢复"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量恢复文件失败: {FileToken}", fileToken);
                response.Errors.Add(new BatchOperationError { Token = fileToken, Name = fileToken, Message = ex.Message });
                response.FailedCount++;
            }
        }

        foreach (var folderToken in request.FolderTokens)
        {
            try
            {
                var folder = await _dbContext.FolderRecords
                    .FirstOrDefaultAsync(f => f.FolderToken == folderToken && f.IsDeleted, cancellationToken);

                if (folder != null && folder.UserId == userId)
                {
                    folder.IsDeleted = false;
                    folder.DeletedTime = null;
                    response.SuccessCount++;
                }
                else
                {
                    response.Errors.Add(new BatchOperationError
                    {
                        Token = folderToken,
                        Name = folder?.FolderName ?? folderToken,
                        Message = folder == null ? "文件夹不在回收站中" : "无权限恢复"
                    });
                    response.FailedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量恢复文件夹失败: {FolderToken}", folderToken);
                response.Errors.Add(new BatchOperationError { Token = folderToken, Name = folderToken, Message = ex.Message });
                response.FailedCount++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        response.Success = response.SuccessCount > 0;

        _logger.LogInformation("批量恢复完成: 成功 {SuccessCount}, 失败 {FailedCount}", response.SuccessCount, response.FailedCount);

        return response;
    }

    /// <summary>
    /// 批量下载文件
    /// </summary>
    public async Task<(byte[] Content, string FileName)> BatchDownloadAsync(BatchDownloadRequest request, int userId, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
        {
            foreach (var fileToken in request.FileTokens)
            {
                try
                {
                    var file = await _dbContext.FileRecords
                        .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

                    if (file != null && file.UserId == userId)
                    {
                        var fileContent = await _fileService.DownloadFileAsync(fileToken, null, cancellationToken);

                        var entry = zipArchive.CreateEntry(file.FileName, CompressionLevel.Optimal);
                        using var entryStream = entry.Open();
                        await entryStream.WriteAsync(fileContent, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量下载文件失败: {FileToken}", fileToken);
                }
            }

            foreach (var folderToken in request.FolderTokens)
            {
                try
                {
                    await AddFolderToZipAsync(zipArchive, folderToken, userId, "", cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "批量下载文件夹失败: {FolderToken}", folderToken);
                }
            }
        }

        var zipFileName = $"download_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";
        _logger.LogInformation("批量下载完成: {FileName}", zipFileName);

        return (memoryStream.ToArray(), zipFileName);
    }

    /// <summary>
    /// 递归添加文件夹到ZIP
    /// </summary>
    private async Task AddFolderToZipAsync(ZipArchive zipArchive, string folderToken, int userId, string basePath, CancellationToken cancellationToken)
    {
        var folder = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == folderToken && !f.IsDeleted, cancellationToken);

        if (folder == null || folder.UserId != userId) return;

        var folderPath = string.IsNullOrEmpty(basePath) ? folder.FolderName : $"{basePath}/{folder.FolderName}";

        var files = await _dbContext.FileRecords
            .Where(f => f.FolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var file in files)
        {
            try
            {
                var fileContent = await _fileService.DownloadFileAsync(file.FileToken, null, cancellationToken);
                var entry = zipArchive.CreateEntry($"{folderPath}/{file.FileName}", CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                await entryStream.WriteAsync(fileContent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载文件失败: {FileToken}", file.FileToken);
            }
        }

        var subFolders = await _dbContext.FolderRecords
            .Where(f => f.ParentFolderToken == folderToken && !f.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var subFolder in subFolders)
        {
            await AddFolderToZipAsync(zipArchive, subFolder.FolderToken, userId, folderPath, cancellationToken);
        }
    }
}
