// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.AgentTools;

/// <summary>
/// 工具执行上下文：当前调用所在的应用/会话环境（多租户隔离的事实来源）。
/// </summary>
/// <remarks>
/// <para>
/// 缺失 <see cref="AppKey"/> 即失败——工具执行期间令牌/端点切换依赖
/// <c>IFeishuAppContextSwitcher.BeginScope(appKey)</c>（租户=应用上下文，TMA2-20）。
/// </para>
/// <para>
/// <b>归属（BUG-1 / 方案 C）</b>：本类型同时出现在 <c>Mud.Feishu.AI</c> 的公开签名
/// （<see cref="IFeishuToolContextAccessor.Begin"/>、各集成面事件处理器）与工具面执行链
/// （<c>FeishuToolHandler</c> / <c>IToolExecutionAuthorizer</c>）中，属"AI 运行时 ↔ 工具面共享接缝"，
/// 故留在 <c>Mud.Feishu.AI</c> 并落在本包自有命名空间
/// <c>Mud.Feishu.AI.AgentTools</c>——命名空间与程序集归属唯一，由
/// <c>PackageOwnershipContractGuards</c> 机械锁定。
/// </para>
/// </remarks>
/// <param name="AppKey">应用唯一标识（必填）。</param>
/// <param name="ConversationKey">会话键（可空）。</param>
/// <param name="ChatId">触发会话的 chat_id（可空）。</param>
/// <param name="UserId">触发用户 ID（可空）。</param>
public sealed record FeishuToolContext(
    string AppKey,
    string? ConversationKey = null,
    string? ChatId = null,
    string? UserId = null,

   // R5 / F-4：把 thread 维度**带进工具执行上下文**，使 im.reply_message 的 reply_in_thread
   // 能**自动取自当前会话**（模型不必猜、也不必显式传参）。
   // 追加为末尾可选参数 ⇒ 既有构造点源码兼容。
   string? ThreadId = null);
