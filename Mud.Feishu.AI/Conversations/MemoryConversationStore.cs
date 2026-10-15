// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Conversations;

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 进程内内存会话存储（<see cref="IConversationStore"/> 的飞书侧薄壳；仅做适配）。
/// </summary>
/// <remarks>
/// <para>
/// TTL 软过期 + 惰性分摊清扫的<b>机制</b>在中立 <c>Mud.AI.Agent.Sessions.MemorySessionStore</c>（中立 <c>ISessionStore</c> 面）。
/// 本壳保留的原因：<see cref="IConversationStore"/> 是<b>已发布 SDK 的宿主可替换契约</b>（Redis 包与宿主实现都基于它），
/// 不能换成中立端口。职责有二：① 把冻结契约桥接到中立实现（签名不变 ⇒ 引用零改动）；② 注入飞书侧的 <b>TTL 单一阈值源</b>
/// <see cref="FeishuConversationOptions.DefaultSessionTtl"/>（中立默认 TTL 与本产品默认值必须只有一处来源）。
/// </para>
/// <para>
/// <b>过期回收</b>：软过期只在<b>读同一键</b>时删除，<b>不再被读取</b>的键永不回收 ⇒ 长跑宿主的内存随会话数单调增长。
/// 故在写入路径按阈值 + 节流触发一次<b>惰性分摊清扫</b>：≤<c>SweepThreshold</c> 条目时 O(1)；超过后按 <c>SweepEveryWrites</c>
/// 次写入节流，单次成本上界 <c>SweepMaxRemoval</c>，<b>不引入后台线程/定时器</b>，语义与 Redis 服务端过期一致。
/// </para>
/// <para>
/// <b>能力边界</b>：清扫只回收<b>已过期</b>条目——TTL 窗口内的<b>新鲜</b>条目仍无数量上限；需要按数量/字节上限淘汰的宿主
/// 应改用 Redis 后端（<c>RedisConversationStore</c>）。仅适合单实例部署与测试。
/// </para>
/// </remarks>
public sealed class MemoryConversationStore : IConversationStore
{
    /// <summary>清扫触发阈值（条目数；转发中立实现的常量，保持测试与守卫的既有锚点可用）。</summary>
    internal const int SweepThreshold = MemorySessionStore.SweepThreshold;

    /// <summary>两次清扫之间的最少写入次数（节流）。</summary>
    internal const int SweepEveryWrites = MemorySessionStore.SweepEveryWrites;

    /// <summary>单次清扫最多回收的条目数（成本上界）。</summary>
    internal const int SweepMaxRemoval = MemorySessionStore.SweepMaxRemoval;

    private readonly MemorySessionStore _inner;

    /// <summary>
    /// 初始化内存会话存储。
    /// </summary>
    /// <param name="ttl">会话 TTL（可空 ⇒ <see cref="FeishuConversationOptions.DefaultSessionTtl"/>）。</param>
    /// <param name="utcNow">UTC 时钟（测试注入用；缺省 <see cref="DateTimeOffset.UtcNow"/>）。</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ttl"/> 非正。</exception>
    public MemoryConversationStore(TimeSpan? ttl = null, Func<DateTimeOffset>? utcNow = null)
        => _inner = new MemorySessionStore(ttl ?? FeishuConversationOptions.DefaultSessionTtl, utcNow);

    /// <inheritdoc />
    public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default)
        => _inner.GetAsync(key, cancellationToken);

    /// <inheritdoc />
    public Task SaveAsync(string key, string serializedSession, CancellationToken cancellationToken = default)
        => _inner.SaveAsync(key, serializedSession, cancellationToken);

    /// <inheritdoc />
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        => _inner.DeleteAsync(key, cancellationToken);

    /// <summary>当前驻留条目数（含已过期未回收者；诊断与用例观测面）。</summary>
    internal int Count => _inner.Count;

    /// <summary>清扫调用计数（诊断/测试观测面，用于断言节流效果）。</summary>
    internal int SweepCallCount => _inner.SweepCallCount;
}
