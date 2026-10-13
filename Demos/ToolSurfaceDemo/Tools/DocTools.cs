// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;
using Mud.Demo.Sdk;
using Mud.Demo.Tools;

namespace ToolSurfaceDemo.Tools;

/// <summary>
/// 「文档列表」工具承载接口（读工具）。
/// </summary>
/// <remarks>
/// 接口名携带令牌标记 <c>IMudDemoTenant</c>（前缀形态）→ 令牌身份 tenant；
/// 工具名 <c>doc.list</c>；<c>Source</c> 挂钩指向 SDK 接口方法，引擎据此提取 HTTP 事实与输出 Schema。
/// </remarks>
[DemoTool("doc.list",
    Description = "列出文档，可按文件夹过滤",
    Source = "IMudDemoTenantV1Doc.ListAsync")]
public interface IMudDemoTenantV1DocListTool
{
    /// <summary>列出文档，可按文件夹过滤。</summary>
    /// <param name="folderId">文件夹 ID（可选，缺省列出全部）。</param>
    string List([ToolParameter("folderId", "文件夹 ID（可选，缺省列出全部）")] string? folderId);
}

/// <summary>
/// 「删除文档」工具承载接口（写工具：声明 <c>IsWrite = true</c>，SDK 源为 DELETE + 危险词命中 → high-risk-write）。
/// </summary>
[DemoTool("doc.delete",
    Description = "按 ID 删除文档（写入操作）",
    Source = "IMudDemoTenantV1DocTrash.DeleteAsync",
    IsWrite = true)]
public interface IMudDemoTenantV1DocTrashDeleteTool
{
    /// <summary>按 ID 删除文档。</summary>
    /// <param name="docId">文档 ID（必填）。</param>
    string Delete([ToolParameter("docId", "文档 ID（必填）", Required = true)] string docId);
}

/// <summary>
/// 文档域执行器：承载 <c>doc.list</c> / <c>doc.delete</c> 两枚工具的运行时逻辑。
/// </summary>
/// <remarks>
/// <para>
/// 构造器参数全部为 <c>Mud.Demo.Sdk</c> 命名空间的 SDK 接口（不可空）→ 生成器按<b>软缺席</b>装配：
/// 容器内任一 SDK 接口缺席时执行器解析为 null，该域工具整体不进注册表（白名单期 fail-fast）。
/// </para>
/// <para>
/// 执行器方法首部用生成的 <see cref="DocListArgs.Unpack"/> / <see cref="DocDeleteArgs.Unpack"/>
/// 把参数字典解包为强类型——参数改名即此处编译失败（运行期漂移降级为编译期错误）。
/// </para>
/// </remarks>
public sealed class DocTools(IMudDemoTenantV1Doc docSdk, IMudDemoTenantV1DocTrash docTrashSdk)
{
    [DemoToolHandler(typeof(IMudDemoTenantV1DocListTool))]
    public async Task<DemoToolResult> HandleListAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var input = DocListArgs.Unpack(args);
        var documents = await docSdk.ListAsync(input.FolderId, cancellationToken);
        return new DemoToolResult
        {
            Payload = JsonSerializer.Serialize(new
            {
                count = documents.Count,
                documents = documents.Select(static document => new { id = document.Id, title = document.Title, folder = document.FolderId }),
            }),
        };
    }

    [DemoToolHandler(typeof(IMudDemoTenantV1DocTrashDeleteTool))]
    public async Task<DemoToolResult> HandleDeleteAsync(
        IReadOnlyDictionary<string, object?> args,
        CancellationToken cancellationToken)
    {
        var input = DocDeleteArgs.Unpack(args);
        var removed = await docTrashSdk.DeleteAsync(input.DocId, cancellationToken);
        return new DemoToolResult
        {
            Payload = JsonSerializer.Serialize(new { deleted = removed.Id, title = removed.Title }),
        };
    }
}
