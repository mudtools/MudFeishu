// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FeishuFileServer.Models;
using Mud.Feishu;
using Mud.Feishu.DataModels.Drive;
using Mud.Feishu.DataModels.Drive.Files;
using Mud.Feishu.Exceptions;

namespace FeishuFileServer.Services.Feishu;

/// <summary>
/// 飞书云盘服务实现
/// 封装飞书云盘API的调用逻辑
/// </summary>
public class FeishuDriveService : IFeishuDriveService
{
    private readonly IFeishuTenantV1DriveFiles _driveFiles;
    private readonly IFeishuTenantV1DriveFolder _driveFolder;
    private readonly IFeishuTenantV1BatchMessage _message;
    private readonly IFeishuTenantV1ChatGroup _chatGroup;
    private readonly ILogger<FeishuDriveService> _logger;
    private readonly string _tempDirectory;

    /// <summary>
    /// 初始化飞书云盘服务实例
    /// </summary>
    /// <param name="driveFiles">飞书文件API</param>
    /// <param name="driveFolder">飞书文件夹API</param>
    /// <param name="logger">日志记录器</param>
    public FeishuDriveService(
        IFeishuTenantV1DriveFiles driveFiles,
        IFeishuTenantV1DriveFolder driveFolder,
        IFeishuTenantV1BatchMessage message,
        IFeishuTenantV1ChatGroup chatGroup,
        ILogger<FeishuDriveService> logger)
    {
        _driveFiles = driveFiles;
        _driveFolder = driveFolder;
        _message = message;
        _chatGroup = chatGroup;
        _logger = logger;
        _tempDirectory = Path.Combine(Path.GetTempPath(), "FeishuFileServer");
        Directory.CreateDirectory(_tempDirectory);
    }

