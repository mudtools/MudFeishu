// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Webhook.Configuration;
using Mud.Feishu.Webhook.Utils;

namespace Mud.Feishu.Webhook.Services;

/// <summary>
/// 飞书事件时间戳验证器实现
/// 支持秒级和毫秒级时间戳的自动识别和验证
/// </summary>
/// <remarks>
/// 初始化时间戳验证器
/// </remarks>
/// <param name="logger">日志记录器</param>
/// <param name="optionsMonitor">配置监视器</param>
/// <param name="appKeyAccessor">应用键上下文访问器</param>
/// <param name="environmentService">环境服务</param>
public class TimestampValidator(
    ILogger<TimestampValidator> logger,
    IOptionsMonitor<FeishuWebhookOptions> optionsMonitor,
    IWebhookAppKeyAccessor appKeyAccessor,
    IEnvironmentService? environmentService = null) : WebhookValidatorBase(appKeyAccessor, logger), ITimestampValidator
{
    private readonly IOptionsMonitor<FeishuWebhookOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly IEnvironmentService _environmentService = environmentService ?? new EnvironmentService();

    /// <inheritdoc />
    public bool ValidateTimestamp(long timestamp, int? toleranceSeconds = null)
    {
        try
        {
            // 如果时间戳为 0，需要根据环境决定是否拒绝
            if (timestamp == 0)
            {
                if (_environmentService.IsProduction)
                {
                    // 生产环境拒绝时间戳为 0 的请求（安全要求）
                    Logger.LogError(
                        "时间戳为 0，拒绝请求（生产环境不允许跳过时间戳验证），AppKey: {AppKey}",
                        CurrentAppKey ?? "null");
                    return false;
                }

                // 开发/测试环境允许，但记录警告
                Logger.LogWarning(
                    "时间戳为 0，跳过时间戳验证（非生产环境，警告：此配置存在安全风险），AppKey: {AppKey}",
                    CurrentAppKey ?? "null");
                return true;
            }

            // 解析容错时间：优先使用显式传入的参数，为 null 时从配置读取
            int effectiveToleranceSeconds;
            if (toleranceSeconds.HasValue)
            {
                // 显式传入了容错值，直接使用
                effectiveToleranceSeconds = toleranceSeconds.Value;
            }
            else
            {
                // 未传入容错值，从配置中读取
                var options = _optionsMonitor.CurrentValue;

                // 多应用场景：尝试从应用特定配置获取
                if (!string.IsNullOrEmpty(CurrentAppKey))
                {
                    var appConfig = options.GetAppConfig(CurrentAppKey!);
                    if (appConfig != null)
                    {
                        // 优先使用应用特定配置，如果未设置（null/-1/0）则使用全局配置
                        effectiveToleranceSeconds = appConfig.TimestampToleranceSeconds is > 0
                            ? appConfig.TimestampToleranceSeconds.Value
                            : options.TimestampToleranceSeconds;
                        Logger.LogDebug("使用应用 {AppKey} 的时间戳容错配置: {ToleranceSeconds}秒",
                            CurrentAppKey, effectiveToleranceSeconds);
                    }
                    else
                    {
                        effectiveToleranceSeconds = options.TimestampToleranceSeconds;
                        Logger.LogDebug("应用配置不存在，使用全局时间戳容错配置: {ToleranceSeconds}秒", effectiveToleranceSeconds);
                    }
                }
                else
                {
                    // 单应用场景：使用全局配置
                    effectiveToleranceSeconds = options.TimestampToleranceSeconds;
                    Logger.LogDebug("使用全局时间戳容错配置: {ToleranceSeconds}秒", effectiveToleranceSeconds);
                }
            }

            // 验证配置有效性
            if (effectiveToleranceSeconds < 0)
            {
                Logger.LogError("时间戳容错配置无效: {ToleranceSeconds}秒，使用默认值 300 秒, AppKey: {AppKey}",
                    effectiveToleranceSeconds, CurrentAppKey ?? "null");
                effectiveToleranceSeconds = 300;
            }

            // 使用 TimestampHelper 转换时间戳
            var requestTime = TimestampHelper.ToDateTimeOffset(timestamp);
            var timestampType = TimestampHelper.IsMilliseconds(timestamp) ? "毫秒级" : "秒级";
            Logger.LogDebug("识别为{TimestampType}时间戳: {Timestamp} -> {RequestTime}, AppKey: {AppKey}",
                timestampType, timestamp, requestTime, CurrentAppKey ?? "null");

            var now = DateTimeOffset.UtcNow;
            var diff = Math.Abs((now - requestTime).TotalSeconds);

            var isValid = diff <= effectiveToleranceSeconds;

            if (!isValid)
            {
                Logger.LogWarning("时间戳超出容错范围: 请求时间 {RequestTime}, 当前时间 {CurrentTime}, 差异 {Diff}秒, 容错范围 {Tolerance}秒, AppKey: {AppKey}",
                    requestTime, now, diff, effectiveToleranceSeconds, CurrentAppKey ?? "null");
            }
            else
            {
                Logger.LogDebug("时间戳验证通过: 请求时间 {RequestTime}, 当前时间 {CurrentTime}, 差异 {Diff}秒, 容错范围 {Tolerance}秒, AppKey: {AppKey}",
                    requestTime, now, diff, effectiveToleranceSeconds, CurrentAppKey ?? "null");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "验证时间戳时发生错误, Timestamp: {Timestamp}, AppKey: {AppKey}",
                timestamp, CurrentAppKey ?? "null");
            return false;
        }
    }

}
