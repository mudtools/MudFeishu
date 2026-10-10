// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.AI;
using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// <b>R-7 窄接缝</b>：把「注册表工具定义 + 编译期 Schema」桥接为 MEAI <see cref="AIFunction"/> 的
/// <b>唯一公开入口</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么需要它（B-7 / R-7）</b>：<c>FeishuToolAIFunction</c> 此前是 <c>public</c>，
/// 原因仅仅是"跨程序集的 MCP 包要 <c>new</c> 它"——这是典型的"公开面被下游逼大"：
/// 桥接类型的构造/内部字段（定义、上下文访问器、Schema 缓存策略）都成了对外承诺，
/// 收敛公开面就必须先给出一个比实现类型更窄、更稳定的接缝。
/// </para>
/// <para>
/// <b>接缝为什么这么窄</b>：宿主/周边包需要的能力只有一句"给我这个工具的可调用形态"。
/// 参数是<b>契约类型</b>（<see cref="FeishuToolDefinition"/> 来自注册表 + 编译期 Schema 常量），
/// 返回值是<b>框架类型</b>（<see cref="AIFunction"/>）——两边都不是本包内部实现，
/// 因此接缝可以在实现类型 internal 化之后继续成立。
/// </para>
/// <para>
/// <b>上下文访问器不进接缝</b>（有意）：<c>IFeishuToolContextAccessor</c> 由 DI 提供（执行链与
/// 桥接必须共用同一个单例，否则 MCP 侧 <c>Begin</c> 建立的上下文对桥接不可见），
/// 让调用方自行传入会重建出"两个访问器"的形态——那正是多租户串号类缺陷的入口。
/// </para>
/// </remarks>
public interface IFeishuToolFunctionFactory
{
    /// <summary>
    /// 构造工具的可调用形态。
    /// </summary>
    /// <param name="definition">注册表工具定义（含名称/描述/风险/身份与执行 Handler）。</param>
    /// <param name="schemaJson">编译期生成的工具描述符常量（信封形态，含 <c>parameters</c>）。</param>
    /// <returns>可直接调用的 <see cref="AIFunction"/>（执行链、授权门禁与出站净化均在其内部）。</returns>
    AIFunction Create(FeishuToolDefinition definition, string schemaJson);
}

/// <summary>
/// <see cref="IFeishuToolFunctionFactory"/> 的默认实现（<c>internal</c>：实现细节不进公开面）。
/// </summary>
/// <param name="contextAccessor">DI 提供的工具执行上下文访问器（与执行链共用同一单例）。</param>
internal sealed class FeishuToolFunctionFactory(IFeishuToolContextAccessor contextAccessor)
    : IFeishuToolFunctionFactory
{
    /// <inheritdoc />
    public AIFunction Create(FeishuToolDefinition definition, string schemaJson)
        => new FeishuToolAIFunction(definition, schemaJson, contextAccessor);
}
