// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.Diagnostics;

/// <summary>
/// Redis 去重子系统诊断快照（R2-22 / E-02）。
/// </summary>
/// <remarks>
/// <para>
/// 快照在某一时刻采集，字段语义与各去重器公开 API 一致：
/// <list type="bullet">
/// <item><see cref="EventCachedCount"/>：事件键空间当前键数（SCAN 计数，含 processing 与 completed）；</item>
/// <item><see cref="NonceCachedCount"/>：Nonce 键空间当前键数（SCAN 计数）；</item>
/// <item><see cref="SeqIdCacheCount"/>：SeqID 容量窗口内的成员数（≤ <c>RedisOptions.SeqIdWindowCapacity</c>）；</item>
/// <item><see cref="SeqIdMaxProcessed"/>：该窗口内已处理 SeqID 的最大值；</item>
/// <item><see cref="ServerTimeSeconds"/>：Redis 服务端 Unix 秒（不可用时回落客户端时间）。</item>
/// </list>
/// </para>
/// <para>
/// <b>成本</b>：事件/Nonce 计数为全库 SCAN，属运维路径，禁止热路径/高频轮询调用。
/// 各 <c>*Available</c> 字段表明对应去重器是否为 Redis 实现（宿主可能替换为内存实现）。
/// </para>
/// </remarks>
public sealed class RedisDeduplicationDiagnosticsSnapshot
{
    private readonly long _serverTimeSeconds;
    private readonly bool _eventDeduplicatorAvailable;
    private readonly long _eventCachedCount;
    private readonly bool _nonceDeduplicatorAvailable;
    private readonly long _nonceCachedCount;
    private readonly bool _seqIdDeduplicatorAvailable;
    private readonly int _seqIdCacheCount;
    private readonly ulong _seqIdMaxProcessed;
    private readonly string? _seqIdScopeKey;

    /// <summary>
    /// 初始化诊断快照。
    /// </summary>
    /// <param name="serverTimeSeconds">Redis 服务端 Unix 秒（不可用时为客户端时间）</param>
    /// <param name="eventDeduplicatorAvailable">事件去重器是否为 Redis 实现</param>
    /// <param name="eventCachedCount">事件键空间键数</param>
    /// <param name="nonceDeduplicatorAvailable">Nonce 去重器是否为 Redis 实现</param>
    /// <param name="nonceCachedCount">Nonce 键空间键数</param>
    /// <param name="seqIdDeduplicatorAvailable">SeqID 去重器是否为 Redis 实现</param>
    /// <param name="seqIdCacheCount">SeqID 容量窗口内成员数</param>
    /// <param name="seqIdMaxProcessed">窗口内最大已处理 SeqID</param>
    /// <param name="seqIdScopeKey">SeqID 隔离维度键（<c>{AppKey}|{MachineName}</c> 或自定义）</param>
    public RedisDeduplicationDiagnosticsSnapshot(
        long serverTimeSeconds,
        bool eventDeduplicatorAvailable,
        long eventCachedCount,
        bool nonceDeduplicatorAvailable,
        long nonceCachedCount,
        bool seqIdDeduplicatorAvailable,
        int seqIdCacheCount,
        ulong seqIdMaxProcessed,
        string? seqIdScopeKey)
    {
        _serverTimeSeconds = serverTimeSeconds;
        _eventDeduplicatorAvailable = eventDeduplicatorAvailable;
        _eventCachedCount = eventCachedCount;
        _nonceDeduplicatorAvailable = nonceDeduplicatorAvailable;
        _nonceCachedCount = nonceCachedCount;
        _seqIdDeduplicatorAvailable = seqIdDeduplicatorAvailable;
        _seqIdCacheCount = seqIdCacheCount;
        _seqIdMaxProcessed = seqIdMaxProcessed;
        _seqIdScopeKey = seqIdScopeKey;
    }

    /// <summary>Redis 服务端 Unix 秒（TIME 不可用时回落客户端时间）。</summary>
    public long ServerTimeSeconds => _serverTimeSeconds;

    /// <summary>事件去重器是否为 Redis 实现（false 时 <see cref="EventCachedCount"/> 恒为 0）。</summary>
    public bool EventDeduplicatorAvailable => _eventDeduplicatorAvailable;

    /// <summary>事件键空间当前键数。</summary>
    public long EventCachedCount => _eventCachedCount;

    /// <summary>Nonce 去重器是否为 Redis 实现。</summary>
    public bool NonceDeduplicatorAvailable => _nonceDeduplicatorAvailable;

    /// <summary>Nonce 键空间当前键数。</summary>
    public long NonceCachedCount => _nonceCachedCount;

    /// <summary>SeqID 去重器是否为 Redis 实现。</summary>
    public bool SeqIdDeduplicatorAvailable => _seqIdDeduplicatorAvailable;

    /// <summary>SeqID 容量窗口内成员数（≤ <c>RedisOptions.SeqIdWindowCapacity</c>）。</summary>
    public int SeqIdCacheCount => _seqIdCacheCount;

    /// <summary>SeqID 窗口内最大已处理 SeqID。</summary>
    public ulong SeqIdMaxProcessed => _seqIdMaxProcessed;

    /// <summary>SeqID 隔离维度键。</summary>
    public string? SeqIdScopeKey => _seqIdScopeKey;
}
