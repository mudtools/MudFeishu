// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Demo.Sdk;

namespace ToolSurfaceDemo.Infrastructure;

/// <summary>
/// 演示用内存 SDK 实现：替代真实传输层，让 demo 无网络依赖即可跑通「模型调用 → 执行器 → SDK」全链路。
/// 生产环境中这一层由 Mud.HttpUtils 的 HTTP 客户端生成器按 <c>IMudDemoTenantV1*</c> 接口生成。
/// </summary>
public sealed class InMemoryDocSdk : IMudDemoTenantV1Doc, IMudDemoTenantV1DocTrash
{
    private readonly Dictionary<string, DemoDocument> _documents = new(StringComparer.Ordinal)
    {
        ["doc-001"] = new("doc-001", "接入指南", "docs/2026"),
        ["doc-002"] = new("doc-002", "API 参考", "docs/2026"),
        ["doc-003"] = new("doc-003", "会议纪要", "docs/archive"),
    };

    /// <inheritdoc />
    public Task<IReadOnlyList<DemoDocument>> ListAsync(string? folderId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<DemoDocument> result = _documents.Values
            .Where(document => folderId is null || document.FolderId == folderId)
            .ToArray();
        return Task.FromResult(result);
    }

    /// <inheritdoc />
    public Task<DemoDocument> DeleteAsync(string docId, CancellationToken cancellationToken = default)
    {
        if (!_documents.Remove(docId, out var removed))
        {
            throw new KeyNotFoundException($"文档 '{docId}' 不存在。");
        }

        return Task.FromResult(removed);
    }
}
