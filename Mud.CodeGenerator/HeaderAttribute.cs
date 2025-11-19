// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式请求标头特性
/// </summary>
/// <remarks>支持多次指定。</remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter,
    AllowMultiple = true)]
internal sealed class HeaderAttribute : Attribute
{
    /// <summary>
    ///     <inheritdoc cref="HeaderAttribute" />
    /// </summary>
    /// <remarks>特性作用于参数时有效。</remarks>
    public HeaderAttribute()
    {
    }

    /// <summary>
    ///     <inheritdoc cref="HeaderAttribute" />
    /// </summary>
    /// <remarks>
    ///     <para>当特性作用于方法或接口时，则表示移除指定请求标头操作。</para>
    ///     <para>当特性作用于参数时，则表示添加请求标头，同时设置请求标头键为 <c>name</c> 的值。</para>
    /// </remarks>
    /// <param name="name">请求标头键</param>
    public HeaderAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    ///     <inheritdoc cref="HeaderAttribute" />
    /// </summary>
    /// <param name="name">请求标头键</param>
    /// <param name="value">请求标头的值</param>
    public HeaderAttribute(string name, object? value)
        : this(name) =>
        Value = value;

    /// <summary>
    ///     请求标头键
    /// </summary>
    /// <remarks>该属性优先级低于 <see cref="AliasAs" /> 属性设置的值。</remarks>
    public string? Name { get; set; }

    private object? field;

    /// <summary>
    ///     请求标头的值
    /// </summary>
    /// <remarks>当特性作用于参数时，表示默认值。</remarks>
    public object? Value
    {
        get => field;
        set
        {
            field = value;
            HasSetValue = true;
        }
    }

    /// <summary>
    ///     别名
    /// </summary>
    /// <remarks>
    ///     <para>特性用于参数时有效。</para>
    ///     <para>该属性优先级高于 <see cref="Name" /> 属性设置的值。</para>
    /// </remarks>
    public string? AliasAs { get; set; }

    /// <summary>
    ///     是否转义
    /// </summary>
    public bool Escape { get; set; }

    /// <summary>
    ///     是否替换已存在的请求标头。默认值为 <c>false</c>
    /// </summary>
    public bool Replace { get; set; }

    /// <summary>
    ///     是否设置了值
    /// </summary>
    internal bool HasSetValue { get; private set; }
}