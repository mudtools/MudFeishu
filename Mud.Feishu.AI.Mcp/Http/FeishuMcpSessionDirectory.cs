// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// 进程内 MCP 会话目录（<b>单实例形态</b>）：按 <c>Mcp-Session-Id</c> 存取 <see cref="FeishuMcpSession"/>，
/// 空闲惰性回收，数量受 <see cref="FeishuMcpHttpOptions.MaxSessions"/> 上界约束。
/// </summary>
/// <remarks>
/// <para>
/// <b>不起后台线程</b>：回收发生在「新建会话」与「命中会话」两条路径上，条目上界就是
/// <see cref="FeishuMcpHttpOptions.MaxSessions"/>，故每次 O(n) 扫描成本有界，不需要定时器，
/// 也不需要 <c>MemorySessionStore</c> 那套写入节流。
/// </para>
/// <para>
/// <b>多实例部署须把会话状态外置</b>：本目录只认自己进程里发过的标识，负载均衡把
/// <c>initialize</c> 打到 A、后续请求打到 B 时表现为 404。要么配会话粘滞，
/// 要么由宿主替换本目录的实现（会话标识 → 租户绑定的外部存储）。
/// 二者都<b>不</b>构成正确性风险：会话标识泄漏不会换租户（绑定在会话上校验），
/// 最坏形态只是「这条会话得重握手」。
/// </para>
/// </remarks>
internal sealed class FeishuMcpSessionDirectory
{
    private static readonly Func<DateTimeOffset> DefaultClock = static () => DateTimeOffset.UtcNow;

    private readonly FeishuMcpHttpOptions _options;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly Dictionary<string, FeishuMcpSession> _sessions = new(StringComparer.Ordinal);
    private readonly object _gate = new();

    /// <summary>初始化会话目录。</summary>
    /// <param name="options">HTTP 传输配置。</param>
    /// <param name="utcNow">UTC 时钟（测试注入用；缺省 <see cref="DateTimeOffset.UtcNow"/>）。</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为空。</exception>
    public FeishuMcpSessionDirectory(FeishuMcpHttpOptions options, Func<DateTimeOffset>? utcNow = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _utcNow = utcNow ?? DefaultClock;
    }

    /// <summary>当前驻留会话数（含已过期未回收者；观测面）。</summary>
    internal int Count
    {
        get
        {
            lock (_gate)
            {
                return _sessions.Count;
            }
        }
    }

    /// <summary>新建一条会话并绑定租户。</summary>
    /// <param name="appKey">宿主认证层给出的租户标识。</param>
    /// <param name="userId">宿主认证层给出的用户标识（可空）。</param>
    /// <param name="protocolVersion">握手协商出的协议版本。</param>
    /// <returns>会话；<see langword="null"/> = 已达 <see cref="FeishuMcpHttpOptions.MaxSessions"/> 上界。</returns>
    public FeishuMcpSession? Create(string appKey, string? userId, string protocolVersion)
    {
        lock (_gate)
        {
            SweepExpired();

            if (_sessions.Count >= _options.MaxSessions)
            {
                return null;
            }

            // 标识必须不可猜：它等价于「这条会话属于哪个租户」的凭据。
            var id = Guid.NewGuid().ToString("N");
            var session = new FeishuMcpSession(id, appKey, userId, protocolVersion, _utcNow());
            _sessions[id] = session;
            return session;
        }
    }

    /// <summary>按标识取会话（顺带刷新访问时刻；已空闲过限则回收并返回 <see langword="null"/>）。</summary>
    /// <param name="id">会话标识（可空）。</param>
    /// <returns>会话，或 <see langword="null"/>（不存在 / 已过期）。</returns>
    public FeishuMcpSession? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        lock (_gate)
        {
            if (!_sessions.TryGetValue(id!, out var session))
            {
                return null;
            }

            if (session.IsExpired(_options.SessionIdleTimeout, _utcNow()))
            {
                _sessions.Remove(id!);
                return null;
            }

            session.Touch(_utcNow());
            return session;
        }
    }

    /// <summary>删除会话（<c>DELETE</c> 终止与过期回收共用）。</summary>
    /// <param name="id">会话标识。</param>
    /// <returns>是否确实删除了一条会话。</returns>
    public bool Remove(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        lock (_gate)
        {
            return _sessions.Remove(id!);
        }
    }

    private void SweepExpired()
    {
        var now = _utcNow();
        if (_sessions.Count == 0)
        {
            return;
        }

        // 遍历中删除需先物化键：Dictionary 不允许在枚举时修改。
        List<string>? expired = null;
        foreach (var pair in _sessions)
        {
            if (pair.Value.IsExpired(_options.SessionIdleTimeout, now))
            {
                (expired ??= []).Add(pair.Key);
            }
        }

        if (expired is null)
        {
            return;
        }

        foreach (var key in expired)
        {
            _sessions.Remove(key);
        }
    }
}
