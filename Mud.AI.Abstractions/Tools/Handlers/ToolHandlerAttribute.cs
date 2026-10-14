// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Abstractions.Tools.Handlers;

/// <summary>
/// 声明「执行器方法 ↔ <c>[AgentTool]</c> 策展接口」的绑定（源生成器据此产出域注册器与 DI 装配）。
/// </summary>
/// <remarks>
/// <para>
/// v2.3 中立化迁入（原 <c>Mud.Feishu.AI.Tools.Handlers.FeishuToolHandlerAttribute</c>，internal → public：
/// 非 owner 声明程序集也要能标注 handler；AI 族未发布，无兼容包袱）。
/// </para>
/// <para>
/// <b>命名空间不变量</b>：handler 特性与工具特性<b>必须分处不同命名空间</b>——引擎靠
/// (命名空间, 特性名) 二元组区分「声明面」与「执行器绑定面」（<c>FeishuToolProfile</c> 既有约束）。
/// </para>
/// <para>
/// <b>为什么必须有这层声明</b>：工具名可以派生（<c>[AgentTool]</c>），执行器类型可以派生
/// （特性的宿主类），但<b>二者的对应关系不可派生</b>——能力条目的 <c>MethodName</c> 是
/// <c>Source</c> 挂钩解析出的 <b>SDK 方法名</b>，而这里需要的是执行器方法名；
/// 一批工具不满足任何简单命名约定（<c>doc_wiki → SearchAsync</c>、
/// <c>resolve_user → ResolveUsersAsync</c>、<c>capability_lookup → LookupAsync</c> …），
/// 约定式推导会产出批量编译错误。
/// </para>
/// <para>
/// <b>为什么用 <see cref="Type"/> 而不是工具名字符串/常量</b>：
/// </para>
/// <list type="number">
/// <item><b>不能用生成的契约常量</b>：名字契约表由<b>生成器</b>在<b>同一趟</b>产出，
/// 源生成器看不到自己本轮输出，特性实参退化为错误常量；</item>
/// <item><b>字符串字面量次优</b>：工具改名需两处同步，只能靠运行期/诊断兜底；</item>
/// <item><b><c>typeof</c> 是符号链接</b>：接口是手写源码（编译期可见），故 <c>typeof</c> 恒可解析——
/// 写错类型是编译错误，接口改名后绑定<b>自动跟随</b>，工具名与描述始终取自接口自身的
/// <c>[AgentTool]</c> 声明（单一真相源）。</item>
/// </list>
/// <para>
/// <b>为什么放在执行器方法上而不是单独的注册器文件里</b>：新增一枚工具的编辑处数由 3 处降为 2 处
/// （接口声明 + 执行器方法），且「这个方法服务哪个工具」在阅读执行器时一目了然；域注册器与
/// DI 装配（含软缺席判定）全部由编译期聚合产出。
/// </para>
/// <para>
/// <b>分组语义</b>：绑定按<b>执行器类</b>聚合——一个类产出一个域注册器与一个 DI 核心方法。
/// 因此「跨 im/bitable/approval 三模块的写域」自然拆成三个注册器，不需要任何特例分支。
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ToolHandlerAttribute(Type toolInterface) : Attribute
{
    /// <summary>承载该工具策展声明的 <c>[AgentTool]</c> 接口类型。</summary>
    public Type ToolInterface { get; } = toolInterface;
}
