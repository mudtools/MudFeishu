namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal class HttpMethodAttribute : Attribute
{
    /// <summary>
    ///     <inheritdoc cref="HttpMethodAttribute" />
    /// </summary>
    /// <param name="httpMethod">请求方式</param>
    /// <param name="requestUri">请求地址</param>
    public HttpMethodAttribute(HttpMethod httpMethod, string? requestUri = null)
    {
        HttpMethod = httpMethod;
        RequestUri = requestUri;
    }

    /// <summary>
    ///     请求方式
    /// </summary>
    public HttpMethod HttpMethod { get; set; }

    /// <summary>
    ///     请求地址
    /// </summary>
    public string? RequestUri { get; set; }
}