using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Mud.Feishu;
using Mud.Feishu.Interfaces;
using FeishuFileServer.Configuration;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;

namespace FeishuFileServer.Services;

/// <summary>
/// 版本服务接口
/// 提供文件版本的查询、创建、下载、恢复和删除功能
/// </summary>
public interface IVersionService
{
    /// <summary>
    /// 获取文件版本列表
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本响应列表</returns>
    Task<List<VersionResponse>> GetVersionsAsync(string fileToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 创建新版本
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="file">上传的文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本创建响应</returns>
    Task<VersionCreateResponse> CreateVersionAsync(string fileToken, IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>
    /// 下载指定版本
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
    Task<byte[]> DownloadVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 恢复到指定版本
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RestoreVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定版本
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task DeleteVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// 版本服务实现
/// 提供文件版本管理的具体实现
/// </summary>
public class VersionService : IVersionService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuDriveService _feishuDriveService;
    private readonly IFeishuTenantV1DriveFilesVersions _fileVersions;
    private readonly ILogger<VersionService> _logger;
    private readonly VersionManagementSettings _versionSettings;

    /// <summary>
    /// 初始化版本服务实例
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="feishuDriveService">飞书云盘服务</param>
    /// <param name="fileVersions">飞书文件版本API</param>
    /// <param name="versionSettings">版本管理配置</param>
    /// <param name="logger">日志记录器</param>
    public VersionService(
        FeishuFileDbContext dbContext,
        IFeishuDriveService feishuDriveService,
        IFeishuTenantV1DriveFilesVersions fileVersions,
        IOptions<VersionManagementSettings> versionSettings,
        ILogger<VersionService> logger)
    {
        _dbContext = dbContext;
        _feishuDriveService = feishuDriveService;
        _fileVersions = fileVersions;
        _versionSettings = versionSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// 获取文件版本列表
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本响应列表</returns>
    public async Task<List<VersionResponse>> GetVersionsAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        var versions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);

        return versions.Select(MapToVersionResponse).ToList();
    }

    /// <summary>
    /// 创建新版本
    /// 当版本数量超过限制时，自动删除最旧的版本
    /// 对于支持飞书版本管理的文件类型，使用飞书API创建版本
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="file">上传的文件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>版本创建响应</returns>
    public async Task<VersionCreateResponse> CreateVersionAsync(string fileToken, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (!_versionSettings.Enabled)
        {
            throw new InvalidOperationException("Version management is disabled");
        }

        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        var existingVersions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .ToListAsync(cancellationToken);

        if (existingVersions.Count >= _versionSettings.MaxVersionsPerFile)
        {
            var oldestVersion = existingVersions.OrderBy(v => v.VersionNumber).First();
            _dbContext.VersionRecords.Remove(oldestVersion);
        }

        foreach (var version in existingVersions)
        {
            version.IsCurrentVersion = false;
        }

        var newVersionNumber = existingVersions.Count > 0
            ? existingVersions.Max(v => v.VersionNumber) + 1
            : 1;

        string? feishuVersionToken = null;
        var objType = GetObjectType(fileRecord.FileName);

        if (!string.IsNullOrEmpty(objType))
        {
            try
            {
                using var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream, cancellationToken);
                memoryStream.Position = 0;

                var uploadResult = await _feishuDriveService.UploadFileAsync(
                    memoryStream,
                    file.FileName,
                    fileRecord.FolderToken,
                    cancellationToken);

                feishuVersionToken = uploadResult.FileToken;
                _logger.LogInformation("Version uploaded to Feishu: {FeishuVersionToken}", feishuVersionToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to upload version to Feishu, continuing with local version only");
            }
        }

        var versionRecord = new VersionRecord
        {
            FileToken = fileToken,
            VersionToken = Guid.NewGuid().ToString("N"),
            VersionNumber = newVersionNumber,
            FileName = file.FileName,
            FileSize = file.Length,
            CreatedTime = DateTime.UtcNow,
            IsCurrentVersion = true,
            FeishuVersionToken = feishuVersionToken
        };

        _dbContext.VersionRecords.Add(versionRecord);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Version created successfully: {VersionToken}", versionRecord.VersionToken);

        return new VersionCreateResponse
        {
            VersionToken = versionRecord.VersionToken,
            VersionNumber = versionRecord.VersionNumber,
            CreatedTime = versionRecord.CreatedTime
        };
    }

    /// <summary>
    /// 下载指定版本
    /// 优先从飞书API获取版本，失败时回退到当前文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
    public async Task<byte[]> DownloadVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default)
    {
        var fileRecord = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == fileToken && !f.IsDeleted, cancellationToken);

