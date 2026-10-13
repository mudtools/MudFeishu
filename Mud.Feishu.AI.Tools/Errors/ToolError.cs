// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Errors;

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
/// <item><see cref="Suggestions"/> 超预算时<b>整体省略</b>（分类字段优先），保证上界不被突破。</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="Category">错误大类（8 类闭集，见 <c>ToolErrorCategory</c>）。</param>
/// <param name="Subtype">错误子类（闭集常量，见 <c>ToolErrorSubtype</c>）。</param>
/// <param name="Retryable">是否可重试（模型据此决定"稍后重试"还是"放弃/换参"）。</param>
/// <param name="RetryAfterSeconds">
/// 重试前<b>建议</b>等待秒数（null = 不提供）。
/// </param>
/// <remarks>
/// <b>语义澄清（E-5 校准，2026-10-13）</b>：本字段是"建议"，<b>不宣称</b>"服务端指示"。
/// 两个来源按优先级取用：
/// <list type="number">
/// <item><c>Retry-After</c> 响应头（服务端权威值）——⚠️ <b>当前传输层不暴露响应头</b>：
/// <c>Mud.HttpUtils.ApiException</c> 只携带 <c>StatusCode</c> / <c>Content</c> / <c>RequestUri</c>，
/// 故该来源在现行依赖下不可达（已登记为开放项，不是遗漏）；</item>
/// <item>本地退避策略给出的建议值（<c>ToolRetryPolicy.ComputeDelay</c> 的首次退避）——
/// 这是<b>唯一</b>现行产出点，只在 <c>Retryable</c> 类错误上产出。</item>
/// </list>
/// 消费者应把本字段读成"建议等这么久再试"，而不是"平台要求等这么久"。
/// </remarks>
/// <param name="ApiCode">飞书业务 code（如有）。</param>
/// <param name="Attempts">已尝试次数（重试场景，默认 1）。</param>
/// <param name="Trace">追踪号（与日志/Span 可关联，白名单化的代价补偿）。</param>
/// <param name="Tool">工具名（载荷锚点，便于宿主按工具聚合）。</param>
/// <param name="Suggestions">
/// 恢复候选（BUG-8：模型可执行的下一步，<b>无需解析人类可读正文</b>）。
/// 生产者只允许是<b>事实</b>来源——闭集参数的合法取值清单、工具声明的所需权限；
/// 不得把"猜测的缺失权限"写成候选（与 <c>ToolErrorSubtype</c> 的"幽灵子类"纪律同源）。
/// </param>
/// <param name="RequiredScopes">
/// <b>E-1：工具<b>声明</b>的所需权限（结构化 scope 事实投影）。</b>
/// </param>
/// <remarks>
/// <para>
/// <b><see cref="RequiredScopes"/> 的语义边界（DP-R7-4 纪律的延续）</b>：这里投影的是
/// <b>"该工具声明需要哪些 scope"这一工具面事实</b>，<b>不是</b>"平台是否已为应用开通"这一判断
/// ——后者在本地无可靠判据（<c>99991663</c>/<c>99991661</c> 在三个层被解释成三种不同的东西），
/// 故本字段的文档与错误文本都不得把它读成"缺的就是这些"。
/// 因此它也<b>不</b>复活已删除的 <c>missing_scope</c> subtype：字段是"工具声明"，subtype 是"平台判据"。
/// </para>
/// <para>
/// <b>产出点唯一</b>：授权类拒绝路径（<c>FeishuToolBinding</c> 的 <c>DenyAsync</c> 与异常归一路径）
/// 从 <c>tool.RequiredScopes</c> 投影，其他分类不产出该字段（避免把无关错误染上权限语义）。
/// </para>
/// <para>
/// <b>预算优先级</b>（≤512 字节不变量）：省略顺序为 <c>Suggestions</c> → <c>RequiredScopes</c> →
/// <c>Trace</c>，而 <c>category</c>/<c>subtype</c>/<c>retryable</c> 恒在（模型可分支的最小集）。
/// </para>
/// </remarks>
public sealed record ToolError(
    string Category,
    string Subtype,
    bool Retryable,
    int? RetryAfterSeconds = null,
    int? ApiCode = null,
    int Attempts = 1,
    string? Trace = null,
    string? Tool = null,
    IReadOnlyList<string>? Suggestions = null,
    IReadOnlyList<string>? RequiredScopes = null);
