// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;


#pragma warning disable CS0618 // R5/X6: Obsolete dual-read fallback base
namespace Mud.Feishu.Abstractions.Extensions;

/// <summary>
/// 跨 Options 一致性护栏（B13）。
/// </summary>
/// <remarks>
/// <para>
/// <c>FeishuWebhookOptions</c>（Webhook 包）与 <c>RedisOptions</c>（Redis 包）互不可见，
/// 不能在单一库内做类型耦合的跨包校验。本类型提供：
/// </para>
/// <list type="number">
///   <item>共享不变量常量与纯函数校验；</item>
///   <item>宿主注入的 <see cref="FeishuConfigurationConsistencyOptions"/>（容差/环境策略）；</item>
///   <item>对 <see cref="FeishuDeduplicationOptions"/> 的 IValidateOptions（NonceTtl 等）。</item>
/// </list>
/// <para>
/// 真正的「Redis NonceTtl vs Webhook TimestampTolerance」组合校验由宿主同时引用两包后，
/// 把 Webhook 容差写入 <see cref="FeishuConfigurationConsistencyOptions.TimestampToleranceSeconds"/>，
/// 再调用 <see cref="AddFeishuConfigurationConsistencyChecks"/> 完成。
/// </para>
/// </remarks>
public static class FeishuConfigurationConsistency
{
    /// <summary>时间戳容差上限（秒），与 Webhook 侧契约一致</summary>
    public const int MaxTimestampToleranceSeconds = 300;

    /// <summary>Nonce TTL 下限建议（秒），与 Webhook 默认容差对齐</summary>
    public const int DefaultNonceTtlSeconds = 300;

    /// <summary>
    /// 校验 NonceTtl ≥ TimestampToleranceSeconds（WHF-03）。
    /// </summary>
    public static ValidateOptionsResult ValidateNonceTtlAgainstTolerance(
        TimeSpan nonceTtl,
        int timestampToleranceSeconds,
        bool failOnValidationError)
    {
        if (timestampToleranceSeconds <= 0)
            return ValidateOptionsResult.Success;

        if (timestampToleranceSeconds > MaxTimestampToleranceSeconds)
        {
            var capMsg = $"TimestampToleranceSeconds({timestampToleranceSeconds}) 不能超过 {MaxTimestampToleranceSeconds} 秒";
            return failOnValidationError ? ValidateOptionsResult.Fail(capMsg) : ValidateOptionsResult.Success;
        }

        if (nonceTtl < TimeSpan.FromSeconds(timestampToleranceSeconds))
        {
            var msg =
                $"NonceTtl({nonceTtl}) 必须 >= TimestampToleranceSeconds({timestampToleranceSeconds})（WHF-03）——" +
                "否则 Nonce 过期后、时间戳容差窗口结束前存在重放窗口";
            return failOnValidationError ? ValidateOptionsResult.Fail(msg) : ValidateOptionsResult.Success;
        }

        return ValidateOptionsResult.Success;
    }

    /// <summary>
    /// 注册跨配置一致性检查（须在宿主同时拥有两侧配置值时调用）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configure">配置一致性选项（容差、是否 Fail）</param>
    public static IServiceCollection AddFeishuConfigurationConsistencyChecks(
        this IServiceCollection services,
        Action<FeishuConfigurationConsistencyOptions> configure)
    {
        if (configure is null)
            throw new ArgumentNullException(nameof(configure));

        services.AddOptions<FeishuConfigurationConsistencyOptions>().Configure(configure);
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<FeishuDeduplicationOptions>, FeishuDeduplicationNonceConsistencyValidator>());
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<DeduplicationOptions>, DeduplicationOptionsConsistencyValidator>());
        return services;
    }
}

/// <summary>跨配置一致性选项（由宿主注入 Webhook 侧事实）</summary>
public class FeishuConfigurationConsistencyOptions
{
    /// <summary>
    /// Webhook 时间戳容差（秒）。null 表示宿主未提供 Webhook 配置，跳过跨包校验。
    /// </summary>
    public int? TimestampToleranceSeconds { get; set; }

    /// <summary>
    /// true：非法组合启动失败；false：仅允许通过（Dev）。
    /// Production 建议 true。
    /// </summary>
    public bool FailOnValidationError { get; set; } = true;

    /// <summary>
    /// 统一节中声明的 Nonce TTL（秒）。null 时回退默认 300。
    /// </summary>
    public int? NonceTtlSeconds { get; set; }
}

/// <summary>对 FeishuDeduplicationOptions 的 Nonce 容差一致性校验</summary>
internal sealed class FeishuDeduplicationNonceConsistencyValidator : IValidateOptions<FeishuDeduplicationOptions>
{
    private readonly IOptionsMonitor<FeishuConfigurationConsistencyOptions> _consistency;

    public FeishuDeduplicationNonceConsistencyValidator(
        IOptionsMonitor<FeishuConfigurationConsistencyOptions> consistency)
    {
        _consistency = consistency;
    }

    public ValidateOptionsResult Validate(string? name, FeishuDeduplicationOptions options)
    {
        var c = _consistency.CurrentValue;
        if (c.TimestampToleranceSeconds is null)
            return ValidateOptionsResult.Success;

        var nonceTtl = options.Nonce?.Ttl
                       ?? TimeSpan.FromSeconds(c.NonceTtlSeconds ?? FeishuConfigurationConsistency.DefaultNonceTtlSeconds);
        return FeishuConfigurationConsistency.ValidateNonceTtlAgainstTolerance(
            nonceTtl, c.TimestampToleranceSeconds.Value, c.FailOnValidationError);
    }
}

/// <summary>对 DeduplicationOptions（旧键）的 Nonce 相关提示性校验（无容差时跳过）</summary>
internal sealed class DeduplicationOptionsConsistencyValidator : IValidateOptions<DeduplicationOptions>
{
    private readonly IOptionsMonitor<FeishuConfigurationConsistencyOptions> _consistency;

    public DeduplicationOptionsConsistencyValidator(
        IOptionsMonitor<FeishuConfigurationConsistencyOptions> consistency)
    {
        _consistency = consistency;
    }

    public ValidateOptionsResult Validate(string? name, DeduplicationOptions options)
    {
        // DeduplicationOptions 不承载 NonceTtl；保留扩展点以便宿主组合层扩展
        return ValidateOptionsResult.Success;
    }
}
