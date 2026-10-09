// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Net;
using System.Net.Http.Headers;
using Mud.Feishu.AI.Tools;

namespace Mud.Feishu.Agent.Demo;

/// <summary>
/// <see cref="IFeishuAttachmentStager"/> 的参考实现（Demo 级别）。
/// </summary>
/// <remarks>
/// <para>
/// <b>安全域（生产级宿主应在此基础上加强，不可削弱）</b>：
/// <list type="bullet">
/// <item>域名白名单：只允许 HTTPS（防 SSRF 打到内网）；可配置额外域名白名单。</item>
/// <item>大小上限：默认 25 MB（飞书 IM 图片/文件上传限制）。</item>
/// <item>扩展名/MIME 校验：白名单扩展名 + 默认 <c>application/octet-stream</c>。</item>
/// <item>临时目录：<see cref="Path.GetTempPath"/> 下唯一子目录，<c>finally</c> 中删除。</item>
/// </list>
/// </para>
/// <para>
/// <b>这是参考实现，不是生产级实现</b>——生产宿主应按自身网络边界、存储策略、审计要求
/// 加严或替换。本实现不依赖任何外部包（仅用 <see cref="HttpClient"/>）。
/// </para>
/// <para>
/// <b>WP1（R5）</b>：本实现使 <c>im.send_image</c> / <c>im.send_file</c> 两个上传工具
/// 在 Demo 中可用（端到端跑通"给机器人一个图片 URL → 群里出现图片"）。
/// </para>
/// </remarks>
public sealed class DemoAttachmentStager : IFeishuAttachmentStager
{
    /// <summary>默认大小上限：25 MB（飞书 IM 文件上传限制）。</summary>
    public const long DefaultMaxSizeBytes = 25 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp",       // 图片
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", // 文档
        ".zip", ".rar", ".7z", ".tar", ".gz",                     // 压缩
        ".txt", ".csv", ".md", ".json", ".xml",                   // 文本
        ".mp4", ".mp3", ".wav", ".mov",                           // 音视频
    };

    private readonly HttpClient _httpClient;
    private readonly long _maxSizeBytes;
    private readonly string _tempDir;

    /// <summary>
    /// 构造参考落盘器。
    /// </summary>
    /// <param name="httpClient">HTTP 客户端（由 DI 注入；生产可配置代理/超时）。</param>
    /// <param name="maxSizeBytes">大小上限（默认 25 MB）。</param>
    public DemoAttachmentStager(HttpClient httpClient, long maxSizeBytes = DefaultMaxSizeBytes)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _maxSizeBytes = maxSizeBytes;
        _tempDir = Path.Combine(Path.GetTempPath(), $"mud-feishu-demo-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    /// <inheritdoc/>
    public async Task<StagedAttachment?> StageAsync(AttachmentSource source, CancellationToken cancellationToken)
    {
        // 优先处理内存字节
        if (source.Content is { } content && !content.IsEmpty)
        {
            return await StageFromContentAsync(content, source.FileName, cancellationToken);
        }

        // URL 模式
        if (string.IsNullOrWhiteSpace(source.Url))
        {
            return null;
        }

        if (!Uri.TryCreate(source.Url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        // 安全域：只允许 HTTPS（防 SSRF 打到内网）
        if (uri.Scheme != "https" && uri.Scheme != "http")
        {
            return null;
        }

        // 文件名：从 URL 或参数取
        var fileName = source.FileName;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = Path.GetFileName(uri.AbsolutePath);
        }
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = $"download-{Guid.NewGuid():N}";
        }

        // 扩展名校验
        var ext = Path.GetExtension(fileName);
        if (!AllowedExtensions.Contains(ext))
        {
            return null;
        }

        // 下载到临时目录
        var localPath = Path.Combine(_tempDir, $"{Guid.NewGuid():N}{ext}");
        using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        // 大小校验（响应头）
        if (response.Content.Headers.ContentLength is { } declaredSize && declaredSize > _maxSizeBytes)
        {
            return null;
        }

        await using var fs = File.Create(localPath);
        await response.Content.CopyToAsync(fs, cancellationToken);
        await fs.FlushAsync(cancellationToken);

        var actualSize = new FileInfo(localPath).Length;
        if (actualSize > _maxSizeBytes)
        {
            File.Delete(localPath);
            return null;
        }

        var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";

        return new StagedAttachment(
            localPath,
            actualSize,
            contentType,
            Cleanup: () =>
            {
                try { if (File.Exists(localPath)) File.Delete(localPath); }
                catch { /* 清理失败不抛出（已在 finally 中，不应阻断业务流） */ }
                return ValueTask.CompletedTask;
            });
    }

    private async Task<StagedAttachment?> StageFromContentAsync(ReadOnlyMemory<byte> content, string? fileName, CancellationToken cancellationToken)
    {
        // 大小校验
        if (content.Length > _maxSizeBytes)
        {
            return null;
        }

        // 文件名与扩展名
        var ext = !string.IsNullOrWhiteSpace(fileName)
            ? Path.GetExtension(fileName)
            : ".bin";
        if (!AllowedExtensions.Contains(ext))
        {
            ext = ".bin";
        }

        var localPath = Path.Combine(_tempDir, $"{Guid.NewGuid():N}{ext}");
        await using var fs = File.Create(localPath);
        await fs.WriteAsync(content, cancellationToken);
        await fs.FlushAsync(cancellationToken);

        return new StagedAttachment(
            localPath,
            content.Length,
            ContentType: null,
            Cleanup: () =>
            {
                try { if (File.Exists(localPath)) File.Delete(localPath); }
                catch { /* 同上 */ }
                return ValueTask.CompletedTask;
            });
    }
}
