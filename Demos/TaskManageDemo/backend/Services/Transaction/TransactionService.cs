// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.EntityFrameworkCore.Storage;
using TaskManageDemo.Backend.Data;

namespace TaskManageDemo.Backend.Services.Transaction;

/// <summary>
/// 事务服务实现
/// </summary>
public class TransactionService : ITransactionService
{
    private readonly TaskManageDbContext _dbContext;
    private readonly ILogger<TransactionService> _logger;

    /// <summary>
    /// 初始化事务服务
    /// </summary>
    public TransactionService(
        TaskManageDbContext dbContext,
        ILogger<TransactionService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// 开始一个新事务
    /// </summary>
    public async Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new TransactionContext(transaction, _logger);
    }

    /// <summary>
    /// 在事务中执行操作
    /// </summary>
    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<ITransactionContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        await using var context = await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(context, cancellationToken);
            await context.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事务执行失败，正在回滚");
            await context.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 在事务中执行操作
    /// </summary>
    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await using var context = await BeginTransactionAsync(cancellationToken);
        try
        {
            await operation();
            await context.CommitAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事务执行失败，正在回滚");
            await context.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// 在事务中执行操作并返回结果
    /// </summary>
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        await using var context = await BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation();
            await context.CommitAsync();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "事务执行失败，正在回滚");
            await context.RollbackAsync();
            throw;
        }
    }
}
