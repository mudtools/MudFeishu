// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Mud.Feishu.AI.Tools.Events;

/// <summary>
/// 外桥事件信封（R7 / C7）：<b>传输层原始载荷</b> + 路由元信息。
/// </summary>
/// <param name="EventKey">事件类型键（如 <c>im.message.receive_v1</c>）。</param>
/// <param name="PayloadJson">
/// <b>原始 JSON 文本</b>（不是已反序列化的对象）。外桥的职责是"把线上看到的原样给出"，
/// 由消费方自行选型解析——这既避免了本仓的 JSON 上下文成为外桥的<b>格式瓶颈</b>，
/// 也避免了"序列化回文本"造成的字段丢失与 AOT 反射序列化破口。
/// </param>
/// <param name="AppKey">事件所属应用（多应用宿主；单应用宿主可空）。</param>
/// <param name="EventId">事件 ID（可空）。</param>
/// <param name="ReceivedAt">接收时刻（可空 ⇒ 由桥接方填入）。</param>
public readonly record struct FeishuEventEnvelope(
    string EventKey,
    string PayloadJson,
    string? AppKey = null,
    string? EventId = null,
    DateTimeOffset? ReceivedAt = null);

/// <summary>事件落地/转发的接收端（外桥的输出侧）。</summary>
public interface IFeishuEventSink
{
    /// <summary>写入一条事件。</summary>
    /// <param name="envelope">事件信封。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    ValueTask WriteAsync(FeishuEventEnvelope envelope, CancellationToken cancellationToken = default);
}

/// <summary>按事件键把 NDJSON 行<b>再</b>路由到文件（官方 CLI 的 <c>regex=dir:./path</c> 同款语义）。</summary>
public interface IFeishuEventFileRouter
{
    /// <summary>
    /// 路由一行 NDJSON。
    /// </summary>
    /// <param name="eventKey">事件类型键（正则匹配的输入）。</param>
    /// <param name="line">已序列化的 NDJSON 行（不含换行）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>是否命中路由（未命中即不落盘）。</returns>
    ValueTask<bool> RouteAsync(string eventKey, string line, CancellationToken cancellationToken = default);
}

/// <summary>
/// NDJSON 外桥（C7）：把飞书事件输出为 <b>NDJSON</b>（一行一个 JSON 对象），供仓外 Agent /
/// 任意进程消费——补上"事件只能进本进程内模型"这一生态缺口。
/// </summary>
/// <remarks>
/// <para>
/// <b>格式</b>：<c>{"event_key":"…","app_key":"…","event_id":"…","received_at":"…","payload":{…}}</c>。
/// <c>payload</c> 是<b>原始文本原样嵌入</b>（不是二次序列化），故本类型零 JSON 序列化依赖、
/// 零 AOT 破口，且字段增删不需要改本仓。
/// </para>
/// <para>
/// <b>与 <c>ConversationalFeishuEventHandler</c> 的互斥（硬要求）</b>：同一事件既进模型又外桥
/// ⇒ 两侧<b>各自</b>产生副作用（发消息 / 落文件），重复消费难以察觉。故本类型只提供"输出端"，
/// <b>不注册任何事件处理器</b>；是否同时启用会话处理由宿主决策并自行保证事件面不重叠
/// （<see cref="EnsureNotConversational"/> 提供机械检查入口）。
/// </para>
/// <para>
/// <b>失败语义</b>：载荷非法 JSON ⇒ <b>抛错</b>（不静默跳过——外桥丢事件是最难发现的故障形态）；
/// 路由落盘失败同样上抛，由宿主决定重试或降级。
/// </para>
/// </remarks>
public sealed class FeishuEventNdjsonBridge : IFeishuEventSink, IDisposable
{
    private readonly TextWriter _output;
    private readonly IFeishuEventFileRouter? _router;
    private readonly ILogger? _logger;
    private readonly bool _ownsWriter;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private long _sequence;
    private bool _disposed;

    /// <summary>
    /// 构造。
    /// </summary>
    /// <param name="output">NDJSON 输出（一般用 <see cref="Console.Out"/>；可空 ⇒ 仅路由不输出）。</param>
    /// <param name="router">文件路由器（可空）。</param>
    /// <param name="logger">日志（可空）。</param>
    /// <param name="ownsWriter">是否由本类释放 <paramref name="output"/>（默认否——不劫持宿主资源生命周期）。</param>
    public FeishuEventNdjsonBridge(
        TextWriter? output = null,
        IFeishuEventFileRouter? router = null,
        ILogger? logger = null,
        bool ownsWriter = false)
    {
        _output = output ?? TextWriter.Null;
        _router = router;
        _logger = logger;
        _ownsWriter = ownsWriter;
    }

