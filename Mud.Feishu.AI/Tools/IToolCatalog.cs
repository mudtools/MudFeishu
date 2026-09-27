// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 单个工具的目录条目（稳定目录契约，AI-FD-D12 P1D-4——Phase 4 工具市场/Skills 前置件）。
/// </summary>
/// <param name="Name">工具名（契约表名）。</param>
/// <param name="Description">模型侧描述。</param>
/// <param name="RequiredScopes">所需权限点清单。</param>
/// <param name="IsWrite">是否写操作。</param>
/// <param name="ParameterSchemaJson">参数 JSON Schema（编译期 <c>[FeishuTool]</c> 常量原文）。</param>
public sealed record ToolCatalogEntry(
    string Name,
    string Description,
    IReadOnlyList<string> RequiredScopes,
    bool IsWrite,
    string ParameterSchemaJson);

/// <summary>
/// 工具目录（AI-FD-D12 P1D-4）：注册表内部视图之上的稳定只读契约——枚举与按名查找。
/// </summary>
/// <remarks>
/// 数据源 = 工具注册表（<c>FeishuToolRegistry.AllTools</c> 包装）；目录包含未启用（白名单未 Map）
/// 的工具——启用语义（<see cref="FeishuToolRegistry"/> 白名单）与「能力存在」语义（目录）正交。
/// </remarks>
public interface IToolCatalog
{
    /// <summary>全部已注册工具条目（注册期构建，运行期只读）。</summary>
    IReadOnlyList<ToolCatalogEntry> Entries { get; }

    /// <summary>按工具名查找条目（未注册返回 <see langword="null"/>）。</summary>
    /// <param name="toolName">工具名。</param>
    ToolCatalogEntry? Find(string toolName);
}

/// <summary>Schema 导出方言（AI-FD-D12 P1D-4：首版仅 OpenAI-compatible；Skills/Aily/MCP 归 Phase 4，接口位预留）。</summary>
public enum ToolSchemaDialect
{
    /// <summary>OpenAI-compatible <c>tools</c> JSON 数组（chat completions 请求体可直接使用）。</summary>
    OpenAiFunctions = 1,
}

/// <summary>
/// 工具 Schema 导出器（AI-FD-D12 P1D-4）：按方言把全部已注册工具导出为模型侧 tools 载荷。
/// </summary>
/// <remarks>
/// 数据源 = 编译期 <c>FeishuToolSchemas</c> 常量（零反射零成本）；输出为合法 JSON 文本，
/// 可被标准 OpenAI tools Schema 校验。
/// </remarks>
public interface IToolSchemaExporter
{
    /// <summary>按方言导出工具 Schema JSON。</summary>
    /// <param name="dialect">导出方言。</param>
    /// <returns>JSON 文本（方言载荷；无工具时为空数组 <c>[]</c>）。</returns>
    string Export(ToolSchemaDialect dialect);
}
