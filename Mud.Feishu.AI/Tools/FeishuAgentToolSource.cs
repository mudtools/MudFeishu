// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 工具源抽象：工具包向 <see cref="Agents.FeishuAgent"/> 暴露已启用工具的桥。
/// </summary>
/// <remarks>
/// <para>
/// 依赖方向铁律：AI 不反向依赖工具包——工具包（如 <c>Mud.Feishu.AI.FeishuTools</c>）
/// 派生本类并注册到容器，<c>AddFeishuAgent</c> 聚合全部工具源产出
/// <c>ChatOptions.Tools</c>。未注册任何工具源时保持裸模型行为。
/// </para>
/// </remarks>
public abstract class FeishuAgentToolSource
{
    /// <summary>
    /// 返回要暴露给模型的工具（应为白名单启用集合；每次调用可重新求值以反映最新注册状态）。
    /// </summary>
    /// <param name="serviceProvider">服务提供器（解析上下文访问器等协作件）。</param>
    /// <returns>模型可见工具列表。</returns>
    public abstract IReadOnlyList<AIFunction> GetTools(IServiceProvider serviceProvider);

    /// <summary>
    /// 返回<b>已启用工具所属域</b>的 guidance 资产（WP6 / AT-F09；装配为宿主指令之后的补充）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 默认返回空集（不注入任何域资产）——未注册工具包、或工具包未提供 guidance 时，
    /// 指令装配与 Phase 0 完全一致（零行为变化）。
    /// </para>
    /// <para>
    /// 为什么由<b>工具源</b>提供而不是让 AI 底座去查域表：域 guidance 的真相源
    /// （<c>Mud.Feishu.AI.Tools.Generated.FeishuToolGuidance</c>）由生成器发射进工具面实现包，
    /// AI 底座反向依赖它会破坏依赖方向铁律；此处以纯文本块形式交接，装配与截断由
    /// <see cref="Agents.FeishuGuidanceComposer"/> 统一负责。
    /// </para>
    /// </remarks>
    /// <param name="serviceProvider">服务提供器。</param>
    /// <returns>域 guidance 块（顺序即域优先级；空集 = 不注入）。</returns>
    public virtual IReadOnlyList<Agents.FeishuGuidanceBlock> GetGuidance(IServiceProvider serviceProvider) => [];
}
