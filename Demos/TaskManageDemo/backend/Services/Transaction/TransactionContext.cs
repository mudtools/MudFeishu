// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Storage;

namespace TaskManageDemo.Backend.Services.Transaction;

/// <summary>
/// 事务上下文实现
/// </summary>
public class TransactionContext : ITransactionContext
{
    private readonly IDbContextTransaction _transaction;
    private readonly ILogger _logger;
    private bool _disposed = false;

    /// <summary>
    /// 初始化事务上下文
    /// </summary>
    public TransactionContext(IDbContextTransaction transaction, ILogger logger)
    {
        _transaction = transaction;
        _logger = logger;
    }

    /// <summary>
    /// 提交事务
    /// </summary>
    public async Task CommitAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TransactionContext));
        }

        await _transaction.CommitAsync();
        _logger.LogDebug("事务已提交");
    }

    /// <summary>
    /// 回滚事务
    /// </summary>
    public async Task RollbackAsync()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TransactionContext));
        }

        await _transaction.RollbackAsync();
        _logger.LogDebug("事务已回滚");
    }

    /// <summary>
    /// 释放资源
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "释放事务资源时发生错误");
        }
        finally
        {
            _disposed = true;
        }
    }
}
