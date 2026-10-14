// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Serialization;
using Mud.HttpUtils.Attributes;

namespace Mud.Demo.Sdk;

/// <summary>
/// 演示用文档模型。
/// </summary>
/// <remarks>
/// 引擎推导 <c>output_schema</c> 时<b>只展开带 <see cref="JsonPropertyNameAttribute"/> 的公共属性</b>
/// （与 System.Text.Json 序列化契约对齐）——裸属性不会出现在模型可见的返回值 Schema 里。
/// </remarks>
public sealed record DemoDocument(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("folder_id")] string FolderId);

/// <summary>
/// 文档域 SDK 接口（租户令牌）。
/// </summary>
/// <remarks>
/// 接口名 <c>IMudDemoTenantV1Doc</c> 是工具面引擎的编译期契约样本：
/// <list type="bullet">
/// <item>命中剖面 <c>InterfaceNameRegex</c>（<c>^IMudDemo(?:Tenant|User)?V[0-9]+(?&lt;domain&gt;[A-Za-z]+)$</c>），domain 段 = <c>Doc</c>；</item>
/// <item>命中剖面 <c>TokenKindMarkers</c>（<c>IMudDemoTenant=Tenant</c>，前缀形态），令牌身份 = <c>tenant</c>。</item>
/// </list>
/// 方法目录（MethodCatalog）从 [Get] / [Delete] / [Path] 特性提取 HTTP 方法、路由与路径参数。
/// </remarks>
public interface IMudDemoTenantV1Doc
{
    /// <summary>列出文档。</summary>
    /// <param name="folderId">文件夹 ID（可选过滤）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [Get("/doc/v1/list")]
    Task<IReadOnlyList<DemoDocument>> ListAsync([Query] string? folderId, CancellationToken cancellationToken = default);
}

/// <summary>文档回收站域 SDK 接口（租户令牌）；domain 段 = <c>DocTrash</c>。</summary>
public interface IMudDemoTenantV1DocTrash
{
    /// <summary>删除文档（写入面：DELETE + 危险词命中 → risk = high-risk-write）。</summary>
    /// <param name="docId">文档 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    [Delete("/doc/v1/documents/{docId}")]
    Task<DemoDocument> DeleteAsync([Path] string docId, CancellationToken cancellationToken = default);
}
