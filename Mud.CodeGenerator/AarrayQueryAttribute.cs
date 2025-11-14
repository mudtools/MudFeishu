namespace Mud.CodeGenerator;

/// <summary>
/// HTTP 声明式数组查询参数特性
/// </summary>
/// <remarks>支持多次指定。</remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter,
    AllowMultiple = true)]
internal sealed class ArrayQueryAttribute : Attribute
{
    /// <summary>
    ///     <inheritdoc cref="QueryAttribute" />
    /// </summary>
    /// <remarks>特性作用于参数时有效。</remarks>
    public ArrayQueryAttribute()
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
    public ArrayQueryAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// <inheritdoc cref="QueryAttribute" />
    /// </summary>
    /// <param name="name">查询参数键</param>
    /// <param name="formatString">多个参数值的分隔字符串</param>
    public ArrayQueryAttribute(string name, string? formatString)
        : this(name) =>
        SplitString = formatString;

    /// <summary>
    /// 查询参数键
    /// </summary>
    public string? Name { get; set; }



    /// <summary>
    /// 多个参数值的分隔字符串。
    /// </summary>
    public string? SplitString { get; set; }
}