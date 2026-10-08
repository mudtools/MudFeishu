// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Internal;
using Mud.Feishu.AI.Tools;
using Mud.Feishu.DataModels.Messages;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// WP7（AT-F07 上传部分 / AT-F08）附件上传工具的<b>四步链路</b>用例：
/// 落盘 → 上传（断言 <c>[FilePath]</c> 实参指向真实存在的文件）→ 发送（断言 key 进了 content）→ 清理。
/// </summary>
/// <remarks>
/// <para>
/// 验收标准沿用 R3/R4：<b>"工具存在"不算通过</b>——每一步都必须抓取 SDK 实参，且
/// <b>临时文件的生命周期</b>（执行期内存在、执行后消失）必须被断言。
/// </para>
/// <para>
/// 落盘器是宿主实现（U-5：SDK 只定契约），故此处用替身：它写出真实临时文件并记录清理调用，
/// 从而把"执行链是否在 finally 中清理"变成可断言事实。
/// </para>
/// </remarks>
public class AttachmentToolsChainTests
{
    private readonly Mock<Mud.Feishu.IFeishuTenantV1Message> _messageClient = new();

    private static IReadOnlyDictionary<string, object?> Args(params (string Key, object? Value)[] items)
        => items.ToDictionary(p => p.Key, p => p.Value);

    /// <summary>宿主落盘器替身：写出真实临时文件，并在 Cleanup 时删除它。</summary>
    private sealed class FakeStager : IFeishuAttachmentStager
    {
        private readonly bool _allow;
        private readonly string _content;

        public FakeStager(bool allow = true, string content = "binary")
        {
            _allow = allow;
            _content = content;
        }

        public int StageCalls { get; private set; }

        public int CleanupCalls { get; private set; }

        public string? LastUrl { get; private set; }

        public string? LastFileName { get; private set; }

