using FeishuFileServer.Models;

namespace FeishuFileServer.Services;

/// <summary>
/// 操作日志服务接口
/// 提供操作日志的记录和查询功能
/// </summary>
public interface IOperationLogService
{
    /// <summary>
    /// 记录操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="username">用户名</param>
    /// <param name="operationType">操作类型</param>
    /// <param name="resourceType">资源类型</param>
    /// <param name="resourceToken">资源令牌</param>
    /// <param name="resourceName">资源名称</param>
    /// <param name="details">操作详情</param>
    /// <param name="ipAddress">IP地址</param>
    /// <param name="userAgent">用户代理</param>
    /// <param name="isSuccess">是否成功</param>
    /// <param name="errorMessage">错误信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task LogAsync(int? userId, string? username, string operationType, string resourceType,
        string? resourceToken = null, string? resourceName = null, string? details = null,
        string? ipAddress = null, string? userAgent = null, bool isSuccess = true,
        string? errorMessage = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户操作日志
    /// </summary>
    /// <param name="userId">用户ID</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    Task<List<OperationLog>> GetUserLogsAsync(int userId, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取资源操作日志
    /// </summary>
    /// <param name="resourceToken">资源令牌</param>
    /// <param name="page">页码</param>
    /// <param name="pageSize">每页大小</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作日志列表</returns>
    Task<List<OperationLog>> GetResourceLogsAsync(string resourceToken, int page = 1, int pageSize = 50, CancellationToken cancellationToken = default);
}
