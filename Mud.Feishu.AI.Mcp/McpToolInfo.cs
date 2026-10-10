// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// 一个经 MCP 暴露的工具（服务端视角的自省视图；<c>tools/list</c> 的内容由它派生）。
/// </summary>
/// <remarks>
/// 本视图<b>不含执行入口</b>（执行入口是内部的 <c>AIFunction</c>）——宿主拿到它只能做
/// "列工具/做展示/写日志"，拿不到"绕过执行链直接调用"的把手。
/// </remarks>
/// <param name="Name">MCP 工具名（严格客户端可接受：<c>[a-zA-Z0-9_-]</c>；契约名的 <c>.</c> 已映射为 <c>_</c>）。</param>
/// <param name="ContractName">飞书工具契约名（如 <c>bitable.query_records</c>；与《工具权限对照表》一致）。</param>
/// <param name="Description">模型侧描述（编译期 Schema 常量原文）。</param>
/// <param name="IsWrite">是否写类工具（<c>WriteAllowList</c> 键控的事实来源）。</param>
/// <param name="Risk">风险分级（编译器从 SDK 事实派生；<c>high-risk-write</c> 映射为 <c>destructiveHint</c>）。</param>
/// <param name="Identity">工具身份（<c>tenant</c> / <c>user</c>）。</param>
public sealed record McpToolInfo(
    string Name,
    string ContractName,
    string Description,
    bool IsWrite,
    FeishuToolRisk Risk,
    string Identity);
