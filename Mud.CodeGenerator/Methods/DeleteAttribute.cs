namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式 DELETE 请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class DeleteAttribute : HttpMethodAttribute
{
    /// <summary>
    ///     <inheritdoc cref="DeleteAttribute" />
    /// </summary>
    /// <param name="requestUri">请求地址</param>
    public DeleteAttribute(string? requestUri = null)
        : base(HttpMethod.Delete, requestUri)
    {
    }
}