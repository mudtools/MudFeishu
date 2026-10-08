// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// <see cref="InMemoryAuditSink"/>：审计三态、工具卡片渲染、JSONL 导出与"导出失败不影响写入"。
/// </summary>
public class InMemoryAuditSinkTests
{
    /// <summary>写入必须不抛、按序入队、并渲染工具卡片。</summary>
    [Fact]
    public async Task WriteAsync_Should_EnqueueAndRenderCard()
    {
        var (renderer, output) = TestDoubles.CreateRenderer();
        var sink = new InMemoryAuditSink(renderer);

        await sink.WriteAsync(TestDoubles.CreateAuditRecord(decision: "denied"));
        await sink.WriteAsync(TestDoubles.CreateAuditRecord("docx.get_raw_content", "allowed", isWrite: false));

        sink.Count.Should().Be(2);
        sink.Snapshot()[0].ToolName.Should().Be("docx.delete_blocks");

        var text = output.ToString();
        text.Should().Contain("docx.delete_blocks");
        text.Should().Contain("high-risk-write", "风险徽标取自注册表（不得运行期手写）");
        text.Should().Contain("denied");
        text.Should().Contain("12ms");
    }

    /// <summary>三态计数必须与 <c>Decision</c> 字面量对齐（<c>/usage</c> 消费）。</summary>
    [Fact]
    public async Task CountByDecision_Should_SplitByLiteral()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var sink = new InMemoryAuditSink(renderer);

        await sink.WriteAsync(TestDoubles.CreateAuditRecord(decision: "allowed"));
        await sink.WriteAsync(TestDoubles.CreateAuditRecord(decision: "denied"));
        await sink.WriteAsync(TestDoubles.CreateAuditRecord(decision: "error"));
        await sink.WriteAsync(TestDoubles.CreateAuditRecord(decision: "error"));

        var counts = sink.CountByDecision();

        counts.Allowed.Should().Be(1);
        counts.Denied.Should().Be(1);
        counts.Error.Should().Be(2);
        counts.Total.Should().Be(4);
    }

    /// <summary><c>Recent</c> 必须倒序（<c>/audit</c> 要求最近的在前）。</summary>
    [Fact]
    public async Task Recent_Should_ReturnNewestFirst()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var sink = new InMemoryAuditSink(renderer);

        await sink.WriteAsync(TestDoubles.CreateAuditRecord("tool.one"));
        await sink.WriteAsync(TestDoubles.CreateAuditRecord("tool.two"));

        var recent = sink.Recent(20);

        recent.Should().HaveCount(2);
        recent[0].ToolName.Should().Be("tool.two");
    }

    /// <summary>JSONL 导出：行数 = 记录数，且不含原始参数（只有 SDK 脱敏后的摘要）。</summary>
    [Fact]
    public async Task ExportJsonl_Should_WriteOneLinePerRecord()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var sink = new InMemoryAuditSink(renderer);
        var path = Path.Combine(Path.GetTempPath(), $"mud-docagent-test-{Guid.NewGuid():N}.jsonl");

        try
        {
            await sink.WriteAsync(TestDoubles.CreateAuditRecord());
            await sink.WriteAsync(TestDoubles.CreateAuditRecord(decision: "allowed"));

            var written = sink.ExportJsonl(path);

            written.Should().Be(path);
            var lines = File.ReadAllLines(path);
            lines.Should().HaveCount(2);
            lines[0].Should().Contain("docx.delete_blocks");
            lines[0].Should().Contain("ArgsDigest", "导出的是 SDK 脱敏后的摘要字段，不是原始参数");
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    /// <summary>
    /// <b>导出失败不得影响写入</b>：导出走独立的 I/O 路径，写入是纯内存入队。
    /// </summary>
    /// <remarks>
    /// 用"把目录路径当文件路径"制造确定性的 I/O 失败（Windows/Linux 均抛 <c>UnauthorizedAccessException</c>），
    /// 不依赖文件锁等易抖动的环境条件。
    /// </remarks>
    [Fact]
    public async Task WriteAsync_Should_NotThrow_On_Exporter()
    {
        var (renderer, _) = TestDoubles.CreateRenderer();
        var sink = new InMemoryAuditSink(renderer);

        await sink.WriteAsync(TestDoubles.CreateAuditRecord());

        // 目录路径 → File.WriteAllText 必然失败（导出异常面）。
        var export = () => sink.ExportJsonl(Path.GetTempPath());
        export.Should().Throw<Exception>();

        // 导出失败后仍可继续写入，且计数正确——写入路径与导出路径完全解耦。
        await sink.WriteAsync(TestDoubles.CreateAuditRecord(decision: "allowed"));
        sink.Count.Should().Be(2);
        sink.CountByDecision().Allowed.Should().Be(1);
    }

    /// <summary>输出通道（终端/管道）失效时写入不得抛出——渲染是展示面，不得影响执行链。</summary>
    [Fact]
    public async Task WriteAsync_Should_NotThrow_When_OutputWriterFails()
    {
        var renderer = new ConsoleRenderer(new ThrowingWriter(), new ThrowingWriter());
        var sink = new InMemoryAuditSink(renderer);

        var act = async () => await sink.WriteAsync(TestDoubles.CreateAuditRecord());

        await act.Should().NotThrowAsync();
        sink.Count.Should().Be(1);
    }

    /// <summary>写入即抛的 <see cref="TextWriter"/>（模拟终端被关闭/管道断裂）。</summary>
    private sealed class ThrowingWriter : TextWriter
    {
        public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

        public override void Write(string? value) => throw new IOException("模拟终端不可用");
    }
}
