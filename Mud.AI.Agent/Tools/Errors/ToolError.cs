// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.AI.Abstractions.Tools.Errors;

namespace Mud.AI.Agent.Tools.Errors;

/// <summary>
/// 结构化工具错误载荷（B2 错误契约）：模型与宿主均可机械消费的错误分类信息。
/// </summary>
/// <remarks>
/// 置于 <see cref="ToolResult"/> 的 <see cref="ToolResult.Error"/> 属性中，同时以紧凑 JSON 注入结果文本的
/// <b>首行</b>（模型可读、不受截断影响）。设计约束：
/// <list type="bullet">
/// <item>载荷 ≤ 512 字节：省略顺序 <c>Suggestions</c> → <c>RequiredScopes</c> → <c>Trace</c>，
/// 而 <c>category</c>/<c>subtype</c>/<c>retryable</c> 恒在（模型可分支的最小集）。</item>
/// <item><see cref="Category"/> 与 <see cref="Subtype"/> 为闭集常量（新增需评审 + 守卫断言）。</item>
/// <item>载荷字段是闭集枚举 + 数字 + 短字符串，无凭据面——出站净化不作用于载荷本身。</item>
/// </list>
/// </remarks>
/// <param name="Category">错误大类（8 类闭集，见 <c>ToolErrorCategory</c>）。</param>
/// <param name="Subtype">错误子类（闭集常量，见 <c>ToolErrorSubtype</c>）。</param>
/// <param name="Retryable">是否可重试（模型据此决定"稍后重试"还是"放弃/换参"）。</param>
/// <param name="RetryAfterSeconds">
/// 重试前<b>建议</b>等待秒数（null = 不提供）。<b>不是</b>"服务端指示"：现行唯一产出点是本地退避策略
/// （<c>ToolRetryPolicy.ComputeDelay</c> 的首次退避，只在 <c>Retryable</c> 类错误上产出）；
/// <c>Retry-After</c> 响应头在中立端口（<see cref="ITransportFault"/> 只暴露 <c>StatusCode</c>）下不可达，
/// 属已登记的开放项——打开它须先扩端口并论证"每个产品都提供得起该事实"。
/// </param>
/// <param name="ApiCode">产品业务 code（如有；透出以便宿主/模型按码排查）。</param>
/// <param name="Attempts">已尝试次数（重试场景，默认 1）。</param>
/// <param name="Trace">追踪号（与日志/Span 可关联，白名单化的代价补偿）。</param>
/// <param name="Tool">工具名（载荷锚点，便于宿主按工具聚合）。</param>
/// <param name="Suggestions">
/// 恢复候选（模型可执行的下一步，<b>无需解析人类可读正文</b>）。生产者只允许是<b>事实</b>来源——
/// 闭集参数的合法取值清单、工具声明的所需权限；不得把"猜测的缺失权限"写成候选
/// （与 <c>ToolErrorSubtype</c> 的"幽灵子类"纪律同源）。
/// </param>
/// <param name="RequiredScopes">
/// <b>工具<b>声明</b>的所需权限</b>（结构化 scope 事实投影）：语义是"该工具声明需要哪些 scope"，
/// <b>不是</b>"平台是否已为应用开通"——后者在本地无可靠判据（权限段业务码在不同层被解释成不同的东西，
/// 见产品侧码表），故本字段与错误文本都不得被读成"缺的就是这些"，也不对应任何"平台缺权限"子类
/// （字段是"工具声明"，子类才是"平台判据"）。产出点唯一：授权类拒绝路径
/// （<c>FeishuToolBinding</c> 的 <c>DenyAsync</c> 与异常归一路径）从 <c>tool.RequiredScopes</c> 投影，
/// 其他分类不产出该字段（避免把无关错误染上权限语义）。
/// </param>
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
