// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions.Configuration;

/// <summary>
/// <see cref="FeishuConversationOptions"/> 的启动期校验器（Options 管线接 <see cref="FeishuConversationOptions.Validate"/>）。
/// </summary>
/// <remarks>
/// net6+ 宿主经 <c>ValidateOnStart</c> 在启动期触发；netstandard2.0 无 ValidateOnStart，
/// 由首次解析 Options 时触发（与 FeishuAppOptionsValidator 同模式）。
/// </remarks>
public sealed class FeishuConversationOptionsValidator : IValidateOptions<FeishuConversationOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FeishuConversationOptions options)
    {
        if (options is null)
            return ValidateOptionsResult.Fail("FeishuConversationOptions 不能为 null");

        try
        {
            options.Validate();
            return ValidateOptionsResult.Success;
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }
    }
}
