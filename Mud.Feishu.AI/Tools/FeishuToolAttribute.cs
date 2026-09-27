// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 标注一个工具接口为 LLM FunctionCall 工具（Phase 1 §3.2）。
/// </summary>
/// <remarks>
/// <para>
/// 接口只声明 Schema（工具名、描述、模型可见参数），不复用 HttpUtils 的
/// <c>[Path]/[Body]</c> 路由特性（语义分属「工具参数」与「HTTP 绑定」两层）；
/// 参数到强类型请求的映射由 <c>FeishuToolBinding</c> 在执行链内完成（§3.3.3）。
/// </para>
/// <para>
/// 工具默认「进注册表不启用」，宿主须显式 <c>MapTool</c>（§3.3.4 白名单语义）。
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public sealed class FeishuToolAttribute(string name) : Attribute
{
    /// <summary>工具名（模型可见契约）。</summary>
    public string Name { get; } = name;

    /// <summary>模型侧描述（Skills 友好风格：短参数名 + 调用样例 + 权限一句话）。</summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// 所需权限点清单（已决策⑥）：随 Schema 输出，供 <see cref="IToolExecutionAuthorizer"/>
    /// 钩子与运维审计消费；SDK 不内建 scope 校验。
    /// </summary>
    public string[] RequiredScopes { get; init; } = [];

    /// <summary>
    /// 是否写操作：写工具在 <c>EnforceToolAuthorization=true</c> 时必须过授权器（Phase 2 强制）。
    /// </summary>
    public bool IsWrite { get; init; }
}
