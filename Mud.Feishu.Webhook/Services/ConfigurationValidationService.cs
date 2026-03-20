// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utilities;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 配置验证服务
/// 负责验证飞书 Webhook 配置的有效性和完整性
/// </summary>
public class ConfigurationValidationService
{
    private readonly ILogger<ConfigurationValidationService> _logger;
    private readonly IEnvironmentService _environmentService;

    /// <summary>
    /// 初始化配置验证服务
    /// </summary>
    /// <param name="logger">日志记录器</param>
    /// <param name="environmentService">环境服务</param>
    public ConfigurationValidationService(
        ILogger<ConfigurationValidationService> logger,
        IEnvironmentService? environmentService = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environmentService = environmentService ?? new EnvironmentService();
    }

    /// <summary>
    /// 验证全局配置
    /// </summary>
    /// <param name="options">配置选项</param>
    /// <returns>验证结果</returns>
    public ConfigurationValidationResult ValidateGlobalConfiguration(FeishuWebhookOptions options)
    {
        var result = new ConfigurationValidationResult();

        try
        {
            if (options == null)
            {
                result.AddError("配置对象不能为空");
                return result;
            }

            // 验证基本配置
            ValidateBasicConfiguration(options, result);

            // 验证安全配置
            ValidateSecurityConfiguration(options, result);

            // 验证性能配置
            ValidatePerformanceConfiguration(options, result);

            // 验证多应用配置
            ValidateMultiAppConfiguration(options, result);

            _logger.LogDebug("全局配置验证完成，错误数: {ErrorCount}, 警告数: {WarningCount}",
                result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证全局配置时发生异常");
            result.AddError($"配置验证异常: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 验证应用特定配置
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <param name="appConfig">应用配置</param>
    /// <param name="globalOptions">全局配置</param>
    /// <returns>验证结果</returns>
    public ConfigurationValidationResult ValidateAppConfiguration(
        string appKey,
        FeishuAppWebhookOptions appConfig,
        FeishuWebhookOptions globalOptions)
    {
        var result = new ConfigurationValidationResult();

        try
        {
            if (string.IsNullOrEmpty(appKey))
            {
                result.AddError("应用键不能为空");
                return result;
            }

            if (appConfig == null)
            {
                result.AddError($"应用 {appKey} 的配置不能为空");
                return result;
            }

            // 验证应用键一致性
            if (!string.IsNullOrEmpty(appConfig.AppKey) && appConfig.AppKey != appKey)
            {
                result.AddWarning($"应用 {appKey} 的配置中 AppKey 字段 ({appConfig.AppKey}) 与实际应用键不一致");
            }

            // 验证验证 Token
            if (string.IsNullOrEmpty(appConfig.VerificationToken))
            {
                result.AddError($"应用 {appKey} 的验证 Token 不能为空");
            }
            else if (appConfig.VerificationToken.Length < 8)
            {
                result.AddWarning($"应用 {appKey} 的验证 Token 长度过短，建议至少 8 个字符");
            }

            // 验证加密密钥
            if (string.IsNullOrEmpty(appConfig.EncryptKey))
            {
                result.AddError($"应用 {appKey} 的加密密钥不能为空");
            }
            else if (appConfig.EncryptKey.Length != 32)
            {
                result.AddError($"应用 {appKey} 的加密密钥长度必须为 32 字符，当前长度: {appConfig.EncryptKey.Length}");
            }

            _logger.LogDebug("应用 {AppKey} 配置验证完成，错误数: {ErrorCount}, 警告数: {WarningCount}",
                appKey, result.Errors.Count, result.Warnings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "验证应用 {AppKey} 配置时发生异常", appKey);
            result.AddError($"应用 {appKey} 配置验证异常: {ex.Message}");
        }

        return result;
    }

    /// <summary>
    /// 验证基本配置
    /// </summary>
    private static void ValidateBasicConfiguration(FeishuWebhookOptions options, ConfigurationValidationResult result)
    {
        // 验证全局路由前缀
        if (string.IsNullOrEmpty(options.GlobalRoutePrefix))
        {
            result.AddWarning("全局路由前缀为空，将使用默认值");
        }

        // 验证请求体大小限制
        if (options.MaxRequestBodySize <= 0)
        {
            result.AddError("最大请求体大小必须大于 0");
        }
        else if (options.MaxRequestBodySize < 1024)
        {
            result.AddWarning("最大请求体大小过小，可能导致正常请求被拒绝");
        }
        else if (options.MaxRequestBodySize > 100 * 1024 * 1024) // 100MB
        {
            result.AddWarning("最大请求体大小过大，可能影响性能和安全性");
        }
    }

    /// <summary>
    /// 验证安全配置
    /// </summary>
    private void ValidateSecurityConfiguration(FeishuWebhookOptions options, ConfigurationValidationResult result)
    {
        // 验证时间戳容错范围
        if (options.TimestampToleranceSeconds < 0)
        {
            result.AddError("时间戳容错范围不能为负数");
        }
        else if (options.TimestampToleranceSeconds == 0)
        {
            result.AddWarning("时间戳容错范围为 0，可能导致正常请求被拒绝");
        }
        else if (options.TimestampToleranceSeconds > 3600) // 1小时
        {
            result.AddWarning("时间戳容错范围过大，可能增加重放攻击风险");
        }

        // 验证签名验证配置
        if (!options.EnforceHeaderSignatureValidation && _environmentService.IsProduction)
        {
            result.AddError("生产环境必须启用请求头签名验证");
        }

        if (!options.EnableBodySignatureValidation && _environmentService.IsProduction)
        {
            result.AddWarning("生产环境建议启用请求体签名验证");
        }

        // 验证 IP 白名单
        if (options.AllowedSourceIPs.Count > 0)
        {
            foreach (var ip in options.AllowedSourceIPs)
            {
                if (string.IsNullOrEmpty(ip))
                {
                    result.AddError("IP 白名单中不能包含空值");
                }
            }
        }
        else if (_environmentService.IsProduction)
        {
            result.AddWarning("生产环境建议配置 IP 白名单以提高安全性");
        }
    }

    /// <summary>
    /// 验证性能配置
    /// </summary>
    private static void ValidatePerformanceConfiguration(FeishuWebhookOptions options, ConfigurationValidationResult result)
    {
        // 验证事件处理超时
        if (options.EventHandlingTimeoutMs <= 0)
        {
            result.AddError("事件处理超时时间必须大于 0");
        }
        else if (options.EventHandlingTimeoutMs < 1000)
        {
            result.AddWarning("事件处理超时时间过短，可能导致正常请求超时");
        }
        else if (options.EventHandlingTimeoutMs > 300000) // 5分钟
        {
            result.AddWarning("事件处理超时时间过长，可能影响系统响应性");
        }

        // 验证并发限制
        if (options.MaxConcurrentEvents <= 0)
        {
            result.AddError("最大并发事件数必须大于 0");
        }
        else if (options.MaxConcurrentEvents > 1000)
        {
            result.AddWarning("最大并发事件数过大，可能影响系统稳定性");
        }
    }

    /// <summary>
    /// 验证多应用配置
    /// </summary>
    private void ValidateMultiAppConfiguration(FeishuWebhookOptions options, ConfigurationValidationResult result)
    {
        if (options.Apps.Count == 0)
        {
            result.AddInfo("未配置多应用，将使用单应用模式");
            return;
        }

        var appKeys = new HashSet<string>();
        var tokens = new HashSet<string>();
        var encryptKeys = new HashSet<string>();

        foreach (var kvp in options.Apps)
        {
            var appKey = kvp.Key;
            var appConfig = kvp.Value;

            // 检查重复的应用键
            if (!appKeys.Add(appKey))
            {
                result.AddError($"发现重复的应用键: {appKey}");
            }

            // 验证应用配置
            var appValidationResult = ValidateAppConfiguration(appKey, appConfig, options);
            result.Merge(appValidationResult);

            // 检查重复的 Token
            if (!string.IsNullOrEmpty(appConfig.VerificationToken))
            {
                if (!tokens.Add(appConfig.VerificationToken))
                {
                    result.AddWarning($"应用 {appKey} 的验证 Token 与其他应用重复");
                }
            }

            // 检查重复的加密密钥
            if (!string.IsNullOrEmpty(appConfig.EncryptKey))
            {
                if (!encryptKeys.Add(appConfig.EncryptKey))
                {
                    result.AddWarning($"应用 {appKey} 的加密密钥与其他应用重复");
                }
            }
        }

        _logger.LogInformation("多应用配置验证完成，共 {AppCount} 个应用", options.Apps.Count);
    }
}

/// <summary>
/// 配置验证结果
/// </summary>
public class ConfigurationValidationResult
{
    /// <summary>
    /// 错误列表
    /// </summary>
    public List<string> Errors { get; } = new();

    /// <summary>
    /// 警告列表
    /// </summary>
    public List<string> Warnings { get; } = new();

    /// <summary>
    /// 信息列表
    /// </summary>
    public List<string> Infos { get; } = new();

    /// <summary>
    /// 是否有效（无错误）
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// 添加错误
    /// </summary>
    public void AddError(string error) => Errors.Add(error);

    /// <summary>
    /// 添加警告
    /// </summary>
    public void AddWarning(string warning) => Warnings.Add(warning);

    /// <summary>
    /// 添加信息
    /// </summary>
    public void AddInfo(string info) => Infos.Add(info);

    /// <summary>
    /// 合并其他验证结果
    /// </summary>
    public void Merge(ConfigurationValidationResult other)
    {
        Errors.AddRange(other.Errors);
        Warnings.AddRange(other.Warnings);
        Infos.AddRange(other.Infos);
    }

    /// <summary>
    /// 获取摘要信息
    /// </summary>
    public string GetSummary()
    {
        return $"验证结果: {(IsValid ? "通过" : "失败")} - 错误: {Errors.Count}, 警告: {Warnings.Count}, 信息: {Infos.Count}";
    }
}