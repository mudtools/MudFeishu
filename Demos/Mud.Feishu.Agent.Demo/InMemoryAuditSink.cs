// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo;

/// <summary>审计计数快照（<c>/usage</c> 展示用）。</summary>
/// <param name="Allowed">放行次数。</param>
/// <param name="Denied">拒绝次数。</param>
/// <param name="Error">错误次数。</param>
internal readonly record struct AuditCounts(int Allowed, int Denied, int Error)
{
    /// <summary>总调用数。</summary>
    public int Total => Allowed + Denied + Error;
}

/// <summary>
/// 执行审计（闸 3）：<c>IToolExecutionAuditSink</c> 的内存实现 + JSONL 导出。
/// </summary>
/// <remarks>
/// <para>
/// <b>脱敏责任在 SDK 侧</b>：<see cref="ToolExecutionAuditRecord.ArgsDigest"/> 已由 SDK 生成
/// （参数名 + 值长度 + 敏感键名掩码）。本实现<b>不接触原始参数</b>，也不从别处捞原始参数打印，
/// 否则审计通道会变成敏感数据泄漏面。
/// </para>
/// <para>
/// <b>投递异常隔离</b>：SDK 侧已隔离，本实现同样不抛——入队是纯内存操作。
/// </para>
/// <para>
/// <b>工具调用可视化的数据源</b>：本 sink 是唯一能看到"每次工具执行结果"的地方
/// （工具在 SDK 执行链内部运行），故工具卡片（工具名 / 风险徽标 / 判定 / 耗时）在此渲染，
/// 不改动 SDK 一行代码。
/// </para>
/// </remarks>
internal sealed class InMemoryAuditSink : IToolExecutionAuditSink
{
    private readonly ConcurrentQueue<ToolExecutionAuditRecord> _records = new();
    private readonly ConsoleRenderer _renderer;
    private int _seq;

    /// <summary>
    /// 初始化审计 sink。
    /// </summary>
    /// <param name="renderer">终端渲染器（工具卡片输出）。</param>
    /// <remarks>
    /// <b>刻意不注入 <c>FeishuToolRegistry</c></b>：本 sink 由 <c>FeishuToolBinding</c> 可选注入，
    /// 而注册表的构造又会解析 Binding——反向依赖会形成运行期循环依赖（启动期无输出卡死）。
    /// 风险事实改从编译期契约表读取（见 <see cref="DocAgentToolFacts"/>）。
    /// </remarks>
    public InMemoryAuditSink(ConsoleRenderer renderer)
    {
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
    }

    /// <summary>已记录条数。</summary>
    public int Count => _records.Count;

    /// <inheritdoc />
    public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
    {
        _records.Enqueue(record);

        _renderer.ToolCallCard(new ToolCallViewModel(
            ToolName: record.ToolName,
            RiskLiteral: DocAgentToolFacts.RiskLiteralOf(record.ToolName, record.IsWrite),
            Decision: record.Decision,
            DurationMs: record.DurationMs,
            ArgsDigest: record.ArgsDigest,
            Reason: record.Reason));

        _renderer.Trace(
            $"审计 #{Interlocked.Increment(ref _seq)} {record.ToolName} → {record.Decision}"
            + $"（{record.DurationMs}ms, write={record.IsWrite}）");

        return Task.CompletedTask;
    }

    /// <summary>按写入顺序取全部记录（不拷贝记录本身，记录是不可变 record）。</summary>
    /// <returns>记录数组。</returns>
    public IReadOnlyList<ToolExecutionAuditRecord> Snapshot() => _records.ToArray();

    /// <summary>最近 <paramref name="count"/> 条（倒序：最近的在前）。</summary>
    /// <param name="count">条数。</param>
    /// <returns>记录数组。</returns>
    public IReadOnlyList<ToolExecutionAuditRecord> Recent(int count)
        => _records.Reverse().Take(count).ToArray();

    /// <summary>按判定分组计数。</summary>
    /// <returns>计数快照。</returns>
    public AuditCounts CountByDecision()
    {
        int allowed = 0, denied = 0, error = 0;
        foreach (var record in _records)
        {
            switch (record.Decision)
            {
                case "allowed":
                    allowed++;
                    break;
                case "denied":
                    denied++;
                    break;
                default:
                    error++;
                    break;
            }
        }

        return new AuditCounts(allowed, denied, error);
    }

    /// <summary>
    /// 全量导出为 JSONL（逐行一个 JSON 对象；UTF-8 无 BOM）。
    /// </summary>
    /// <param name="path">目标路径（空 = <c>%TEMP%</c> 下的时间戳文件名）。</param>
    /// <returns>实际落盘路径。</returns>
    public string ExportJsonl(string? path)
    {
        var target = string.IsNullOrWhiteSpace(path) ? DefaultExportPath() : path!;
        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var lines = new StringBuilder();
        foreach (var record in _records)
        {
            // 具体类型（非反射泛型）重载：Demo 不在 AOT 门禁面内，但仍保持零告警的习惯。
            lines.Append(JsonSerializer.Serialize(record)).Append('\n');
        }

        File.WriteAllText(target, lines.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return target;
    }

    /// <summary>默认导出路径（<c>%TEMP%/mud-feishu-docagent-audit-{yyyyMMdd-HHmmss}.jsonl</c>）。</summary>
    /// <returns>绝对路径。</returns>
    public static string DefaultExportPath()
        => Path.Combine(
            Path.GetTempPath(),
            "mud-feishu-docagent-audit-"
            + DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
            + ".jsonl");
}
