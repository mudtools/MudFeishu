// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Configuration;


/// <summary>
/// 单个应用的配置
/// </summary>
/// <remarks>
/// 应用配置支持继承全局配置：
/// - 数值字段（<see cref="TimestampToleranceSeconds"/>、<see cref="EventHandlingTimeoutMs"/>）设置为 <c>null</c>、<c>-1</c> 或 <c>0</c> 时继承全局配置，正整数使用应用级配置
/// - 布尔字段设置为 <c>null</c> 时继承全局配置，设置具体值时使用应用级配置
/// </remarks>
public class FeishuAppWebhookOptions
{
    /// <summary>
    /// 应用键（用于标识应用）
    /// </summary>
    public string AppKey { get; set; } = string.Empty;

    /// <summary>
    /// 应用验证 Token
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// 事件加密 Key
    /// </summary>
    public string EncryptKey { get; set; } = string.Empty;

    /// <summary>
    /// 应用描述（可选）
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 时间戳容差范围（秒），默认 <c>null</c> 表示继承全局配置。
    /// <para>设置为正整数时使用应用级配置；设置为 <c>null</c>、<c>-1</c> 或 <c>0</c> 时继承全局 <c>TimestampToleranceSeconds</c>。</para>
    /// <para>推荐使用 <c>null</c> 表示继承（与其他可空字段一致）；<c>-1</c> 仍向后兼容但已弃用。</para>
    /// </summary>
    public int? TimestampToleranceSeconds { get; set; }

    /// <summary>
    /// 事件处理超时时间（毫秒），默认 <c>null</c> 表示继承全局配置。
    /// <para>设置为正整数时使用应用级配置；设置为 <c>null</c>、<c>-1</c> 或 <c>0</c> 时继承全局 <c>EventHandlingTimeoutMs</c>。</para>
    /// <para>推荐使用 <c>null</c> 表示继承（与其他可空字段一致）；<c>-1</c> 仍向后兼容但已弃用。</para>
    /// </summary>
    public int? EventHandlingTimeoutMs { get; set; }

    /// <summary>
    /// 是否强制验证请求头签名，默认 null 表示继承全局配置
    /// 设置为 true 或 false 时使用应用级配置，设置为 null 时继承全局 EnforceHeaderSignatureValidation
    /// </summary>
    public bool? EnforceHeaderSignatureValidation { get; set; }

    /// <summary>
    /// 是否启用事件处理异常捕获，默认 null 表示继承全局配置
    /// </summary>
    public bool? EnableExceptionHandling { get; set; }

    /// <summary>
    /// 是否启用性能监控，默认 null 表示继承全局配置
    /// </summary>
    public bool? EnablePerformanceMonitoring { get; set; }

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">配置无效时抛出</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(AppKey))
            throw new InvalidOperationException("AppKey 不能为空");

        if (string.IsNullOrWhiteSpace(VerificationToken))
            throw new InvalidOperationException("VerificationToken 不能为空");

        if (string.IsNullOrWhiteSpace(EncryptKey))
            throw new InvalidOperationException("EncryptKey 不能为空");

        if (EncryptKey.Length != 32)
            throw new InvalidOperationException("EncryptKey 长度必须为 32 字符");

        // TimestampToleranceSeconds: null/-1/0 表示继承全局配置，正整数表示应用级配置
        // 不需要验证负数，因为 -1 是合法的向后兼容特殊值

        // EventHandlingTimeoutMs: null/-1/0 表示继承全局配置，正整数表示应用级配置
        if (EventHandlingTimeoutMs.HasValue && EventHandlingTimeoutMs.Value < -1)
            throw new InvalidOperationException("EventHandlingTimeoutMs 不能小于 -1");
        if (EventHandlingTimeoutMs.HasValue && EventHandlingTimeoutMs.Value > 0 && EventHandlingTimeoutMs.Value < 1000)
            throw new InvalidOperationException("EventHandlingTimeoutMs 必须至少为 1000 毫秒");
    }

    /// <summary>
    /// 获取有效的时间戳容差（解析继承逻辑）
    /// </summary>
    /// <param name="globalValue">全局配置值</param>
    /// <returns>有效的时间戳容差秒数</returns>
    public int GetEffectiveTimestampTolerance(int globalValue) =>
        TimestampToleranceSeconds is > 0 ? TimestampToleranceSeconds.Value : globalValue;

    /// <summary>
    /// 获取有效的事件处理超时（解析继承逻辑）
    /// </summary>
    /// <param name="globalValue">全局配置值</param>
    /// <returns>有效的事件处理超时毫秒数</returns>
    public int GetEffectiveEventHandlingTimeout(int globalValue) =>
        EventHandlingTimeoutMs is > 0 ? EventHandlingTimeoutMs.Value : globalValue;

    /// <summary>
    /// 获取有效的异常处理配置（解析继承逻辑）
    /// </summary>
    /// <param name="globalValue">全局配置值</param>
    /// <returns>有效的异常处理配置</returns>
    public bool GetEffectiveEnableExceptionHandling(bool globalValue) =>
        EnableExceptionHandling ?? globalValue;

    /// <summary>
    /// 获取有效的性能监控配置（解析继承逻辑）
    /// </summary>
    /// <param name="globalValue">全局配置值</param>
    /// <returns>有效的性能监控配置</returns>
    public bool GetEffectiveEnablePerformanceMonitoring(bool globalValue) =>
        EnablePerformanceMonitoring ?? globalValue;

    /// <summary>
    /// 获取有效的请求头签名验证配置（解析继承逻辑）
    /// </summary>
    /// <param name="globalValue">全局配置值</param>
    /// <returns>有效的请求头签名验证配置</returns>
    public bool GetEffectiveEnforceHeaderSignatureValidation(bool globalValue) =>
        EnforceHeaderSignatureValidation ?? globalValue;

    /// <summary>
    /// 返回配置的字符串表示（用于调试和日志记录）
    /// </summary>
    public override string ToString()
    {
        var description = !string.IsNullOrEmpty(Description) ? $", Description: {Description}" : "";
        return $"FeishuAppWebhookOptions {{ AppKey: {AppKey}{description}, TimestampToleranceSeconds: {TimestampToleranceSeconds}, EventHandlingTimeoutMs: {EventHandlingTimeoutMs} }}";
    }
}
