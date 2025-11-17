namespace Mud.CodeGenerator;

/// <summary>
/// HTTP 声明式token参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class TokenAttribute : Attribute
{
    /// <summary>
    /// <inheritdoc cref="PathAttribute" />
    /// </summary>
    public TokenAttribute()
    {
    }
}