        public async Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken)
        {
            StageCalls++;
            LastUrl = source.Url;
            LastFileName = source.FileName;
            if (!_allow)
            {
                return null;
            }

            var path = Path.Combine(Path.GetTempPath(), $"mud-feishu-test-{Guid.NewGuid():N}.bin");
            await File.WriteAllTextAsync(path, _content, cancellationToken).ConfigureAwait(false);

            return new StagedAttachment(
                path,
                _content.Length,
                "application/octet-stream",
                () =>
                {
                    CleanupCalls++;
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }

                    return ValueTask.CompletedTask;
                });
        }
    }

    // ───────────────────── ① 落盘 → 上传 → 发送 → 清理 ─────────────────────

    [Fact]
    public async Task SendImage_ShouldStageUploadAndSend_ThenCleanUp()
    {
        var stager = new FakeStager();
        string? stagedPath = null;
        var fileExistedAtUpload = false;
        UploadImageRequest? uploadRequest = null;
        SendMessageRequest? sendRequest = null;

        _messageClient
            .Setup(c => c.UploadImageAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((UploadImageRequest request, CancellationToken _) =>
            {
                uploadRequest = request;
                stagedPath = request.FilePath;
                fileExistedAtUpload = request.FilePath is not null && File.Exists(request.FilePath);
            })
            .ReturnsAsync(new FeishuApiResult<ImageUpdateResult>
            {
                Code = 0,
                Data = new ImageUpdateResult { ImageKey = "img_key_1" },
            });

        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string _, CancellationToken __) => sendRequest = request)
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_img_1" },
            });

        var tools = new AttachmentTools(_messageClient.Object, stager);
        var result = await tools.SendImageAsync(
            Args(
                ("receive_id", "oc_group1"),
                ("image_url", "https://example.com/a.png"),
                ("file_name", "a.png"),
                ("receive_id_type", "chat_id")),
            CancellationToken.None);

        result.ToString().Should().Contain("om_img_1");

        stager.StageCalls.Should().Be(1, "URL 必须先经宿主落盘（SDK 不实现下载）");
        stager.LastUrl.Should().Be("https://example.com/a.png");
        stager.LastFileName.Should().Be("a.png");

        uploadRequest!.ImageType.Should().Be("message", "发送消息用的图片必须是 image_type=message");
        uploadRequest.FilePath.Should().Be(stagedPath);
        fileExistedAtUpload.Should().BeTrue("上传时刻文件必须真实存在（[FilePath] 是本地绝对路径）");

        sendRequest!.MsgType.Should().Be("image");
        sendRequest.ReceiveId.Should().Be("oc_group1");
        sendRequest.Content.Should().Contain("img_key_1", "上传返回的 image_key 必须进消息 content");

        stager.CleanupCalls.Should().Be(1, "临时文件必须在 finally 中被清理（不进 GC/finalizer）");
        File.Exists(stagedPath).Should().BeFalse("执行完毕后临时文件必须消失");
    }

    [Fact]
    public async Task SendFile_ShouldStageUploadAndSend_ThenCleanUp()
    {
        var stager = new FakeStager();
        string? stagedPath = null;
        UploadMessageFileRequest? uploadRequest = null;
        SendMessageRequest? sendRequest = null;

        _messageClient
            .Setup(c => c.UploadFileAsync(It.IsAny<UploadMessageFileRequest>(), It.IsAny<CancellationToken>()))
            .Callback((UploadMessageFileRequest request, CancellationToken _) =>
            {
                uploadRequest = request;
                stagedPath = request.FilePath;
            })
            .ReturnsAsync(new FeishuApiResult<FileUploadResult>
            {
                Code = 0,
                Data = new FileUploadResult { FileKey = "file_key_1" },
            });

        _messageClient
            .Setup(c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback((SendMessageRequest request, string _, CancellationToken __) => sendRequest = request)
            .ReturnsAsync(new FeishuApiResult<MessageDataResult>
            {
                Code = 0,
                Data = new MessageDataResult { MessageId = "om_file_1" },
            });

        var tools = new AttachmentTools(_messageClient.Object, stager);
        var result = await tools.SendFileAsync(
            Args(
                ("receive_id", "ou_user1"),
                ("file_url", "https://example.com/report.pdf"),
                ("file_name", "周报.pdf"),
                ("receive_id_type", "open_id")),
            CancellationToken.None);

        result.ToString().Should().Contain("om_file_1");
        uploadRequest!.FileType.Should().Be("stream", "非枚举内格式统一用 stream（官方口径）");
        uploadRequest.FileName.Should().Be("周报.pdf");
        sendRequest!.MsgType.Should().Be("file");
        sendRequest.Content.Should().Contain("file_key_1");
        stager.CleanupCalls.Should().Be(1);
        File.Exists(stagedPath).Should().BeFalse();
    }

    // ───────────────────── ② 参数非法（本地路径 / 非 http 协议） ─────────────────────

    [Theory]
    [InlineData(@"C:\temp\a.png")]
    [InlineData("file:///tmp/a.png")]
    [InlineData("ftp://example.com/a.png")]
    [InlineData("不是地址")]
    public async Task SendImage_WithNonHttpUrl_ShouldReturnStructuredError_WithoutStaging(string url)
    {
        var stager = new FakeStager();
        var tools = new AttachmentTools(_messageClient.Object, stager);

        var result = await tools.SendImageAsync(
            Args(("receive_id", "oc_group1"), ("image_url", url)),
            CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] im.send_image");
        result.ToString().Should().Contain("http/https");
        stager.StageCalls.Should().Be(0, "非法来源必须在落盘之前拦截（不得诱导宿主读任意本地文件）");
        _messageClient.Verify(
            c => c.UploadImageAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ───────────────────── ③ 宿主安全域拒绝 → invalid_args ─────────────────────

    [Fact]
    public async Task SendImage_WhenHostRejectsSource_ShouldReturnStructuredError_WithoutUploading()
    {
        var stager = new FakeStager(allow: false);
        var tools = new AttachmentTools(_messageClient.Object, stager);

        var result = await tools.SendImageAsync(
            Args(("receive_id", "oc_group1"), ("image_url", "https://internal.corp/secret.png")),
            CancellationToken.None);

        result.ToString().Should().StartWith("[tool_error] im.send_image");
        result.ToString().Should().Contain("不被宿主允许", "安全域由宿主实现，SDK 只如实回填（不假装能校验）");
        stager.CleanupCalls.Should().Be(0, "未落盘则无清理义务");
        _messageClient.Verify(
            c => c.UploadImageAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "来源被拒时不得上传（否则等于绕过了宿主的域名白名单）");
    }

    // ───────────────────── ④ dry_run：不落盘、不上传、不发送 ─────────────────────

    [Fact]
    public async Task SendImage_DryRun_ShouldNeitherStageNorCallDownstream()
    {
        var stager = new FakeStager();
        var tools = new AttachmentTools(_messageClient.Object, stager);

        var result = await tools.SendImageAsync(
            Args(
                ("receive_id", "oc_group1"),
                ("image_url", "https://example.com/a.png"),
                ("dry_run", true)),
            CancellationToken.None);

        var text = result.ToString()!;
        text.Should().Contain("[dry_run]");
        text.Should().Contain("POST /open-apis/im/v1/images");
        text.Should().NotContain("https://example.com/a.png", "摘要不回原文（与写工具同一纪律）");
        stager.StageCalls.Should().Be(0, "预演不落盘（否则会白占磁盘）");
    }

    // ───────────────────── ⑤ 上传失败：短路且仍然清理 ─────────────────────

    [Fact]
    public async Task SendImage_WhenUploadFails_ShouldReturnError_AndStillCleanUp()
    {
        var stager = new FakeStager();
        string? stagedPath = null;

        _messageClient
            .Setup(c => c.UploadImageAsync(It.IsAny<UploadImageRequest>(), It.IsAny<CancellationToken>()))
            .Callback((UploadImageRequest request, CancellationToken _) => stagedPath = request.FilePath)
            .ReturnsAsync(new FeishuApiResult<ImageUpdateResult> { Code = 234001, Msg = "image too large" });

        var tools = new AttachmentTools(_messageClient.Object, stager);
        var result = await tools.SendImageAsync(
            Args(("receive_id", "oc_group1"), ("image_url", "https://example.com/a.png")),
            CancellationToken.None);

        result.ToString().Should().Contain("234001", "上传失败必须以飞书 code 回填（模型据此换图片）");
        _messageClient.Verify(
            c => c.SendMessageAsync(It.IsAny<SendMessageRequest>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "上传失败不得继续发送");
        stager.CleanupCalls.Should().Be(1, "失败路径同样必须清理（finally 覆盖成功与失败）");
        File.Exists(stagedPath).Should().BeFalse();
    }

    // ───────────────────── ⑥ 软缺席：落盘器缺席 → 工具不注册 ─────────────────────

    [Fact]
    public void WithoutAttachmentStager_ShouldNotRegisterAttachmentTools()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { }, withAttachmentStager: false);

        var registry = provider.GetRequiredService<FeishuToolRegistry>();
        var names = registry.AllTools.Select(static t => t.Name).ToArray();

        names.Should().NotContain(FeishuToolNames.ImSendImage, "落盘器缺席 → 上传工具不暴露（软缺席，非运行期报错）");
        names.Should().NotContain(FeishuToolNames.ImSendFile);
        names.Should().Contain(FeishuToolNames.ImSendMessage, "同域其它工具不受影响（缺席粒度到工具）");
    }

    [Fact]
    public void WithAttachmentStager_ShouldRegisterAttachmentTools()
    {
        using var provider = GuardProviderFactory.CreateProvider(_ => { });
        var registry = provider.GetRequiredService<FeishuToolRegistry>();

        registry.AllTools.Select(static t => t.Name)
            .Should().Contain([FeishuToolNames.ImSendImage, FeishuToolNames.ImSendFile]);
        registry.EnabledTools.Should().BeEmpty("两个上传工具同样默认不启用（写工具需 WriteAllowList + 授权门禁）");
    }
}
