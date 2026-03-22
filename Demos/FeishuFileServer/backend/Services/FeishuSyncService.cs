using System.Diagnostics;
using FeishuFileServer.Data;
using FeishuFileServer.Models;
using FeishuFileServer.Models.DTOs;
using FeishuFileServer.Services.Feishu;
using Microsoft.EntityFrameworkCore;
using Mud.Feishu;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.Interfaces;

using FileInfo = Mud.Feishu.DataModels.Drive.Folder.FileInfo;

namespace FeishuFileServer.Services;

/// <summary>
/// 飞书云盘同步服务
/// 提供从飞书云盘同步文件和文件夹数据到本地数据库的功能
/// </summary>
public class FeishuSyncService : IFeishuSyncService
{
    private readonly FeishuFileDbContext _dbContext;
    private readonly IFeishuTenantV1DriveFolder _driveFolder;
    private readonly IFeishuTenantV1DriveFiles _driveFiles;
    private readonly ILogger<FeishuSyncService> _logger;
    
    /// <summary>
    /// 用户同步状态缓存
    /// 键为用户ID，值为同步状态
    /// </summary>
    private static readonly Dictionary<int, SyncStatus> _syncStatuses = new();

    /// <summary>
    /// 初始化飞书同步服务
    /// </summary>
    /// <param name="dbContext">数据库上下文</param>
    /// <param name="driveFolder">飞书文件夹接口</param>
    /// <param name="driveFiles">飞书文件接口</param>
    /// <param name="logger">日志记录器</param>
    public FeishuSyncService(
        FeishuFileDbContext dbContext,
        IFeishuTenantV1DriveFolder driveFolder,
        IFeishuTenantV1DriveFiles driveFiles,
        ILogger<FeishuSyncService> logger)
    {
        _dbContext = dbContext;
        _driveFolder = driveFolder;
        _driveFiles = driveFiles;
        _logger = logger;
    }

