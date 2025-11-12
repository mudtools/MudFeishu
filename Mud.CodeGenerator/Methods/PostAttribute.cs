namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式 POST 请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class PostAttribute : HttpMethodAttribute
{
    /// <summary>
    ///     <inheritdoc cref="PostAttribute" />
    /// </summary>
    /// <param name="requestUri">请求地址</param>
    public PostAttribute(string? requestUri = null)
        : base(HttpMethod.Post, requestUri)
    {
    }
}