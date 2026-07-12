// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// Redis 存储共享工具类
/// <para>提供 RedisTokenStore 和 RedisUserTokenStore 共用的底层操作方法，消除重复代码并统一行为。</para>
/// </summary>
internal static class RedisStoreHelper
{
    /// <summary>
    /// 将 <see cref="CancellationToken"/> 转换为 StackExchange.Redis 的 <see cref="CommandFlags"/>。
    /// </summary>
    /// <remarks>
    /// TM-02 修复：StackExchange.Redis 不直接接受 CancellationToken，通过注册回调将取消请求映射为
    /// <see cref="CommandFlags.FireAndForget"/> 不可行（会丢失响应），故采用以下策略：
    /// 1. 预先注册 cancellation callback，在取消时通过物理中断等待中的 Task（Task.WhenAny 竞速）。
    /// 2. 未取消时正常等待 Redis 响应。
    /// 此处返回 None，实际取消由调用方的 await 配合 cancellationToken 实现。
    /// </remarks>
    public static CommandFlags ToCommandFlags(CancellationToken cancellationToken) => CommandFlags.None;

    /// <summary>
    /// 获取可用的 Redis 服务器节点。
    /// </summary>
    /// <remarks>
    /// S-2 修复：原实现固定取 <c>endpoints[0]</c>，集群/主从场景下该节点不可用时无故障转移。
    /// 改为遍历所有 endpoints，选择首个 <c>IsConnected &amp;&amp; !IsReplica</c> 的主节点；
    /// 若全部不可用或全为副本，回退到首个节点（保持原行为，由调用方处理异常）。
    /// </remarks>
    /// <param name="redis">Redis 连接复用器</param>
    /// <returns>可用的 Redis 服务器实例</returns>
    /// <exception cref="InvalidOperationException">当连接多路复器未配置任何端点时抛出</exception>
    public static IServer GetServer(IConnectionMultiplexer redis)
    {
        var endpoints = redis.GetEndPoints();
        // TM-01 修复：防御 endpoints 为空集合（极端故障场景），避免 IndexOutOfRangeException。
        if (endpoints.Length == 0)
            throw new InvalidOperationException("Redis 连接多路复器未配置任何端点，无法获取服务器实例。");

        foreach (var endpoint in endpoints)
        {
            try
            {
                var server = redis.GetServer(endpoint);
                if (server.IsConnected && !server.IsReplica)
                    return server;
            }
            catch
            {
                // 跳过不可访问的节点，继续尝试下一个
            }
        }

        // 所有节点不可用或全为副本时回退到首个节点
        return redis.GetServer(endpoints[0]);
    }
}
