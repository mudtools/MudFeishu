// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 结构化工具错误载荷（B2 错误契约）：模型与宿主均可机械消费的错误分类信息。
/// </summary>
/// <remarks>
/// <para>
/// 置于 <see cref="FeishuToolResult"/> 的 <see cref="FeishuToolResult.Error"/> 属性中，
/// 同时以紧凑 JSON 形式注入结果文本的<b>首行</b>（模型可读、不受截断影响）。
/// </para>
/// <para>
/// <b>设计约束</b>：
/// <list type="bullet">
/// <item>载荷 ≤ 512 字节（首行 JSON，正文截断不得吃掉载荷）。</item>
/// <item><see cref="Category"/> 与 <see cref="Subtype"/> 为闭集常量（新增需评审 + 守卫断言）。</item>
/// <item>载荷字段是闭集枚举 + 数字 + 短字符串，无凭据面——出站净化不作用于载荷本身。</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="Category">错误大类（8 类闭集，见 <c>ToolErrorCategory</c>）。</param>
/// <param name="Subtype">错误子类（闭集常量，见 <c>ToolErrorSubtype</c>）。</param>
/// <param name="Retryable">是否可重试（模型据此决定"稍后重试"还是"放弃/换参"）。</param>
/// <param name="RetryAfterSeconds">重试前建议等待秒数（来自 <c>Retry-After</c> 头；null = 不提供）。</param>
/// <param name="ApiCode">飞书业务 code（如有）。</param>
/// <param name="Attempts">已尝试次数（重试场景，默认 1）。</param>
/// <param name="Trace">追踪号（与日志/Span 可关联，白名单化的代价补偿）。</param>
/// <param name="Tool">工具名（载荷锚点，便于宿主按工具聚合）。</param>
public sealed record ToolError(
    string Category,
    string Subtype,
    bool Retryable,
    int? RetryAfterSeconds = null,
    int? ApiCode = null,
    int Attempts = 1,
    string? Trace = null,
    string? Tool = null);
