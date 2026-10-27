// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
// -----------------------------------------------------------------------

using Mud.Feishu.AI.CapabilityDemo.Common;

namespace Mud.Feishu.AI.CapabilityDemo.Host;

/// <summary>执行审计（闸 3）：内存队列 + 可选工具卡片 + JSONL 导出。</summary>
/// <remarks>
/// <see cref="ToolExecutionAuditRecord.ArgsDigest"/> 已由 SDK 脱敏；本类不接触原始参数，
/// 也不从别处捞原文打印——审计出口若要原文，就变成了新的泄漏面。
/// </remarks>
internal sealed class InMemoryAuditSink(bool printCards = true) : IToolExecutionAuditSink
{
    private readonly ConcurrentQueue<ToolExecutionAuditRecord> _records = new();

    public int Count => _records.Count;

    public Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default)
    {
        _records.Enqueue(record);

        if (printCards)
        {
            DemoConsole.ToolCard(
                record.ToolName, record.Risk, record.Decision, record.DurationMs, record.ArgsDigest, record.Reason);
        }

        return Task.CompletedTask;
    }

    public IReadOnlyList<ToolExecutionAuditRecord> Snapshot() => _records.ToArray();

    public (int Allowed, int Denied, int Error) CountByDecision()
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

        return (allowed, denied, error);
    }

    public string ExportJsonl(string? path)
    {
        var target = string.IsNullOrWhiteSpace(path)
            ? Path.Combine(
                Path.GetTempPath(),
                $"cap-demo-audit-{DateTime.Now:yyyyMMdd-HHmmss}.jsonl")
            : path!;

        var directory = Path.GetDirectoryName(target);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new StringBuilder();
        foreach (var record in _records)
        {
            builder.Append(JsonSerializer.Serialize(record)).Append('\n');
        }

        File.WriteAllText(target, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return target;
    }
}
