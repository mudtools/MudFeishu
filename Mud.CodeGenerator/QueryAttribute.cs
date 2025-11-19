// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式查询参数特性
/// </summary>
/// <remarks>支持多次指定。</remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter,
    AllowMultiple = true)]
internal sealed class QueryAttribute : Attribute
{
    /// <summary>
    ///     <inheritdoc cref="QueryAttribute" />
    /// </summary>
    /// <remarks>特性作用于参数时有效。</remarks>
    public QueryAttribute()
    {
    }

    /// <summary>
    ///     <inheritdoc cref="QueryAttribute" />
    /// </summary>
    /// <remarks>
    ///     <para>当特性作用于方法或接口时，则表示移除指定查询参数操作。</para>
    ///     <para>当特性作用于参数时，则表示添加查询参数，同时设置查询参数键为 <c>name</c> 的值。</para>
    /// </remarks>
    /// <param name="name">查询参数键</param>
    public QueryAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// <inheritdoc cref="QueryAttribute" />
    /// </summary>
    /// <param name="name">查询参数键</param>
    /// <param name="formatString">参数值的格式化字符串</param>
    public QueryAttribute(string name, string? formatString)
        : this(name) =>
        FormatString = formatString;

    /// <summary>
    /// 查询参数键
    /// </summary>
    public string? Name { get; set; }



    /// <summary>
    /// 参数值的格式化字符串。
    /// </summary>
    public string? FormatString { get; set; }
}