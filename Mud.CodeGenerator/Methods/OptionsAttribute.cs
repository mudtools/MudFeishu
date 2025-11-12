namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式 OPTIONS 请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class OptionsAttribute : HttpMethodAttribute
{
    /// <summary>
    ///     <inheritdoc cref="OptionsAttribute" />
    /// </summary>
    /// <param name="requestUri">请求地址</param>
    public OptionsAttribute(string? requestUri = null)
        : base(HttpMethod.Options, requestUri)
    {
    }
}