    /// <summary>已写出的事件行数（可观测性：外桥是否真的在出数据）。</summary>
    public long WrittenCount => Interlocked.Read(ref _sequence);

    /// <inheritdoc />
    public async ValueTask WriteAsync(FeishuEventEnvelope envelope, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(envelope.EventKey))
        {
            throw new ArgumentException("事件信封缺少 EventKey（外桥无法路由）", nameof(envelope));
        }

        var line = FormatLine(envelope);
        Interlocked.Increment(ref _sequence);

        // 串行化写入：TextWriter 不是线程安全的，而事件通道天然并发。
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_output != TextWriter.Null)
            {
                await _output.WriteLineAsync(line).ConfigureAwait(false);
                await _output.FlushAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }

        if (_router is not null)
        {
            var routed = await _router.RouteAsync(envelope.EventKey, line, cancellationToken).ConfigureAwait(false);
            _logger?.LogDebug(
                "事件已外桥：{EventKey}（路由落盘={Routed}）", envelope.EventKey, routed);
        }
    }

    /// <summary>
    /// 机械互斥检查：宿主若同时注册了会话式事件处理器，必须显式确认事件面不重叠。
    /// </summary>
    /// <remarks>
    /// 刻意做成<b>显式调用</b>而非 DI 自动探测：自动探测需要给所有会话处理器打标记（改既有公开面），
    /// 而"事件面不重叠"本来就是宿主的业务知识。本方法至少把"忘了想这件事"变成一行显式声明。
    /// </remarks>
    /// <param name="conversationalHandlerCount">宿主注册的会话式事件处理器数量。</param>
    /// <exception cref="InvalidOperationException">数量 &gt; 0 时抛出（必须显式调用 <see cref="AllowConcurrentWithConversational"/> 放行）。</exception>
    public static void EnsureNotConversational(int conversationalHandlerCount)
    {
        if (conversationalHandlerCount > 0)
        {
            throw new InvalidOperationException(
                $"宿主同时注册了 {conversationalHandlerCount} 个会话式事件处理器与 NDJSON 外桥——"
                + "同一事件不得既进模型又外桥（双重副作用）。确认事件面互斥后请显式调用 AllowConcurrentWithConversational() 放行");
        }
    }

    /// <summary>显式放行"与会话处理共存"（调用点必须写明互斥依据）。</summary>
    /// <param name="reason">互斥依据（写入日志/审计；空字符串会被拒绝）。</param>
    public static void AllowConcurrentWithConversational(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("放行必须给出互斥依据（否则这行代码就是无理由的副作用开关）", nameof(reason));
        }
    }

    /// <summary>
    /// 格式化为一行 NDJSON（<c>payload</c> 原样嵌入）。
    /// </summary>
    private static string FormatLine(FeishuEventEnvelope envelope)
    {
        if (string.IsNullOrWhiteSpace(envelope.PayloadJson))
        {
            return $"{{\"event_key\":{Quote(envelope.EventKey)},\"payload\":null}}";
        }

        // 先验证是合法 JSON 再原样嵌入：宁可抛错，也不要产出"看起来合法、实际无法解析"的行。
        using (JsonDocument.Parse(envelope.PayloadJson))
        {
        }

        var builder = new System.Text.StringBuilder();
        builder.Append("{\"event_key\":").Append(Quote(envelope.EventKey));

        if (!string.IsNullOrEmpty(envelope.AppKey))
        {
            builder.Append(",\"app_key\":").Append(Quote(envelope.AppKey));
        }

        if (!string.IsNullOrEmpty(envelope.EventId))
        {
            builder.Append(",\"event_id\":").Append(Quote(envelope.EventId));
        }

        builder.Append(",\"received_at\":").Append(
            Quote((envelope.ReceivedAt ?? DateTimeOffset.UtcNow).ToString("O", CultureInfo.InvariantCulture)));

        builder.Append(",\"payload\":").Append(envelope.PayloadJson).Append('}');
        return builder.ToString();
    }

    /// <summary>
    /// 最小 JSON 字符串转义（只需覆盖控制字符、引号与反斜杠）。
    /// </summary>
    /// <remarks>
    /// 不用 <c>JsonSerializer</c>：为一个字符串引入序列化入口既无必要，也会让"本类型零 JSON 依赖"的说法失效。
    /// </remarks>
    private static string Quote(string value)
    {
        var builder = new System.Text.StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (ch < ' ')
                    {
                        builder.Append("\\u").Append(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(ch);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsWriter)
        {
            _output.Dispose();
        }

        _gate.Dispose();
    }
}

