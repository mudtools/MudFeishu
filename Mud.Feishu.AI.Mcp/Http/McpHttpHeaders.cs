// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Mcp;

/// <summary>
/// Streamable HTTP 用到的头名（HTTP 传输专有，故不进 <see cref="McpProtocol"/> 的协议常量面）。
/// </summary>
internal static class McpHttpHeaders
{
    /// <summary>会话标识头：<c>initialize</c> 响应下发，之后每条请求原样带回。</summary>
    public const string SessionId = "Mcp-Session-Id";

    /// <summary>协议版本头：握手后每条请求带上，须与本会话协商结果一致。</summary>
    public const string ProtocolVersion = "MCP-Protocol-Version";

    /// <summary>方法清单头（405 响应携带）。</summary>
    public const string Allow = "Allow";

    /// <summary>本端点支持的方法清单。</summary>
    public const string AllowedMethods = "POST, DELETE";
}