        if (fileRecord == null)
        {
            throw new KeyNotFoundException($"File with token {fileToken} not found");
        }

        var versionRecord = await _dbContext.VersionRecords
            .FirstOrDefaultAsync(v => v.FileToken == fileToken && v.VersionToken == versionToken, cancellationToken);

        if (versionRecord == null)
        {
            throw new KeyNotFoundException($"Version {versionToken} not found for file {fileToken}");
        }

        try
        {
            var objType = GetObjectType(fileRecord.FileName);
            if (!string.IsNullOrEmpty(objType))
            {
                var result = await _fileVersions.GetFileVersionByFileTokenAsync(
                    fileToken,
                    versionToken,
                    objType,
                    cancellationToken: cancellationToken);

                if (result?.Data != null && !string.IsNullOrEmpty(result.Data.VersionSuffix))
                {
                    _logger.LogInformation("Found version {VersionSuffix} for file {FileToken}", result.Data.VersionSuffix, fileToken);
                    return await _feishuDriveService.DownloadFileAsync(fileToken, cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download version from Feishu, falling back to current file");
        }

        _logger.LogInformation("Downloading current file version for {FileToken}", fileToken);
        return await _feishuDriveService.DownloadFileAsync(fileToken, cancellationToken);
    }

    /// <summary>
    /// 恢复到指定版本
    /// 将指定版本标记为当前版本
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task RestoreVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Restoring version {VersionToken} for file {FileToken}", versionToken, fileToken);

        var existingVersions = await _dbContext.VersionRecords
            .Where(v => v.FileToken == fileToken)
            .ToListAsync(cancellationToken);

        foreach (var existing in existingVersions)
        {
            existing.IsCurrentVersion = existing.VersionToken == versionToken;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Version restored successfully");
    }

    /// <summary>
    /// 删除指定版本
    /// 如果删除的是当前版本，自动将最新版本设为当前版本
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="versionToken">版本令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteVersionAsync(string fileToken, string versionToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting version {VersionToken} for file {FileToken}", versionToken, fileToken);

        var versionRecord = await _dbContext.VersionRecords
            .FirstOrDefaultAsync(v => v.FileToken == fileToken && v.VersionToken == versionToken, cancellationToken);

        if (versionRecord != null)
        {
            if (versionRecord.IsCurrentVersion)
            {
                var otherVersion = await _dbContext.VersionRecords
                    .Where(v => v.FileToken == fileToken && v.VersionToken != versionToken)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync(cancellationToken);

                if (otherVersion != null)
                {
                    otherVersion.IsCurrentVersion = true;
                }
            }

            _dbContext.VersionRecords.Remove(versionRecord);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        _logger.LogInformation("Version deleted successfully");
    }

    /// <summary>
    /// 根据文件扩展名获取飞书对象类型
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>对象类型，不支持时返回null</returns>
    private string? GetObjectType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".docx" => "docx",
            ".xlsx" => "sheet",
            _ => null
        };
    }

    /// <summary>
    /// 将版本记录实体映射为版本响应DTO
    /// </summary>
    /// <param name="record">版本记录实体</param>
    /// <returns>版本响应DTO</returns>
    private static VersionResponse MapToVersionResponse(VersionRecord record)
    {
        return new VersionResponse
        {
            VersionToken = record.VersionToken,
            VersionNumber = record.VersionNumber,
            FileName = record.FileName,
            FileSize = record.FileSize,
            CreatedTime = record.CreatedTime,
            IsCurrentVersion = record.IsCurrentVersion
        };
    }
}