    /// <summary>
    /// 同步所有文件和文件夹
    /// 从飞书云盘根目录开始递归同步所有数据
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果，包含新增和更新的文件/文件夹数量</returns>
    public async Task<SyncResult> SyncAllAsync(int userId, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult
        {
            SyncTime = DateTime.UtcNow
        };

        try
        {
            UpdateSyncStatus(userId, true, 0, "开始同步...");

            var rootFolders = await SyncFolderRecursiveAsync(null, userId, result, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Success = true;
            _logger.LogInformation("同步完成: 新增文件夹 {AddedFolders}, 更新文件夹 {UpdatedFolders}, 新增文件 {AddedFiles}, 更新文件 {UpdatedFiles}",
                result.AddedFolders, result.UpdatedFolders, result.AddedFiles, result.UpdatedFiles);

            UpdateSyncStatus(userId, false, 100, "同步完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步失败");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            UpdateSyncStatus(userId, false, 0, $"同步失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 同步指定文件夹
    /// 从飞书云盘同步指定文件夹及其子内容到本地数据库
    /// </summary>
    /// <param name="folderToken">文件夹Token</param>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步结果，包含新增和更新的文件/文件夹数量</returns>
    public async Task<SyncResult> SyncFolderAsync(string folderToken, int userId, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult
        {
            SyncTime = DateTime.UtcNow
        };

        try
        {
            UpdateSyncStatus(userId, true, 0, $"同步文件夹 {folderToken}...");

            await SyncFolderRecursiveAsync(folderToken, userId, result, cancellationToken);

            await _dbContext.SaveChangesAsync(cancellationToken);

            result.Success = true;

            UpdateSyncStatus(userId, false, 100, "同步完成");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "同步文件夹失败");
            result.Success = false;
            result.ErrorMessage = ex.Message;
            UpdateSyncStatus(userId, false, 0, $"同步失败: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 获取用户的同步状态
    /// 返回最近一次同步的状态信息
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步状态信息</returns>
    public Task<SyncStatus> GetSyncStatusAsync(int userId, CancellationToken cancellationToken = default)
    {
        if (_syncStatuses.TryGetValue(userId, out var status))
        {
            return Task.FromResult(status);
        }

        return Task.FromResult(new SyncStatus
        {
            IsSyncing = false,
            Progress = 0
        });
    }

    /// <summary>
    /// 递归同步文件夹及其子内容
    /// 遍历文件夹下的所有文件和子文件夹，并同步到本地数据库
    /// </summary>
    /// <param name="folderToken">文件夹Token，为null时从根目录开始</param>
    /// <param name="userId">用户ID</param>
    /// <param name="result">同步结果对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步的文件夹列表</returns>
    private async Task<List<FolderRecord>> SyncFolderRecursiveAsync(string? folderToken, int userId, SyncResult result, CancellationToken cancellationToken)
    {
        var folders = new List<FolderRecord>();
        var allFiles = new List<FileInfo>();
        string? pageToken = null;

        do
        {
            var response = await _driveFolder.GetFilesPageListAsync(
                folderToken,
                page_token: pageToken,
                cancellationToken: cancellationToken);

            if (response?.Data?.Files == null || response.Data.Files.Length == 0)
            {
                break;
            }

            foreach (var file in response.Data.Files)
            {
                if (file.Type == "folder")
                {
                    var folder = await SyncFolderRecordAsync(file, folderToken, userId, result, cancellationToken);
                    if (folder != null)
                    {
                        folders.Add(folder);
                        result.SyncedFolders++;

                        await SyncFolderRecursiveAsync(file.Token, userId, result, cancellationToken);
                    }
                }
                else
                {
                    allFiles.Add(file);
                }
            }

            pageToken = response.Data.NextPageToken;
        }
        while (!string.IsNullOrEmpty(pageToken));

        if (allFiles.Count > 0)
        {
            await SyncFileRecordsWithMetadataAsync(allFiles, folderToken, userId, result, cancellationToken);
            result.SyncedFiles += allFiles.Count;
        }

        return folders;
    }

    /// <summary>
    /// 批量同步文件记录及其元数据
    /// 使用批量查询接口获取文件元数据，提高同步效率
    /// </summary>
    /// <param name="files">文件信息列表</param>
    /// <param name="folderToken">所属文件夹Token</param>
    /// <param name="userId">用户ID</param>
    /// <param name="result">同步结果对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SyncFileRecordsWithMetadataAsync(List<FileInfo> files, string? folderToken, int userId, SyncResult result, CancellationToken cancellationToken)
    {
        /// <summary>
        /// 每批处理的最大文件数量
        /// </summary>
        const int batchSize = 50;
        
        for (int i = 0; i < files.Count; i += batchSize)
        {
            var batch = files.Skip(i).Take(batchSize).ToList();
            
            var requestDocs = batch
                .Where(f => !string.IsNullOrEmpty(f.Token))
                .Select(f => new RequestDoc
                {
                    DocToken = f.Token!,
                    DocType = MapFileType(f.Type)
                })
                .ToArray();

            if (requestDocs.Length == 0)
            {
                continue;
            }

            var metasRequest = new MetasBatchQueryRequest
            {
                RequestDocs = requestDocs
            };

            /// <summary>
            /// 文件Token到元数据的映射字典
            /// </summary>
            Dictionary<string, FileMetaInfo> metaDict = new();
            
            try
            {
                var metasResponse = await _driveFiles.BatchQueryMetasAsync(metasRequest, cancellationToken: cancellationToken);
                
                if (metasResponse?.Data?.Metas != null)
                {
                    foreach (var meta in metasResponse.Data.Metas)
                    {
                        if (!string.IsNullOrEmpty(meta.DocToken))
                        {
                            metaDict[meta.DocToken] = meta;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "批量获取文件元数据失败");
            }

            foreach (var file in batch)
            {
                if (string.IsNullOrEmpty(file.Token))
                {
                    continue;
                }

                metaDict.TryGetValue(file.Token, out var metaInfo);
                await SyncFileRecordAsync(file, folderToken, userId, result, metaInfo, cancellationToken);
            }
        }
    }

    /// <summary>
    /// 映射飞书文件类型到标准文件类型
    /// </summary>
    /// <param name="type">飞书文件类型</param>
    /// <returns>标准文件类型</returns>
    private static string MapFileType(string? type)
    {
        return type switch
        {
            "doc" => "doc",
            "docx" => "docx",
            "sheet" => "sheet",
            "bitable" => "bitable",
            "mindnote" => "mindnote",
            "file" => "file",
            "folder" => "folder",
            "shortcut" => "file",
            "slides" => "file",
            "wiki" => "wiki",
            _ => "file"
        };
    }

    /// <summary>
    /// 同步单个文件夹记录
    /// 如果文件夹不存在则创建，否则更新
    /// </summary>
    /// <param name="file">飞书文件夹信息</param>
    /// <param name="parentFolderToken">父文件夹Token</param>
    /// <param name="userId">用户ID</param>
    /// <param name="result">同步结果对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>同步后的文件夹记录，如果Token为空则返回null</returns>
    private async Task<FolderRecord?> SyncFolderRecordAsync(FileInfo file, string? parentFolderToken, int userId, SyncResult result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.Token))
        {
            return null;
        }

        var existingFolder = await _dbContext.FolderRecords
            .FirstOrDefaultAsync(f => f.FolderToken == file.Token, cancellationToken);

        if (existingFolder == null)
        {
            var newFolder = new FolderRecord
            {
                FolderToken = file.Token,
                FolderName = file.Name ?? "未命名文件夹",
                ParentFolderToken = parentFolderToken,
                CreatedTime = ParseDateTime(file.CreatedTime) ?? DateTime.UtcNow,
                IsDeleted = false,
                UserId = userId
            };

            _dbContext.FolderRecords.Add(newFolder);
            result.AddedFolders++;

            _logger.LogDebug("新增文件夹: {FolderName} ({FolderToken}), 父文件夹: {ParentFolderToken}", newFolder.FolderName, newFolder.FolderToken, parentFolderToken);
            return newFolder;
        }
        else
        {
            var needUpdate = false;

            if (existingFolder.FolderName != file.Name)
            {
                existingFolder.FolderName = file.Name ?? existingFolder.FolderName;
                needUpdate = true;
            }

            if (existingFolder.ParentFolderToken != parentFolderToken && !string.IsNullOrEmpty(parentFolderToken))
            {
                existingFolder.ParentFolderToken = parentFolderToken;
                needUpdate = true;
            }

            if (needUpdate)
            {
                result.UpdatedFolders++;
                _logger.LogDebug("更新文件夹: {FolderName} ({FolderToken})", existingFolder.FolderName, existingFolder.FolderToken);
            }
            
            return existingFolder;
        }
    }

    /// <summary>
    /// 同步单个文件记录
    /// 如果文件不存在则创建，否则更新
    /// </summary>
    /// <param name="file">飞书文件信息</param>
    /// <param name="folderToken">所属文件夹Token</param>
    /// <param name="userId">用户ID</param>
    /// <param name="result">同步结果对象</param>
    /// <param name="metaInfo">文件元数据（可选）</param>
    /// <param name="cancellationToken">取消令牌</param>
    private async Task SyncFileRecordAsync(FileInfo file, string? folderToken, int userId, SyncResult result, FileMetaInfo? metaInfo, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(file.Token))
        {
            return;
        }

        var existingFile = await _dbContext.FileRecords
            .FirstOrDefaultAsync(f => f.FileToken == file.Token, cancellationToken);

        var fileName = metaInfo?.Title ?? file.Name ?? "未命名文件";
        var createTime = ParseDateTime(metaInfo?.CreateTime ?? file.CreatedTime) ?? DateTime.UtcNow;
        var modifyTime = ParseDateTime(metaInfo?.LatestModifyTime) ?? createTime;

        if (existingFile == null)
        {
            var newFile = new FileRecord
            {
                FileToken = file.Token,
                FileName = fileName,
                FileSize = 0,
                MimeType = GetMimeType(file.Type),
                FolderToken = folderToken,
                UploadTime = createTime,
                UserId = userId,
                IsDeleted = false
            };

            _dbContext.FileRecords.Add(newFile);
            result.AddedFiles++;

            _logger.LogDebug("新增文件: {FileName} ({FileToken}), 类型: {FileType}", newFile.FileName, newFile.FileToken, file.Type);
        }
        else
        {
            var needUpdate = false;

            if (existingFile.FileName != fileName)
            {
                existingFile.FileName = fileName;
                needUpdate = true;
            }

            if (existingFile.FolderToken != folderToken)
            {
                existingFile.FolderToken = folderToken;
                needUpdate = true;
            }

            if (needUpdate)
            {
                result.UpdatedFiles++;
                _logger.LogDebug("更新文件: {FileName} ({FileToken})", existingFile.FileName, existingFile.FileToken);
            }
        }
    }

    /// <summary>
    /// 根据飞书文件类型获取MIME类型
    /// </summary>
    /// <param name="fileType">飞书文件类型</param>
    /// <returns>MIME类型字符串</returns>
    private static string GetMimeType(string? fileType)
    {
        return fileType switch
        {
            "doc" => "application/msword",
            "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "sheet" => "application/vnd.ms-excel",
            "bitable" => "application/vnd.lark.lark-table",
            "mindnote" => "application/vnd.lark.lark-mindnote",
            "slides" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "file" => "application/octet-stream",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// 更新用户的同步状态
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="isSyncing">是否正在同步</param>
    /// <param name="progress">同步进度（0-100）</param>
    /// <param name="currentItem">当前同步项描述</param>
    private void UpdateSyncStatus(int userId, bool isSyncing, int progress, string currentItem)
    {
        var status = new SyncStatus
        {
            IsSyncing = isSyncing,
            Progress = progress,
            CurrentItem = currentItem,
            LastSyncTime = DateTime.UtcNow
        };

        if (_syncStatuses.ContainsKey(userId))
        {
            _syncStatuses[userId] = status;
        }
        else
        {
            _syncStatuses.Add(userId, status);
        }
    }

    /// <summary>
    /// 解析时间字符串为DateTime对象
    /// 支持Unix时间戳和标准日期时间格式
    /// </summary>
    /// <param name="timeStr">时间字符串</param>
    /// <returns>解析后的DateTime，解析失败返回null</returns>
    private static DateTime? ParseDateTime(string? timeStr)
    {
        if (string.IsNullOrEmpty(timeStr))
        {
            return null;
        }

        if (long.TryParse(timeStr, out var timestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        if (DateTime.TryParse(timeStr, out var dt))
        {
            return dt;
        }

        return null;
    }
}
