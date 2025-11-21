// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.CodeGenerator;

/// <summary>
/// HTTP客户端API包装器特性，用于标记需要进行自动生成的HTTP客户端API接口。
/// 该特性指定了令牌管理服务接口和包装器接口，用于自动生成具有令牌管理功能的HTTP客户端实现。
/// </summary>
/// <remarks>
/// 该特性通常与 <see cref="HttpClientApiAttribute"/> 一起使用，为API接口提供统一的令牌管理和包装功能。
/// 生成的HTTP客户端将自动处理令牌的获取、刷新和注入，简化API调用过程。
/// </remarks>
/// <example>
/// <code>
/// [HttpClientApi]
/// [HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IMyApi))]
/// public interface IMyApi
/// {
///     [Get("https://api.example.com/users")]
///     Task&lt;string&gt; GetUsersAsync();
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
public class HttpClientApiWrapAttribute : Attribute
{
    /// <summary>
    /// 初始化 <see cref="HttpClientApiWrapAttribute"/> 类的新实例。
    /// </summary>
    /// <remarks>
    /// 使用此构造函数创建的实例需要后续设置 <see cref="TokenManage"/> 和 <see cref="WrapInterface"/> 属性。
    /// </remarks>
    public HttpClientApiWrapAttribute()
    {
    }

    /// <summary>
    /// 使用指定的令牌管理服务接口名称初始化 <see cref="HttpClientApiWrapAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="tokenManage">令牌管理服务接口的名称，用于获取和刷新访问令牌。</param>
    /// <example>
    /// <code>
    /// [HttpClientApiWrap(TokenManage = nameof(ITokenManager))]
    /// public interface IMyApi { }
    /// </code>
    /// </example>
    public HttpClientApiWrapAttribute(string tokenManage)
    {
        TokenManage = tokenManage;
    }

    /// <summary>
    /// 使用指定的令牌管理服务接口名称和包装器接口名称初始化 <see cref="HttpClientApiWrapAttribute"/> 类的新实例。
    /// </summary>
    /// <param name="tokenManage">令牌管理服务接口的名称，用于获取和刷新访问令牌。</param>
    /// <param name="wrapInterface">包装器接口的名称，用于生成最终的HTTP客户端实现。</param>
    /// <example>
    /// <code>
    /// [HttpClientApiWrap(TokenManage = nameof(ITokenManager), WrapInterface = nameof(IMyApi))]
    /// public interface IMyApi { }
    /// </code>
    /// </example>
    public HttpClientApiWrapAttribute(string tokenManage, string wrapInterface)
    {
        WrapInterface = wrapInterface;
        TokenManage = tokenManage;
    }

    /// <summary>
    /// 获取或设置包装器接口的名称。
    /// </summary>
    /// <value>
    /// 包装器接口的名称，用于指定生成的HTTP客户端实现的接口类型。
    /// 如果未设置，则使用被标记的接口本身作为包装器接口。
    /// </value>
    /// <remarks>
    /// 该属性通常在需要将多个API接口包装到一个统一接口时使用。
    /// </remarks>
    public string? WrapInterface { get; set; } = string.Empty;

    /// <summary>
    /// 获取或设置令牌管理服务接口的名称。
    /// </summary>
    /// <value>
    /// 令牌管理服务接口的名称，该接口负责提供和管理访问令牌。
    /// 必须实现令牌的获取、刷新和验证功能。
    /// </value>
    /// <remarks>
    /// 在 .NET 8.0 及更高版本中，此属性为必填项。
    /// 令牌管理服务需要实现获取令牌、刷新令牌和验证令牌有效性的方法。
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// 在 .NET 8.0 及更高版本中，如果未设置此属性则抛出此异常。
    /// </exception>
    public
#if NET7_0_OR_GREATER
        required
#endif
  string? TokenManage
    { get; set; }
}
