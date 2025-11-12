
namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式 PUT 请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class PutAttribute : HttpMethodAttribute
{
    /// <summary>
    ///     <inheritdoc cref="PutAttribute" />
    /// </summary>
    /// <param name="requestUri">请求地址</param>
    public PutAttribute(string? requestUri = null)
        : base(HttpMethod.Put, requestUri)
    {
    }
}