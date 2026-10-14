// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.HttpUtils 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Demo.Tools;

/// <summary>
/// 工具声明特性（剖面槽 <c>ToolAttributeName = "DemoTool"</c>）。
/// </summary>
/// <remarks>
/// 标注在<b>工具承载接口</b>上；引擎按本特性把接口识别为「一枚模型可调用工具」，
/// 接口名须携带令牌标记（如 <c>IMudDemoTenant…</c> → tenant，槽位 016 校验与
/// <c>TokenKindMarkers</c> 推导结果一致）。
/// </remarks>
[AttributeUsage(AttributeTargets.Interface)]
public sealed class DemoToolAttribute : Attribute
{
    /// <summary>构造工具声明。</summary>
    /// <param name="name">模型可见工具名（dot.case，如 <c>doc.list</c>），同一剖面内唯一（槽位 003 守卫）。</param>
    public DemoToolAttribute(string name) => Name = name;

    /// <summary>模型可见工具名。</summary>
    public string Name { get; }

    /// <summary>工具描述（进入描述符；供模型选择工具）。</summary>
    public string? Description { get; set; }

    /// <summary>
    /// SDK 源挂钩：<c>{SDK接口简单名}.{方法名}</c>（如 <c>IMudDemoTenantV1Doc.ListAsync</c>）。
    /// 引擎据此解析 HTTP 方法 / 路由 / 输出 Schema 等编译期事实；指向不存在的方法即槽位 019（Error）。
    /// </summary>
    public string? Source { get; set; }

    /// <summary>读写分类声明（未声明时按 SDK 源 HTTP 方法推导；两者冲突即槽位 017）。</summary>
    public bool IsWrite { get; set; }

    /// <summary>所需授权 scope。</summary>
    public string[]? RequiredScopes { get; set; }

    /// <summary>scope 的 any-of 组。</summary>
    public string[]? AnyOf { get; set; }
}

/// <summary>工具参数声明特性（剖面槽 <c>ParameterAttributeName = "ToolParameter"</c>）。</summary>
/// <remarks>
/// 参数特性的声明名必须与 C# 参数名一致（模型可见键 = C# 参数名，槽位 015 校验）；
/// <see cref="Required"/> 决定描述符 <c>required</c> 数组与解包读取器形态（必填/可选）。
/// 参数描述优先取本特性的 <c>description</c> 实参，缺省回退到 XML &lt;param&gt; 文档注释
/// （引擎在语义文档不可见时回退语法树提取，未开启 GenerateDocumentationFile 的消费方同样可用）。
/// </remarks>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
public sealed class ToolParameterAttribute : Attribute
{
    /// <summary>构造参数声明。</summary>
    /// <param name="name">参数名（snake_case，须与 C# 参数名一致）。</param>
    /// <param name="description">参数描述（模型可见；缺省回退 XML &lt;param&gt; 注释）。</param>
    public ToolParameterAttribute(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    /// <summary>参数名。</summary>
    public string Name { get; }

    /// <summary>参数描述（引擎读取构造实参 2）。</summary>
    public string? Description { get; }

    /// <summary>是否必填。</summary>
    public bool Required { get; set; }
}

/// <summary>
/// 执行器绑定特性（剖面槽 <c>ToolHandlerAttributeName = "DemoToolHandler"</c>）。
/// </summary>
/// <remarks>
/// 标注在执行器方法上：把「工具 ↔ 执行器方法」映射显式化。方法签名契约为
/// <c>Task&lt;DemoToolResult&gt; Method(IReadOnlyDictionary&lt;string, object?&gt; args, CancellationToken ct)</c>
/// （结果类型来自剖面 <c>ResultTypeName</c> 槽），形态不符即槽位 024。
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DemoToolHandlerAttribute : Attribute
{
    /// <summary>构造绑定声明。</summary>
    /// <param name="toolInterface">绑定的工具承载接口。</param>
    public DemoToolHandlerAttribute(Type toolInterface) => ToolInterface = toolInterface;

    /// <summary>绑定的工具承载接口。</summary>
    public Type ToolInterface { get; }
}
