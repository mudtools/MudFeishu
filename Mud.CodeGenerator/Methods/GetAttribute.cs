
namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式 GET 请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class GetAttribute : HttpMethodAttribute
{
    /// <summary>
    ///     <inheritdoc cref="GetAttribute" />
    /// </summary>
    /// <param name="requestUri">请求地址</param>
    public GetAttribute(string? requestUri = null)
        : base(HttpMethod.Get, requestUri)
    {
    }
}