/// <summary>
/// 正则路由的 NDJSON 文件路由器（官方 <c>lark-cli event consume --regex 'dir:./path'</c> 的等价物）。
/// </summary>
/// <remarks>
/// <b>文件名规则</b>：<c>{目录}/{事件键清洗}-{Unix 毫秒}-{序号}.ndjson</c>。序号来自进程内单调计数，
/// 因此同一毫秒内的多条事件不会互相覆盖（追加模式下亦不会交错写半行）。
/// </remarks>
public sealed class RegexEventFileRouter : IFeishuEventFileRouter
{
    private readonly string _baseDirectory;
    private readonly IReadOnlyList<KeyValuePair<System.Text.RegularExpressions.Regex, string>> _routes;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private long _sequence;

    /// <summary>
    /// 构造。
    /// </summary>
    /// <param name="baseDirectory">落盘根目录（不存在时按需创建）。</param>
    /// <param name="routes">路由规则（事件键正则 → 目标子目录；按声明顺序取首个命中）。</param>
    public RegexEventFileRouter(string baseDirectory, params (string Pattern, string Directory)[] routes)
    {
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            throw new ArgumentException("落盘根目录不能为空", nameof(baseDirectory));
        }

        _baseDirectory = baseDirectory;

        var parsed = new List<KeyValuePair<System.Text.RegularExpressions.Regex, string>>(routes?.Length ?? 0);
        foreach (var (pattern, directory) in routes ?? [])
        {
            if (string.IsNullOrWhiteSpace(pattern) || string.IsNullOrWhiteSpace(directory))
            {
                throw new ArgumentException("路由规则的正则与目标目录都不能为空", nameof(routes));
            }

            parsed.Add(new KeyValuePair<System.Text.RegularExpressions.Regex, string>(
                new System.Text.RegularExpressions.Regex(
                    pattern,
                    System.Text.RegularExpressions.RegexOptions.CultureInvariant,
                    TimeSpan.FromSeconds(5)),
                directory));
        }

        _routes = parsed;
    }

    /// <inheritdoc />
    public async ValueTask<bool> RouteAsync(string eventKey, string line, CancellationToken cancellationToken = default)
    {
        string? directory = null;
        foreach (var route in _routes)
        {
            if (route.Key.IsMatch(eventKey))
            {
                directory = route.Value;
                break;
            }
        }

        if (directory is null)
        {
            return false;
        }

        var target = System.IO.Path.Combine(_baseDirectory, directory);
        System.IO.Directory.CreateDirectory(target);

        // 每（目录 × 事件键）一个文件：路径稳定 ⇒ 消费方可 tail；追加写入 ⇒ 历史不被截断。
        // （不用「时间戳 + 序号」命名：那会让同一事件的事件散成无数碎片文件，tail 与重放都无从下手。）
        var file = System.IO.Path.Combine(target, Sanitize(eventKey) + ".ndjson");
        Interlocked.Increment(ref _sequence);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // 追加模式 + UTF-8 无 BOM：NDJSON 消费方按行读，追加语义不会截断历史。
            // ⚠️ 不用 File.AppendAllLinesAsync：该 API 在 netstandard2.0 目标上不存在。
            using var stream = new System.IO.FileStream(
                file, System.IO.FileMode.Append, System.IO.FileAccess.Write, System.IO.FileShare.Read);
            using var writer = new StreamWriter(stream, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            await writer.WriteLineAsync(line).ConfigureAwait(false);
            await writer.FlushAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }

        return true;
    }

    private static string Sanitize(string eventKey)
    {
        var builder = new System.Text.StringBuilder(eventKey.Length);
        foreach (var ch in eventKey)
        {
            builder.Append(char.IsLetterOrDigit(ch) || ch is '.' or '_' or '-' ? ch : '_');
        }

        return builder.ToString();
    }
}