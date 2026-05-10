// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace FeishuFileServer.Services;

/// <summary>
/// 文件服务实现
/// 提供文件的上传、下载、删除、查询、移动和复制功能的具体实现
/// </summary>
public class FileService : IFileService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly IVersionService _versionService;
    private readonly FileUploadSettings _uploadSettings;
    private readonly ILogger<FileService> _logger;

    /// <summary>
    /// 初始化文件服务实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="feishuDriveService">飞书云盘服务</param>
    /// <param name="versionService">版本服务</param>
    /// <param name="uploadSettings">上传配置选项</param>
    /// <param name="logger">日志记录器</param>
    public FileService(
        FeishuFileDbContext dbContext,
        IFeishuDriveService feishuDriveService,
        IVersionService versionService,
        IOptions<FileUploadSettings> uploadSettings,
        ILogger<FileService> logger)
    {
        _dbContext = dbContext;
        _feishuDriveService = feishuDriveService;
        _versionService = versionService;
        _uploadSettings = uploadSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 上传文件
    /// 支持文件去重，相同MD5的文件直接返回已有记录
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <param name="folderToken">目标文件夹令牌</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>上传响应</returns>
    public async Task<FileUploadResponse> UploadFileAsync(IFormFile file, string? folderToken, int? userId = null, CancellationToken cancellationToken = default)
    {
        ValidateFile(file);

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream, cancellationToken);
        memoryStream.Position = 0;

        string? fileMD5 = null;
        if (_uploadSettings.EnableDeduplication)
        {
            fileMD5 = CalculateMD5(memoryStream);
            memoryStream.Position = 0;

            var existingFile = await _dbContext.FileRecords
                .FirstOrDefaultAsync(f => f.FileMD5 == fileMD5 && !f.IsDeleted, cancellationToken);

            if (existingFile != null)
            {
                _logger.LogInformation("File with MD5 {MD5} already exists, returning existing file", fileMD5);
                return new FileUploadResponse
                {
                    FileToken = existingFile.FileToken,
                    FileName = existingFile.FileName,
                    FileSize = existingFile.FileSize,
                    MimeType = existingFile.MimeType,
                    FileMD5 = existingFile.FileMD5,
                    UploadTime = existingFile.UploadTime
                };
            }
        }

        var fileRecord = await _feishuDriveService.UploadFileAsync(memoryStream, file.FileName, folderToken, cancellationToken);

        fileRecord.FileMD5 = fileMD5;
        fileRecord.FolderToken = folderToken;
        fileRecord.UserId = userId;

        _dbContext.FileRecords.Add(fileRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File uploaded successfully: {FileToken} by user {UserId}", fileRecord.FileToken, userId);

        return new FileUploadResponse
        {
            FileToken = fileRecord.FileToken,
            FileName = fileRecord.FileName,
            FileSize = fileRecord.FileSize,
            MimeType = fileRecord.MimeType,
            FileMD5 = fileRecord.FileMD5,
            UploadTime = fileRecord.UploadTime
        };
    }

    /// <summary>
    /// 下载文件
    /// 支持下载指定版本的文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌，可选</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
    public async Task<byte[]> DownloadFileAsync(string fileToken, string? versionToken = null, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        if (!string.IsNullOrEmpty(versionToken))
        {
            _logger.LogInformation("Downloading file {FileToken} version {VersionToken}", fileToken, versionToken);
            return await _versionService.DownloadVersionAsync(fileToken, versionToken, cancellationToken);
        }

        return await _feishuDriveService.DownloadFileAsync(fileToken, cancellationToken);
    }

    /// <summary>
    /// 删除文件
    /// 将文件移到回收站（软删除），同时删除云端文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        fileRecord.IsDeleted = true;
        fileRecord.DeletedTime = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File moved to recycle bin: {FileToken}", fileToken);
    }

    /// <summary>
    /// 获取文件信息
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件信息，不存在时返回null</returns>
    public async Task<FileInfoResponse?> GetFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            return null;
        }

        return MapToFileInfoResponse(fileRecord);
    }

    /// <summary>
    /// 获取文件列表
    /// 支持按文件夹、用户筛选和搜索
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="userId">用户ID</param>
    /// <param name="searchKeyword">搜索关键词</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件列表响应</returns>
    public async Task<FileListResponse> GetFilesAsync(string? folderToken = null, int? userId = null, string? searchKeyword = null, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.FileRecords.Where(f => !f.IsDeleted);

        if (!string.IsNullOrEmpty(folderToken))
        {
            query = query.Where(f => f.FolderToken == folderToken);
        }

        if (userId.HasValue)
        {
            query = query.Where(f => f.UserId == userId);
        }

        if (!string.IsNullOrWhiteSpace(searchKeyword))
        {
            query = query.Where(f => f.FileName.Contains(searchKeyword));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var files = await query
            .OrderByDescending(f => f.UploadTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new FileListResponse
        {
            Files = files.Select(MapToFileInfoResponse).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    /// <summary>
    /// 重命名文件
    /// 更新文件的显示名称
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="newName">新文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>更新后的文件信息</returns>
    public async Task<FileInfoResponse?> RenameFileAsync(string fileToken, string newName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException("文件名不能为空", nameof(newName));
        }

        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"文件 {fileToken} 不存在");
        }

        var oldName = fileRecord.FileName;
        fileRecord.FileName = newName;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File renamed from {OldName} to {NewName}: {FileToken}", oldName, newName, fileToken);

        return MapToFileInfoResponse(fileRecord);
    }

    /// <summary>
    /// 移动文件到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task MoveFileAsync(string fileToken, string destFolderToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        await _feishuDriveService.MoveFileAsync(fileToken, destFolderToken, cancellationToken);

        fileRecord.FolderToken = destFolderToken;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("File moved: {FileToken} to {DestFolderToken}", fileToken, destFolderToken);
    }

    /// <summary>
    /// 复制文件到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="newName">新文件名</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task CopyFileAsync(string fileToken, string destFolderToken, string? newName = null, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        await _feishuDriveService.CopyFileAsync(fileToken, destFolderToken, newName, cancellationToken);

        _logger.LogInformation("File copied: {FileToken} to {DestFolderToken}", fileToken, destFolderToken);
    }

    /// <summary>
    /// 验证上传文件
    /// 检查文件是否为空、类型是否允许、大小是否超限
    /// </summary>
    /// <param name="file">上传的文件</param>
    /// <exception cref="ArgumentException">验证失败时抛出</exception>
    private void ValidateFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("文件为空");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_uploadSettings.AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"文件类型 {extension} 不被允许");
        }

        var maxSizeBytes = _uploadSettings.MaxFileSizeMB * 1024 * 1024;
        if (file.Length > maxSizeBytes)
        {
            throw new ArgumentException($"文件大小超过最大限制 {_uploadSettings.MaxFileSizeMB}MB");
        }
    }

    /// <summary>
    /// 计算文件的MD5哈希值
    /// 用于文件去重
    /// </summary>
    /// <param name="stream">文件流</param>
    /// <returns>MD5哈希字符串</returns>
    private string CalculateMD5(Stream stream)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
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
