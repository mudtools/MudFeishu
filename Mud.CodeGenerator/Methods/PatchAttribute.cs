
namespace Mud.CodeGenerator;

/// <summary>
///     HTTP 声明式 PUT 请求方式特性
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
internal sealed class PatchAttribute : HttpMethodAttribute
{
    /// <summary>
    ///     <inheritdoc cref="PatchAttribute" />
    /// </summary>
    /// <param name="requestUri">请求地址</param>
    public PatchAttribute(string? requestUri = null)
        : base(HttpMethod.Patch, requestUri)
    {
    }
}