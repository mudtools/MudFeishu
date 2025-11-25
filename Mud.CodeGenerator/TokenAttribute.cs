// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

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
public enum TokenType
{
    /// <summary>
    /// 使用租户Token调用函数。
    /// </summary>
    TenantAccessToken = 0,
    /// <summary>
    /// 使用用户Token调用函数。
    /// </summary>
    UserAccessToken = 1,
    /// <summary>
    /// 使用应用Token调用函数。
    /// </summary>
    AppAccessToken = 2,
}