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