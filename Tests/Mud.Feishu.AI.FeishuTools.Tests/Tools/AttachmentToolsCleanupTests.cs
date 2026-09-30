// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Logging;
using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// R2-06：临时附件清理失败必须<b>留痕</b>，且不得改变业务语义（成功仍是成功）。
/// </summary>
/// <remarks>
/// <para>
/// <b>缺陷原始形态</b>：三处 <c>finally</c> 里的 <c>catch (Exception) { }</c> 完全静默，
/// 注释写着"磁盘残留由宿主监控负责"——而宿主<b>没有任何可观测来源</b>。
/// 临时附件是<b>含用户数据的外泄面载体</b>：清理持续失败 ⇒ 磁盘被敏感文件占满且无信号。
/// </para>
/// <para>
/// 本类同时锁住两个方向：① 业务结果<b>不变</b>（清理是宿主实现，其故障不得把一次成功发送改成失败）；
/// ② 必须产出 Warning 日志（可观测性由日志承载，而不是靠"宿主自己发现"）。
/// </para>
/// <para>
/// 日志替身用<b>手写实现</b>而非 <c>Mock&lt;ILogger&lt;AttachmentTools&gt;&gt;</c>：
/// <c>AttachmentTools</c> 是 <c>internal</c>，DynamicProxy 生成的动态程序集不在
/// <c>InternalsVisibleTo</c> 名单内，Moq 无法为闭合到内部类型的泛型接口建代理。
/// </para>
/// </remarks>
public class AttachmentToolsCleanupTests
{
    private const string ImageUrl = "https://cdn.example.com/a.png";

    [Fact]
    public async Task SendImage_ShouldStillSucceed_AndLogWarning_WhenCleanupFails()
    {
        var (tools, logger) = CreateTools(cleanupThrows: true);

        var result = await tools.SendImageAsync(
            Arguments(("image_url", ImageUrl), ("receive_id", "oc_1")), CancellationToken.None);

        result.ToString().Should().Contain("om_sent", "清理失败不得覆盖业务结果（既有判断正确，保持不变）");
        logger.Warnings.Should().ContainSingle(
            "清理失败必须产出恰好一条 Warning（磁盘残留是用户数据驻留面，宿主需要可观测来源）")
            .Which.Should().Contain("临时附件清理失败");
        logger.Warnings[0].Should().Contain("fake-attachment.bin", "留痕必须带残留文件路径（否则宿主无法清理）");
    }

    [Fact]
    public async Task SendFile_ShouldStillSucceed_AndLogWarning_WhenCleanupFails()
    {
        var (tools, logger) = CreateTools(cleanupThrows: true);

        var result = await tools.SendFileAsync(
            Arguments(("file_url", "https://cdn.example.com/a.pdf"), ("file_name", "a.pdf"), ("receive_id", "oc_1")),
            CancellationToken.None);

        result.ToString().Should().Contain("om_sent");
        logger.Warnings.Should().ContainSingle().Which.Should().Contain("临时附件清理失败");
    }

    /// <summary>清理成功时不产生任何日志（防"每条都告警"的噪声化，那会让真故障失去信号价值）。</summary>
    [Fact]
    public async Task SendImage_ShouldNotLog_WhenCleanupSucceeds()
    {
        var (tools, logger) = CreateTools(cleanupThrows: false);

        await tools.SendImageAsync(Arguments(("image_url", ImageUrl), ("receive_id", "oc_1")), CancellationToken.None);

        logger.Entries.Should().BeEmpty("清理成功是正常路径，不应产生任何日志");
    }

    // ────────── 辅助 ──────────

    private static (AttachmentTools Tools, RecordingLogger<AttachmentTools> Logger) CreateTools(bool cleanupThrows)
    {
        var messageClient = new Mock<Mud.Feishu.IFeishuTenantV1Message>();
        messageClient
            .Setup(c => c.UploadImageAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<ImageUpdateResult> { Code = 0, Data = new ImageUpdateResult { ImageKey = "img_1" } });
        messageClient
            .Setup(c => c.UploadFileAsync(It.IsAny<UploadMessageFileRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<FileUploadResult> { Code = 0, Data = new FileUploadResult { FileKey = "file_1" } });
        messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FeishuApiResult<MessageDataResult> { Code = 0, Data = new MessageDataResult { MessageId = "om_sent" } });

        var stager = new Mock<IFeishuAttachmentStager>();
        stager
            .Setup(s => s.StageAsync(It.IsAny<AttachmentSource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new StagedAttachment(
                LocalPath: Path.Combine(Path.GetTempPath(), "fake-attachment.bin"),
                Size: 1024,
                ContentType: "application/octet-stream",
                Cleanup: () => cleanupThrows
                    ? ValueTask.FromException(new IOException("磁盘只读"))
                    : ValueTask.CompletedTask));

        var logger = new RecordingLogger<AttachmentTools>();
        return (new AttachmentTools(messageClient.Object, stager.Object, logger), logger);
    }

    private static IReadOnlyDictionary<string, object?> Arguments(params (string Key, string Value)[] pairs)
        => pairs.ToDictionary(static p => p.Key, static p => (object?)p.Value, StringComparer.Ordinal);
}

/// <summary>把日志落进内存的手写替身（绕开 DynamicProxy 对 internal 类型实参的限制）。</summary>
/// <typeparam name="T">日志类别（本用例为 internal 的执行器类型）。</typeparam>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    /// <summary>已记录的日志条目（级别 / 消息 / 异常）。</summary>
    public List<(LogLevel Level, string Message, Exception? Exception)> Entries { get; } = [];

    /// <summary>已记录的 Warning 级消息。</summary>
    public List<string> Warnings => Entries
        .Where(static e => e.Level == LogLevel.Warning)
        .Select(static e => e.Message)
        .ToList();

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => true;

    /// <inheritdoc />
    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (formatter is null)
        {
            return;
        }

        Entries.Add((logLevel, formatter(state, exception), exception));
    }
}
