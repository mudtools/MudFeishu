// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>MCP 协议层的 JSON 小工具（AOT 安全）。</summary>
/// <remarks>
/// 本包不能复用 <c>Mud.Feishu.AI.Tools</c> 的 <c>ToolResultText.AddNode</c>（internal），
/// 故在此保留同款实现：走 <c>IList{JsonNode?}</c> 显式接口实现，绕过带
/// <c>RequiresDynamicCode</c> 注解的泛型 <c>JsonArray.Add&lt;T&gt;(T)</c>
/// （T 为非原生 JsonNode 子类时在 net8+ 会产 IL2026/IL3050，破坏 AOT 严格门禁）。
/// </remarks>
internal static class McpJson
{
    /// <summary>AOT 安全的 <see cref="JsonArray"/> 追加。</summary>
    /// <param name="array">目标数组。</param>
    /// <param name="node">要追加的节点（可为 null）。</param>
    public static void AddNode(this JsonArray array, JsonNode? node)
        => ((IList<JsonNode?>)array).Add(node);
}
