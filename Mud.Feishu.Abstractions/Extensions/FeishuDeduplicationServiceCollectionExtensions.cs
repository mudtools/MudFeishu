// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Mud.Feishu.Abstractions.Configuration;


#pragma warning disable CS0618 // R5/X6: Obsolete dual-read fallback base — intentionally references DeduplicationOptions/EventDeduplicationOptions
namespace Mud.Feishu.Abstractions.Extensions;

/// <summary>
/// 统一事件去重配置注册扩展（C1/B4）。
/// </summary>
public static class FeishuDeduplicationServiceCollectionExtensions
{
    /// <summary>
    /// 注册统一去重配置：绑定 <c>FeishuDeduplication</c> 节（若存在）并启用 Profile/Validator。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="configuration">配置根；可为 null（仅注册 Options/Validator）</param>
    /// <param name="sectionName">配置节名，默认 FeishuDeduplication</param>
    public static IServiceCollection AddFeishuDeduplicationOptions(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = FeishuDeduplicationOptions.SectionName)
    {
        services.AddOptions<FeishuDeduplicationOptions>();
        services.TryAddSingleton<IValidateOptions<FeishuDeduplicationOptions>, FeishuDeduplicationOptionsValidator>();

        if (configuration is not null)
        {
            // 捕获传入 configuration，不覆盖宿主已注册的 IConfiguration
            var captured = configuration;
            var capturedSection = sectionName;
            services.TryAddEnumerable(ServiceDescriptor.Singleton<IConfigureOptions<FeishuDeduplicationOptions>>(
                new ConfigureNamedOptions<FeishuDeduplicationOptions>(Microsoft.Extensions.Options.Options.DefaultName,
                    options =>
                    {
                        var section = captured.GetSection(capturedSection);
                        if (!section.Exists())
                            return;
                        section.Bind(options);
                        options.IsConfiguredFromConfiguration = true;
                        options.Validate();
                    })));
        }
        else
        {
            services.TryAddSingleton<IConfigureOptions<FeishuDeduplicationOptions>, FeishuDeduplicationSectionBinder>();
        }

        return services;
    }

    /// <summary>
    /// 以代码方式应用去重 Profile（B4）。
    /// </summary>
    /// <param name="services">服务集合</param>
    /// <param name="profile">Default / HighReliability / HighAvailability</param>
    /// <param name="overrideConfigure">字段级微调（覆盖 Profile 预设）</param>
    public static IServiceCollection AddFeishuDeduplicationOptions(
        this IServiceCollection services,
        DeduplicationProfile profile,
        Action<FeishuDeduplicationOptions>? overrideConfigure = null)
    {
        services.AddOptions<FeishuDeduplicationOptions>();
        services.TryAddSingleton<IValidateOptions<FeishuDeduplicationOptions>, FeishuDeduplicationOptionsValidator>();
        services.TryAddSingleton<IConfigureOptions<FeishuDeduplicationOptions>, FeishuDeduplicationSectionBinder>();

        services.PostConfigure<FeishuDeduplicationOptions>(options =>
        {
            options.Profile = profile switch
            {
                DeduplicationProfile.HighReliability => FeishuDeduplicationOptions.ProfileHighReliability,
                DeduplicationProfile.HighAvailability => FeishuDeduplicationOptions.ProfileHighAvailability,
                _ => FeishuDeduplicationOptions.ProfileDefault
            };
            overrideConfigure?.Invoke(options);
            options.Validate();
        });

        return services;
    }

