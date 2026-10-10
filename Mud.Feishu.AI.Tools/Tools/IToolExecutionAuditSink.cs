// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具执行结构化审计出口（AI-FD-D12 P1D-3b）：执行链每完成一次工具执行（含<b>拒绝</b>——
/// 拒绝也是审计事件）投递一条记录，供宿主对接 SIEM/落库。
/// </summary>
/// <remarks>
/// <para>
/// SDK 内建默认 = <b>无实现</b>（OTel Span 已覆盖基础审计）；宿主按需注册（DI 注册任一实现即生效）。
/// 投递异常只记日志、绝不影响执行链（异常隔离，D15 精神）。
/// </para>
/// <para>
/// <b>脱敏责任</b>：<see cref="ToolExecutionAuditRecord.ArgsDigest"/> 由 SDK 侧完成生成
/// （参数名 + 值长度 + 敏感键名掩码）——宿主 sink 不接触原始参数，防审计通道变成敏感数据泄漏面；
/// 模型 API Key/AppSecret 绝不入审计载荷（安全默认 §三.5）。
/// </para>
/// </remarks>
public interface IToolExecutionAuditSink
{
    /// <summary>写入一条工具执行审计记录。</summary>
    /// <param name="record">审计记录（入参已脱敏摘要）。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    Task WriteAsync(ToolExecutionAuditRecord record, CancellationToken cancellationToken = default);
}

/// <summary>
/// 工具执行审计记录（脱敏后的最小事实集）。
/// </summary>
/// <param name="ToolName">工具名（注册表契约名）。</param>
/// <param name="AppKey">应用唯一标识。</param>
/// <param name="RequiredScopes">所需权限点清单（随 Schema 审计）。</param>
/// <param name="Decision">判定结果：<c>allowed</c> / <c>denied</c> / <c>error</c>。</param>
/// <param name="Reason">拒绝或错误原因（可空；不含敏感原文）。</param>
/// <param name="IsWrite">是否写操作。</param>
/// <param name="ArgsDigest">入参摘要（SDK 侧脱敏：参数名 + 值长度 + 敏感键名掩码，截断）。</param>
/// <param name="DurationMs">执行耗时（毫秒；拒绝路径为授权耗时）。</param>
/// <param name="ConversationKey">会话键（可空；键维度允许进审计载荷，不进 Metrics tag——原则 8）。</param>
public sealed record ToolExecutionAuditRecord(
    string ToolName,
    string AppKey,
    IReadOnlyList<string> RequiredScopes,
    string Decision,
    string? Reason,
    bool IsWrite,
    string? ArgsDigest,
    long DurationMs,
    string? ConversationKey);
