namespace FeishuFileServer.Configuration;

/// <summary>
/// 文件上传配置
/// 配置文件上传的限制和行为
/// </summary>
public class FileUploadSettings
{
    /// <summary>
    /// 允许的文件扩展名列表
    /// </summary>
    public List<string> AllowedExtensions { get; set; } = new()
    {
        ".docx", ".xlsx", ".pptx", ".png", ".jpg", ".jpeg", ".tiff", ".pdf"
    };

    /// <summary>
    /// 最大文件大小（MB）
    /// </summary>
    public int MaxFileSizeMB { get; set; } = 100;

    /// <summary>
    /// 是否启用分片上传
    /// </summary>
    public bool EnableChunkUpload { get; set; } = true;

    /// <summary>
    /// 分片大小（MB）
    /// </summary>
    public int ChunkSizeMB { get; set; } = 5;

    /// <summary>
    /// 是否启用文件去重
    /// 通过MD5哈希值检测重复文件
    /// </summary>
    public bool EnableDeduplication { get; set; } = true;
}

/// <summary>
/// 版本管理配置
/// 配置文件版本历史的管理策略
/// </summary>
public class VersionManagementSettings
{
    /// <summary>
    /// 是否启用版本管理
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 每个文件保留的最大版本数
    /// 超过此数量时将删除最旧的版本
    /// </summary>
    public int MaxVersionsPerFile { get; set; } = 50;
}

/// <summary>
/// CORS跨域配置
/// 配置允许的跨域请求来源和方法
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// 允许的来源列表
    /// </summary>
    public List<string> AllowedOrigins { get; set; } = new() { "http://localhost:3000" };

    /// <summary>
    /// 允许的HTTP方法列表
    /// </summary>
    public List<string> AllowedMethods { get; set; } = new() { "GET", "POST", "PUT", "DELETE", "OPTIONS" };

    /// <summary>
    /// 允许的请求头列表
    /// </summary>
    public List<string> AllowedHeaders { get; set; } = new() { "*" };
}
