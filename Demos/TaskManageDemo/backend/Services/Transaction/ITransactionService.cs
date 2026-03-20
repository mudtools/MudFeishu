// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace TaskManageDemo.Backend.Services.Transaction;

/// <summary>
/// 事务服务接口
/// </summary>
public interface ITransactionService
{
    /// <summary>
    /// 开始一个新事务
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>事务上下文</returns>
    Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 在事务中执行操作
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<T> ExecuteInTransactionAsync<T>(
        Func<ITransactionContext, CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在事务中执行操作
    /// </summary>
    /// <param name="operation">要执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>任务</returns>
    Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default);

    /// <summary>
    /// 在事务中执行操作并返回结果
    /// </summary>
    /// <typeparam name="T">返回类型</typeparam>
    /// <param name="operation">要执行的操作</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>操作结果</returns>
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default);
}

/// <summary>
/// 事务上下文接口
/// </summary>
public interface ITransactionContext : IAsyncDisposable
{
    /// <summary>
    /// 提交事务
    /// </summary>
    Task CommitAsync();

    /// <summary>
    /// 回滚事务
    /// </summary>
    Task RollbackAsync();
}
