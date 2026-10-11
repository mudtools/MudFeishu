// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// <b>R-7 窄接缝</b>：把「工具名 + 原因」渲染为<b>结构化错误文本</b>（首行 JSON 载荷 + 人类可读正文）
/// 的唯一公开入口。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它（B-7 / R-7）</b>：周边包（MCP 协议层）在"未预期异常"分支需要回填一条
/// 与执行链同构的错误文本，此前直接调用 <c>FeishuToolBinding.StructuredError</c>——
/// 该类型是实现细节（含授权/审计/重试/上下文切换的完整执行链），却被下游当作工具函数使用。
/// 抽成一句能力后，实现类型可以 <c>internal</c> 化。
/// </para>
/// <para>
/// <b>为什么不让调用方自己拼 JSON</b>：错误载荷的字段集（<c>category</c>/<c>subtype</c>/<c>retryable</c>/
/// <c>api_code</c>/<c>attempts</c>/<c>trace</c>/<c>tool</c>）是<b>模型可见契约</b>，由
/// <c>ToolErrorFactory</c> 单点构造。让 MCP 手拼会立刻产生第二份契约（且与 R-1 的
/// "错误出口唯一"直接冲突）。
/// </para>
/// </remarks>
public interface IToolErrorTextFormatter
{
    /// <summary>
    /// 渲染结构化错误文本。
    /// </summary>
    /// <param name="toolName">契约工具名（载荷锚点，供审计与模型对齐）。</param>
    /// <param name="reason">人类可读原因（调用方负责不泄漏凭据——净化在执行链出口）。</param>
    /// <returns>可直接回填模型的错误文本。</returns>
    string Format(string toolName, string reason);
}

/// <summary>
/// <see cref="IToolErrorTextFormatter"/> 的默认实现（<c>internal</c>：实现细节不进公开面）。
/// </summary>
/// <remarks>
/// 实现只有一行转发——接缝的价值不在"多一层"，而在把<b>契约</b>（本接口）与<b>实现</b>
/// （执行链类型）解耦，使后者可以退出公开面。
/// </remarks>
internal sealed class ToolErrorTextFormatter : IToolErrorTextFormatter
{
    /// <inheritdoc />
    public string Format(string toolName, string reason)
        => FeishuToolBinding.StructuredError(toolName, reason);
}
