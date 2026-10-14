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
/// <b>命名空间不变量</b>：handler 特性与工具特性<b>必须分处不同命名空间</b>——引擎靠
/// (命名空间, 特性名) 二元组区分「声明面」与「执行器绑定面」（<c>FeishuToolProfile</c> 既有约束）。
/// <para>
/// <b>为什么必须有这层声明</b>：工具名可以派生（<c>[AgentTool]</c>）、执行器类型可以派生（特性宿主类），
/// 但<b>二者的对应关系不可派生</b>——能力条目的 <c>MethodName</c> 是 <c>Source</c> 挂钩解析出的
/// <b>SDK 方法名</b>，这里要的是执行器方法名，且一批工具不满足任何简单命名约定
/// （<c>doc_wiki → SearchAsync</c>、<c>resolve_user → ResolveUsersAsync</c> …），约定式推导会批量编译错误。
/// </para>
/// <para>
/// <b>为什么用 <see cref="Type"/> 而不是工具名字符串</b>：①不能引用生成的契约常量——名字契约表由<b>生成器
/// 同一趟</b>产出，它看不到自己本轮输出，实参退化为错误常量；②字符串字面量改名要两处同步，只能靠诊断兜底；
/// ③<see cref="Type"/> 是符号链接：接口是手写源码故 <c>typeof</c> 恒可解析，写错类型即编译错误、接口改名
/// 后绑定<b>自动跟随</b>，工具名与描述始终取自接口自身的 <c>[AgentTool]</c>（单一真相源）。
/// </para>
/// <b>放置与分组</b>：标在执行器方法上——新增一枚工具只需改 2 处（接口声明 + 执行器方法），且
/// 「这个方法服务哪个工具」阅读时一目了然；绑定按<b>执行器类</b>聚合，一个类产出域注册器与 DI 核心方法
/// 各一个，跨模块写域自然拆成多个注册器，无需特例分支。本特性为 public 是为了让非 owner 声明程序集
/// 也能标注 handler（AI 族未发布，无兼容包袱）。
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class ToolHandlerAttribute(Type toolInterface) : Attribute
{
    /// <summary>承载该工具策展声明的 <c>[AgentTool]</c> 接口类型。</summary>
    public Type ToolInterface { get; } = toolInterface;
}