    /// <summary>
    /// 上传文件到飞书云盘
    /// 使用临时文件作为中转，支持大文件上传
    /// </summary>
    /// <param name="fileStream">文件流</param>
    /// <param name="fileName">文件名</param>
    /// <param name="folderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件记录</returns>
    public async Task<FileRecord> UploadFileAsync(Stream fileStream, string fileName, string? folderToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading file {FileName} to folder {FolderToken}", fileName, folderToken ?? "root");

        var tempFilePath = Path.Combine(_tempDirectory, $"{Guid.NewGuid()}_{fileName}");
        try
        {
            fileStream.Position = 0;
            using (var fs = File.Create(tempFilePath))
            {
                await fileStream.CopyToAsync(fs, cancellationToken);
            }

            var fileSize = new FileInfo(tempFilePath).Length;
            var uploadRequest = new UploadAllFileRequest
            {
                FileName = fileName,
                ParentType = "explorer",
                ParentNode = folderToken ?? string.Empty,
                Size = (int)fileSize,
                FilePath = tempFilePath
            };

            var result = await _driveFiles.UploadAllFileAsync(uploadRequest, cancellationToken);

            if (result?.Data == null)
            {
                throw new Exception($"Failed to upload file to Feishu: {result?.Msg ?? "Unknown error"}");
            }

            var fileToken = result.Data.FileToken;
            _logger.LogInformation("File uploaded successfully with token: {FileToken}", fileToken);

            var fileInfo = new FileRecord
            {
                FileToken = fileToken,
                FolderToken = folderToken,
                FileName = fileName,
                FileSize = fileSize,
                MimeType = GetMimeType(fileName),
                UploadTime = DateTime.UtcNow
            };

            return fileInfo;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }
        }
    }

    /// <summary>
    /// 初始化分片上传到飞书云盘
    /// </summary>
    public async Task<string> InitChunkUploadAsync(string fileName, long fileSize, string? folderToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing chunk upload for {FileName}, size: {FileSize}", fileName, fileSize);

        var request = new FilesUploadPrepareRequest
        {
            FileName = fileName,
            ParentType = "explorer",
            ParentNode = folderToken ?? string.Empty,
            Size = (int)fileSize
        };

        var result = await _driveFiles.UploadPrepareFileAsync(request, cancellationToken);

        if (result?.Data == null)
        {
            throw new Exception($"Failed to init chunk upload: {result?.Msg ?? "Unknown error"}");
        }

        _logger.LogInformation("Chunk upload initialized, uploadId: {UploadId}", result.Data.UploadId);
        return result.Data.UploadId ?? throw new Exception("UploadId is null");
    }

    /// <summary>
    /// 上传分片到飞书云盘
    /// </summary>
    public async Task UploadChunkAsync(string uploadId, int seq, byte[] chunkData, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading chunk {Seq} for uploadId: {UploadId}", seq, uploadId);

        var tempChunkPath = Path.Combine(_tempDirectory, $"chunk_{uploadId}_{seq}");
        try
        {
            await File.WriteAllBytesAsync(tempChunkPath, chunkData, cancellationToken);

            var request = new FilesUploadPartRequest
            {
                UploadId = uploadId,
                Seq = seq,
                Size = chunkData.Length,
                FileName = tempChunkPath
            };

            var result = await _driveFiles.UploadPartFileAsync(request, cancellationToken);

            if (result?.Code != 0 && result?.Code != null)
            {
                throw new Exception($"Failed to upload chunk: {result.Msg}");
            }

            _logger.LogInformation("Chunk {Seq} uploaded successfully", seq);
        }
        finally
        {
            if (File.Exists(tempChunkPath))
            {
                File.Delete(tempChunkPath);
            }
        }
    }

    /// <summary>
    /// 完成分片上传到飞书云盘
    /// </summary>
    public async Task<string> CompleteChunkUploadAsync(string uploadId, int totalChunks, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Completing chunk upload for uploadId: {UploadId}, totalChunks: {TotalChunks}", uploadId, totalChunks);

        var request = new FilesUploadFinishRequest
        {
            UploadId = uploadId,
            BlockNum = totalChunks
        };

        var result = await _driveFiles.UploadFinishFileAsync(request, cancellationToken);

        if (result?.Data == null)
        {
            throw new Exception($"Failed to complete chunk upload: {result?.Msg ?? "Unknown error"}");
        }

        var fileToken = result.Data.FileToken ?? throw new Exception("FileToken is null");
        _logger.LogInformation("Chunk upload completed, fileToken: {FileToken}", fileToken);
        return fileToken;
    }

    /// <summary>
    /// 从飞书云盘下载文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件内容字节数组</returns>
    public async Task<byte[]> DownloadFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Downloading file {FileToken}", fileToken);

        var content = await _driveFiles.DownloadFileAsync(fileToken, cancellationToken: cancellationToken);

        if (content == null)
        {
            throw new Exception($"Failed to download file {fileToken}");
        }

        return content;
    }

    /// <summary>
    /// 从飞书云盘删除文件
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteFileAsync(string fileToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting file {FileToken} from Feishu cloud", fileToken);

        try
        {
            var result = await _driveFiles.DeleteFileByFileTokenAsync(fileToken, "file", cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Delete file API returned null result for {FileToken}", fileToken);
                return;
            }

            if (result.Code == 0)
            {
                _logger.LogInformation("File deleted successfully from Feishu cloud: {FileToken}", fileToken);
                return;
            }

            var errorMsg = result.Msg ?? "Unknown error";
            var errorCode = result.Code;

            var notFoundCodes = new[] { 10010, 1060101, 1060102, 1060103 };
            var notFoundKeywords = new[] { "not found", "不存在", "no such file", "file not found", "resource not found", "permission denied", "无权限", "no permission" };

            if (notFoundCodes.Contains(errorCode) || notFoundKeywords.Any(k => errorMsg.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("File not found or no permission in Feishu cloud (Code: {Code}, Msg: {Msg}): {FileToken}", errorCode, errorMsg, fileToken);
                return;
            }

            _logger.LogWarning("Delete file from Feishu cloud returned error (Code: {Code}, Msg: {Msg}): {FileToken}", errorCode, errorMsg, fileToken);
        }
        catch (FeishuException ex)
        {
            _logger.LogWarning("Feishu API error when deleting file {FileToken}: Code={Code}, Message={Message}", fileToken, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception when deleting file from Feishu cloud: {FileToken}. The file may not exist or no permission.", fileToken);
        }
    }

    /// <summary>
    /// 从飞书云盘删除文件夹
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task DeleteFolderAsync(string folderToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting folder {FolderToken} from Feishu cloud", folderToken);

        try
        {
            var result = await _driveFiles.DeleteFileByFileTokenAsync(folderToken, "folder", cancellationToken);

            if (result == null)
            {
                _logger.LogWarning("Delete folder API returned null result for {FolderToken}", folderToken);
                return;
            }

            if (result.Code == 0)
            {
                _logger.LogInformation("Folder deleted successfully from Feishu cloud: {FolderToken}", folderToken);
                return;
            }

            var errorMsg = result.Msg ?? "Unknown error";
            var errorCode = result.Code;

            var notFoundCodes = new[] { 10010, 1060101, 1060102, 1060103 };
            var notFoundKeywords = new[] { "not found", "不存在", "no such folder", "folder not found", "resource not found", "permission denied", "无权限", "no permission" };

            if (notFoundCodes.Contains(errorCode) || notFoundKeywords.Any(k => errorMsg.Contains(k, StringComparison.OrdinalIgnoreCase)))
            {
                _logger.LogWarning("Folder not found or no permission in Feishu cloud (Code: {Code}, Msg: {Msg}): {FolderToken}", errorCode, errorMsg, folderToken);
                return;
            }

            _logger.LogWarning("Delete folder from Feishu cloud returned error (Code: {Code}, Msg: {Msg}): {FolderToken}", errorCode, errorMsg, folderToken);
        }
        catch (FeishuException ex)
        {
            _logger.LogWarning("Feishu API error when deleting folder {FolderToken}: Code={Code}, Message={Message}", folderToken, ex.ErrorCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception when deleting folder from Feishu cloud: {FolderToken}. The folder may not exist or no permission.", folderToken);
        }
    }

    /// <summary>
    /// 移动文件到目标文件夹
    /// </summary>
    /// <param name="fileToken">文件令牌</param>
    /// <param name="destFolderToken">目标文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    public async Task MoveFileAsync(string fileToken, string destFolderToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Moving file {FileToken} to folder {DestFolderToken}", fileToken, destFolderToken);

        var moveRequest = new MoveFileRequest
        {
            Type = "file",
            FolderToken = destFolderToken
        };

        var result = await _driveFiles.MoveFileByFileTokenAsync(moveRequest, fileToken, cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            throw new Exception($"Failed to move file: {result.Msg}");
        }
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
        _logger.LogInformation("Copying file {FileToken} to folder {DestFolderToken}", fileToken, destFolderToken);

        var copyRequest = new CopyFileRequest
        {
            Name = newName ?? "Copy",
            Type = "file",
            FolderToken = destFolderToken
        };

        var result = await _driveFiles.CopyFileByFileTokenAsync(copyRequest, fileToken, cancellationToken: cancellationToken);

        if (result?.Code != 0 && result?.Code != null)
        {
            throw new Exception($"Failed to copy file: {result.Msg}");
        }
    }

    /// <summary>
    /// 获取文件夹内的文件列表
    /// </summary>
    /// <param name="folderToken">文件夹令牌</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>文件记录列表</returns>
    public async Task<List<FileRecord>> GetFilesAsync(string? folderToken = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting files from folder {FolderToken}", folderToken ?? "root");

        var result = await _driveFolder.GetFilesPageListAsync(folderToken, cancellationToken: cancellationToken);

        if (result?.Data?.Files == null)
        {
            return new List<FileRecord>();
        }

        return result.Data.Files.Select(f => new FileRecord
        {
            FileToken = f.Token ?? string.Empty,
            FileName = f.Name ?? string.Empty,
            FileSize = 0,
            MimeType = f.Type ?? "application/octet-stream",
            FolderToken = folderToken,
            UploadTime = ParseCreateTime(f.CreatedTime)
        }).ToList();
    }

    /// <summary>
    /// 解析创建时间字符串
    /// 支持Unix时间戳和ISO日期格式
    /// </summary>
    /// <param name="timeStr">时间字符串</param>
    /// <returns>DateTime对象</returns>
    private DateTime ParseCreateTime(string? timeStr)
    {
        if (string.IsNullOrEmpty(timeStr))
        {
            return DateTime.UtcNow;
        }

        if (long.TryParse(timeStr, out var timestamp))
        {
            return DateTimeOffset.FromUnixTimeSeconds(timestamp).DateTime;
        }

        if (DateTime.TryParse(timeStr, out var dt))
        {
            return dt;
        }

        return DateTime.UtcNow;
    }

    /// <summary>
    /// 根据文件扩展名获取MIME类型
    /// </summary>
    /// <param name="fileName">文件名</param>
    /// <returns>MIME类型字符串</returns>
    private string GetMimeType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".tiff" => "image/tiff",
            ".pdf" => "application/pdf",
            _ => "application/octet-stream"
        };
    }
}