    /// <summary>
    /// 应用既有 <see cref="DeduplicationOptions"/> 预设到统一节（Profile DI 兼容入口）。
    /// </summary>
    public static IServiceCollection AddFeishuDeduplicationOptions(
        this IServiceCollection services,
        DeduplicationOptions preset,
        Action<FeishuDeduplicationOptions>? overrideConfigure = null)
    {
        if (preset is null)
            throw new ArgumentNullException(nameof(preset));

        var profile = DeduplicationProfile.Default;
        if (preset.CacheExpiration == DeduplicationOptions.HighReliability.CacheExpiration
            && preset.ProcessingTimeout == DeduplicationOptions.HighReliability.ProcessingTimeout)
        {
            profile = DeduplicationProfile.HighReliability;
        }
        else if (preset.CacheExpiration == DeduplicationOptions.HighAvailability.CacheExpiration
                 && preset.ProcessingTimeout == DeduplicationOptions.HighAvailability.ProcessingTimeout)
        {
            profile = DeduplicationProfile.HighAvailability;
        }

        return services.AddFeishuDeduplicationOptions(profile, options =>
        {
            if (preset.CacheExpiration > TimeSpan.Zero)
                options.Event.Ttl = preset.CacheExpiration;
            if (preset.ProcessingTimeout > TimeSpan.Zero)
                options.Event.ProcessingTimeout = preset.ProcessingTimeout;
            if (preset.CleanupInterval > TimeSpan.Zero)
                options.Event.CleanupInterval = preset.CleanupInterval;
            if (!string.IsNullOrEmpty(preset.KeyPrefix))
                options.Event.KeyPrefix = preset.KeyPrefix;
            if (preset.MaxCacheSize > 0)
                options.Event.MaxCacheSize = preset.MaxCacheSize;
            overrideConfigure?.Invoke(options);
        });
    }
}

/// <summary>去重 Profile 枚举（B4）</summary>
public enum DeduplicationProfile
{
    /// <summary>默认</summary>
    Default = 0,

    /// <summary>高可靠性（更长 TTL、更短处理超时；分布式失败语义以实际消费点为准）</summary>
    HighReliability = 1,

    /// <summary>高可用（标准 TTL、更长处理超时）</summary>
    HighAvailability = 2
}

/// <summary>FeishuDeduplication 节绑定器：节存在时标记 IsConfiguredFromConfiguration</summary>
internal sealed class FeishuDeduplicationSectionBinder : IConfigureOptions<FeishuDeduplicationOptions>
{
    private readonly IConfiguration? _configuration;

    public FeishuDeduplicationSectionBinder(IServiceProvider serviceProvider)
    {
        _configuration = serviceProvider.GetService<IConfiguration>();
    }

    public void Configure(FeishuDeduplicationOptions options)
    {
        if (_configuration is null)
            return;

        var section = _configuration.GetSection(FeishuDeduplicationOptions.SectionName);
        if (!section.Exists())
            return;

        section.Bind(options);
        options.IsConfiguredFromConfiguration = true;
        options.Validate();
    }
}

/// <summary>FeishuDeduplicationOptions 校验器</summary>
internal sealed class FeishuDeduplicationOptionsValidator : IValidateOptions<FeishuDeduplicationOptions>
{
    public ValidateOptionsResult Validate(string? name, FeishuDeduplicationOptions options)
    {
        try
        {
            options.Validate();
        }
        catch (InvalidOperationException ex)
        {
            return ValidateOptionsResult.Fail(ex.Message);
        }

        // TMA2-20：Distributed 且配置了前缀时，三前缀不得相同
        if (string.Equals(options.Mode, FeishuDeduplicationOptions.ModeDistributed, StringComparison.OrdinalIgnoreCase))
        {
            var prefixes = new[]
            {
                options.Event?.KeyPrefix,
                options.Nonce?.KeyPrefix,
                options.SeqId?.KeyPrefix
            }.Where(p => !string.IsNullOrEmpty(p)).Select(p => p!).ToList();

            if (prefixes.Count >= 2 && prefixes.Distinct(StringComparer.Ordinal).Count() == 1)
            {
                return ValidateOptionsResult.Fail(
                    "FeishuDeduplication 分布式模式下 Event/Nonce/SeqId KeyPrefix 不得相同（TMA2-20 多租户隔离）");
            }
        }

        return ValidateOptionsResult.Success;
    }
}
