namespace Mud.CodeGenerator;

/// <summary>
/// HTTP 声明式token参数特性
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
internal sealed class TokenAttribute : Attribute
{
    /// <summary>
    /// <inheritdoc cref="TokenAttribute" />
    /// </summary>
    public TokenAttribute() : this(TokenType.TenantAccessToken)
    {
    }

    /// <summary>
    /// <inheritdoc cref="TokenAttribute" />
    /// </summary>
    /// <param name="tokenType">飞书Token类型。</param>
    public TokenAttribute(TokenType tokenType)
    {
        TokenType = tokenType;
    }

    /// <summary>
    /// 飞书Token类型。
    /// </summary>
    public TokenType TokenType { get; set; } = TokenType.TenantAccessToken;
}

/// <summary>
/// 飞书Token类型。
/// </summary>
internal enum TokenType
{
    /// <summary>
    /// 使用应用Token调用函数。
    /// </summary>
    TenantAccessToken = 0,
    /// <summary>
    /// 使用用户Token调用函数。
    /// </summary>
    UserAccessToken = 1,
    /// <summary>
    /// 由用户决定使用何种Token调用函数。
    /// </summary>
    Both = 2,
}