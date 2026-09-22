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
    /// 应用键（诊断用标识）。由 <c>Apps</c> 字典的键**强制派生**，面向宿主只读。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>R5.2/X8：不要再配置这个键，也不要用它做路由。</b>
    /// </para>
    /// <para>
    /// 为什么改为只读：此前该属性仅在为空时回填（<c>if (string.IsNullOrEmpty(config.AppKey))</c>），
    /// 因此配置值可与字典键**分叉**——而路由只认字典键，分叉会让诊断日志与校验错误消息报出
    /// 与实际路由不一致的应用标识。现在 <see cref="FeishuWebhookOptions.Validate"/> 一律用字典键覆盖它。
    /// </para>
    /// <para>
    /// 为什么用 <c>internal set</c> 而不是 <c>[Obsolete]</c>：<c>[Obsolete]</c> 只产生**警告**，而且对
    /// <c>appsettings.json</c> 中的 <c>Apps:&lt;key&gt;:AppKey</c>（真正的误用入口）完全无效；
    /// <c>internal set</c> 则让宿主程序集在**编译期**就无法赋值，语义也更诚实（派生值 != 可配置项）。
    /// 与 R5.1 对 <c>FeishuDeduplicationOptions.IsConfiguredFromConfiguration</c> 的处置同构。
    /// </para>
    /// </remarks>
    public string AppKey { get; internal set; } = string.Empty;

    /// <summary>
    /// 应用验证 Token
    /// </summary>
    public string VerificationToken { get; set; } = string.Empty;

    /// <summary>
    /// 事件加密 Key
    /// </summary>
    public string EncryptKey { get; set; } = string.Empty;

    /// <summary>
    /// 期望的 AppId（WHF-R2/C7）。配置后，解密后的事件 AppId 与此值不匹配时拒绝处理。
    /// <para>防御 EncryptKey 复用误配置导致的跨应用事件串扰。未配置时行为不变。</para>
    /// </summary>
    public string? ExpectedAppId { get; set; }

    /// <summary>
    /// 时间戳容差范围（秒），默认 <c>null</c> 表示继承全局配置。
    /// <para>设置为正整数时使用应用级配置；设置为 <c>null</c>、<c>-1</c> 或 <c>0</c> 时继承全局 <c>TimestampToleranceSeconds</c>。</para>
    /// <para>推荐使用 <c>null</c> 表示继承（与其他可空字段一致）；<c>-1</c> 仍向后兼容但已弃用。</para>
    /// <para>WHF-03：应用级正整数同样受重放窗口上限约束（≤ 300 秒），由 <see cref="Validate"/> 强制。</para>
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
    /// <remarks>
    /// 安全约束（WHF-01）：生产环境（<c>IEnvironmentService.IsProduction == true</c>）该字段
    /// 不允许显式设置为 <c>false</c>——由 <see cref="FeishuWebhookOptionsValidator"/> 在启动期强制拒绝，
    /// 与 <see cref="GetEffectiveEnforceHeaderSignatureValidation"/> 的运行时继承解析路径保持一致。
    /// </remarks>
    public bool? EnforceHeaderSignatureValidation { get; set; }

    /// <summary>
    /// 是否启用事件处理异常捕获，默认 null 表示继承全局配置
    /// </summary>
    public bool? EnableExceptionHandling { get; set; }

    /// <summary>
    /// 验证配置有效性
    /// </summary>
    /// <exception cref="InvalidOperationException">配置无效时抛出</exception>
    public void Validate()
    {
        // R5.2/X8：不再校验 AppKey —— 它是从 Apps 字典键派生的诊断字段（见 AppKey 的 remarks），
        // 独立构造本对象时为空属正常；字典键的格式由 FeishuWebhookOptions.Validate 单独校验。

        if (string.IsNullOrWhiteSpace(VerificationToken))
            throw new InvalidOperationException("VerificationToken 不能为空");

        if (string.IsNullOrWhiteSpace(EncryptKey))
            throw new InvalidOperationException("EncryptKey 不能为空");

        if (EncryptKey.Length != 32)
            throw new InvalidOperationException("EncryptKey 长度必须为 32 字符");

        // TimestampToleranceSeconds: null/-1/0 表示继承全局配置，正整数表示应用级配置
        // 不需要验证负数，因为 -1 是合法的向后兼容特殊值
        // WHF-03：应用级正整数同样受重放窗口上限约束
        if (TimestampToleranceSeconds is > FeishuWebhookOptions.MaxTimestampToleranceSeconds)
            throw new InvalidOperationException(
                $"TimestampToleranceSeconds 不能超过 {FeishuWebhookOptions.MaxTimestampToleranceSeconds} 秒" +
                "（重放窗口上限，飞书官方建议 ≤60 秒）");

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
        return $"FeishuAppWebhookOptions {{ AppKey: {AppKey}, TimestampToleranceSeconds: {TimestampToleranceSeconds}, EventHandlingTimeoutMs: {EventHandlingTimeoutMs} }}";
    }
}
