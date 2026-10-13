// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// MCP 资源条目（<c>resources/list</c> 的内容来源，方案 M-2）。
/// </summary>
/// <param name="Uri">资源 URI（形如 <c>feishu-guidance://im</c>）。</param>
/// <param name="Name">人读名称（客户端 UI 展示）。</param>
/// <param name="Description">用途说明（何时该读它）。</param>
/// <param name="MimeType">媒体类型（本服务统一 <c>text/markdown</c>）。</param>
/// <param name="SizeBytes">
/// 内容大小（UTF-8 字节）。
/// </param>
/// <remarks>
/// <para>
/// <b>为什么带 <c>SizeBytes</c></b>（方案 §10 风险登记"客户端盲目全读"的对策）：MCP 客户端常常
/// 把 <c>resources/list</c> 的结果一次性灌进上下文；给出体积估算后，客户端（或用户）可以据此
/// 决定"按需读哪几个"，而不是"全部读"。这是本服务对渐进披露的最后一环补充：
/// <b>instructions 给路由摘要 → resources/list 给清单与体积 → resources/read 才取正文</b>。
/// </para>
/// <para>
/// <b>为什么不做分页</b>：资源清单规模 = 启用域 × 2 + high-risk 工具数 + 2，量级为几十~百余条，
/// 远未到需要 cursor 的程度；协议上 <c>resources/list</c> 的分页是可选项
/// （<c>nextCursor</c> 缺失即表示已列完），故首版刻意不做（方案 M-4）。
/// </para>
/// </remarks>
public sealed record McpResourceInfo(
    string Uri,
    string Name,
    string Description,
    string MimeType,
    int SizeBytes);
