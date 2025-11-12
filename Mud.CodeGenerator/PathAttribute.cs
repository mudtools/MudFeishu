namespace Mud.CodeGenerator;

/// <summary>
/// HTTP 声明式路径参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class PathAttribute : Attribute
{
    /// <summary>
    /// <inheritdoc cref="PathAttribute" />
    /// </summary>
    public PathAttribute()
    {
    }


    /// <summary>
    /// <inheritdoc cref="PathAttribute" />
    /// </summary>
    /// <param name="formatString">参数值的格式化字符串</param>
    public PathAttribute(string? formatString) =>
        FormatString = formatString;

    /// <summary>
    /// 参数值的格式化字符串。
    /// </summary>
    public string? FormatString { get; set; }
}