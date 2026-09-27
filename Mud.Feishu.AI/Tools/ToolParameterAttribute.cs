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
}

/// <summary>
/// 对模型隐藏某工具参数（生成器不纳入 Schema）。
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class ToolHideAttribute(string name) : Attribute
{
    /// <summary>被隐藏的参数名。</summary>
    public string Name { get; } = name;
}
