// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Options;

namespace Mud.Feishu.Abstractions;

/// <summary>
/// <see cref="FeishuAppOptions"/> 的启动期校验器（R5/X1）。
/// </summary>
/// <remarks>
/// <para>
/// 为什么需要它：<see cref="FeishuAppOptions"/> 此前**从未绑定任何配置节**（仅
/// <c>services.AddOptions&lt;FeishuAppOptions&gt;()</c>），因此 <c>ContextRetireDelaySeconds</c> 的
/// 合法范围只在 <c>FeishuAppContextRetirement</c> 运行期被检查，且错误暴露得很晚。
/// 绑定落地后，这里把约束前移到**启动期**（fail-fast）。
/// </para>
/// <para>
/// 注意：本类型不参与用户配置面，因此不使用 <c>required</c> 成员（源生成配置绑定会以
/// <c>new T()</c> 构造，<c>required</c> 会导致 <c>CS9035</c>）。
/// </para>
/// </remarks>
public sealed class FeishuAppOptionsValidator : IValidateOptions<FeishuAppOptions>
{
    /// <summary>
    /// <see cref="FeishuAppOptions.ContextRetireDelaySeconds"/> 的下界（秒）。
    /// </summary>
    public const int MinContextRetireDelaySeconds = 1;

    /// <summary>
    /// <see cref="FeishuAppOptions.ContextRetireDelaySeconds"/> 的上界（秒）。
    /// </summary>
    public const int MaxContextRetireDelaySeconds = 3600;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, FeishuAppOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail($"{nameof(FeishuAppOptions)} 实例不能为 null。");
        }

        if (options.ContextRetireDelaySeconds < MinContextRetireDelaySeconds
            || options.ContextRetireDelaySeconds > MaxContextRetireDelaySeconds)
        {
            return ValidateOptionsResult.Fail(
                $"{nameof(FeishuAppOptions)}.{nameof(FeishuAppOptions.ContextRetireDelaySeconds)} " +
                $"必须在 {MinContextRetireDelaySeconds}–{MaxContextRetireDelaySeconds} 秒之间，" +
                $"当前值 {options.ContextRetireDelaySeconds}。" +
                "（该约束此前仅在运行期由 FeishuAppContextRetirement 抛出，R5 起在启动期快速失败）");
        }

        return ValidateOptionsResult.Success;
    }
}
