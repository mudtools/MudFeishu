// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools;

/// <summary>
/// 覆盖工具参数的模型侧描述与必填性（Phase 1 §3.2）。
/// </summary>
/// <remarks>
/// XML 注释进 Schema <c>description</c> 之外，本特性提供显式覆盖
/// （改善模型入参推断准确率，对齐 Aily「字段描述解释 + 样例」建议）。
/// </remarks>
/// <param name="name">参数名（模型可见，snake_case 契约）。</param>
/// <param name="description">参数描述（可含取值样例）。</param>
/// <remarks>
/// 两种标注形态均支持：标注在<b>参数上</b>（推荐，生成器按符号读取），或标注在
/// 方法上并按 <see cref="Name"/> 匹配参数（生成器兼容此写法）。
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
public sealed class ToolParameterAttribute(string name, string description) : Attribute
{
    /// <summary>参数名（模型可见，snake_case 契约）。</summary>
    public string Name { get; } = name;

    /// <summary>参数描述（可含取值样例）。</summary>
    public string Description { get; } = description;

    /// <summary>是否必填（模型侧 <c>required</c>；可空参数不纳入 required）。</summary>
    public bool Required { get; init; }

    /// <summary>
    /// 该参数的<b>取值闭集类型</b>（R5 / F-2）：指定后生成器据此渲染 <c>"enum":[…]</c>，
    /// 并在解包期对非法值报<code>invalid_args</code> +附合法值清单。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>为什么需要它（F-2 根因）</b>：此前枚举语义参数只能靠描述里的散文表达
    /// （如"块类型（2=文本段落, 3=标题1, …）"），而
    /// <c>ToolArgsEmitter.TryResolveReader</c> 只映射 <c>string/string[]/int/bool</c>
    /// ⇒ <b>任何其他类型（含 C# enum）触发 MUDFT020 零容忍 Error 且整份 {Tool}Args 不产出</b>。
    /// 于是"参数类型是枚举"这件事<b>在当前架构下不可表达</b>。
    /// </para>
    /// <para>
    /// <b>为什么是 <see cref="Type"/> 而非字符串</b>：类型系统能校验的东西不降级为字符串
    /// （R4 U-2 的否决理由）。写错类型名 ⇒ 编译期报错。
    /// </para>
    /// <para>
    /// <b>接受两类闭集来源（R5 / R-4，必须同时支持）</b>：
    /// <list type="number">
    /// <item><b>真正的 C# enum</b>：<c>TypeKind == TypeKind.Enum</c>，成员取
    /// <c>GetMembers().OfType&lt;IFieldSymbol&gt;()</c> 并以 <c>HasConstantValue</c> 过滤；</item>
    /// <item><b>常量类</b>（<c>static class</c> + <c>const int</c>）：如
    /// <c>BlockTypes</c>（<c>Block.BlockType</c> 是 <c>int</c>，改成 <c>enum</c> 属破坏性变更）。
    /// 其 <c>TypeKind</c> 是 <c>Class</c> 而<b>非</b> <c>Enum</c>
    /// ⇒ 只判 <c>TypeKind == Enum</c> 会<b>永不命中</b>，这是 B-1 落地后必须解决的耦合点。</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>约束</b>：常量类的成员必须是 <c>const int</c>（或整型常量）；不满足时生成器报
    /// MUDFT011 之外的既有零容忍诊断而非静默放行。
    /// </para>
    /// </remarks>
    public Type? EnumType { get; init; }
}
