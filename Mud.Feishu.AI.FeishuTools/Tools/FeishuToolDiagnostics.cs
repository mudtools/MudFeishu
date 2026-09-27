// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.FeishuTools;

/// <summary>
/// 工具执行遥测：执行链 Span 与护城河审计属性（Phase 1 §5 硬验收③）。
/// </summary>
/// <remarks>
/// <para>
/// 复用 <see cref="FeishuActivitySource"/>（宿主经 Mud.Feishu.OpenTelemetry 零配置并入既有链路）。
/// Span 名 <c>feishu.agent.tool</c>；审计属性：<c>feishu.tool_name</c>、<c>feishu.app_key</c>、
/// <c>feishu.tool.scopes</c>、<c>feishu.tool.decision</c>（allowed/denied）、<c>feishu.tool.write</c>——
/// scope 与授权判定进结构化审计（对齐官方 lark-cli 的权限自省能力，但为宿主强制门禁，已决策⑥）。
/// </para>
/// <para>
/// 敏感治理：Span 属性只允许结构化标量（工具名/ID/判定结果），工具入参值与结果文本<b>不得</b>入属性
/// （对齐 D5 日志最小暴露精神）。
/// </para>
/// </remarks>
internal static class FeishuToolDiagnostics
{
    /// <summary>Span 名：工具执行。</summary>
    public const string ActivityName = "feishu.agent.tool";

    /// <summary>Span 属性：工具名（注册表契约名）。</summary>
    public const string TagToolName = "feishu.tool_name";

    /// <summary>Span 属性：所需权限点清单（逗号连接；占位 scope，落地时按控制台核对）。</summary>
    public const string TagScopes = "feishu.tool.scopes";

    /// <summary>Span 属性：授权判定（<see cref="DecisionAllowed"/> / <see cref="DecisionDenied"/>）。</summary>
    public const string TagDecision = "feishu.tool.decision";

    /// <summary>Span 属性：是否写操作。</summary>
    public const string TagIsWrite = "feishu.tool.write";

    /// <summary>Span 属性：结果是否被截断。</summary>
    public const string TagTruncated = "feishu.tool.truncated";

    /// <summary>判定值：放行。</summary>
    public const string DecisionAllowed = "allowed";

    /// <summary>判定值：拒绝（含写工具未过授权、授权器拒绝、待确认挂起三种）。</summary>
    public const string DecisionDenied = "denied";

    /// <summary>
    /// 开启一次工具执行 Span（无监听器时返回 null，调用方须判空）。
    /// </summary>
    /// <param name="toolName">工具名。</param>
    /// <param name="appKey">应用唯一标识。</param>
    /// <param name="requiredScopes">所需权限点（审计消费）。</param>
    /// <param name="isWrite">是否写操作。</param>
    /// <returns>Activity（可能为 null）。</returns>
    public static Activity? StartToolActivity(string toolName, string appKey, IReadOnlyList<string> requiredScopes, bool isWrite)
    {
        var activity = FeishuActivitySource.Instance.StartActivity(ActivityName, ActivityKind.Internal);
        activity?.SetTag(TagToolName, toolName)
                .SetTag(FeishuActivitySource.Tags.AppKey, appKey)
                .SetTag(TagScopes, string.Join(",", requiredScopes))
                .SetTag(TagIsWrite, isWrite);
        return activity;
    }
}
