// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 飞书应用配置构建器
/// </summary>
/// <remarks>
/// 提供流式API来构建飞书应用配置列表。
/// 支持链式调用，简化代码配置的编写。
/// </remarks>
/// <example>
/// <code>
/// var builder = new FeishuAppConfigBuilder();
/// builder.AddApp("default", "cli_xxx", "dsk_xxx", isDefault: true)
///        .AddApp("hr-app", "cli_yyy", "dsk_yyy", opt =>
///        {
///            opt.TimeOut = 45;
///            opt.RetryCount = 5;
///        });
/// var configs = builder.Build();
/// </code>
/// </example>
public class FeishuAppConfigBuilder
{
    /// <summary>
    /// 应用配置列表
    /// </summary>
    private readonly List<FeishuAppConfig> _configs = new();

    /// <summary>
    /// 添加飞书应用配置
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <param name="appId">飞书应用ID</param>
    /// <param name="appSecret">飞书应用密钥</param>
    /// <param name="isDefault">是否为默认应用</param>
    /// <param name="configure">额外的配置选项</param>
    /// <returns>配置构建器实例，支持链式调用</returns>
    /// <exception cref="ArgumentException">当应用键已存在时抛出</exception>
    /// <remarks>
    /// 添加一个新的飞书应用配置。
    /// 如果应用键已存在，会抛出异常。
    /// </remarks>
    public FeishuAppConfigBuilder AddApp(
        string appKey,
        string appId,
        string appSecret,
        bool isDefault = false,
        Action<FeishuAppConfig>? configure = null)
    {
        if (_configs.Any(c => c.AppKey.Equals(appKey, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"应用 {appKey} 已存在", nameof(appKey));

        var config = new FeishuAppConfig
        {
            AppKey = appKey,
            AppId = appId,
            AppSecret = appSecret,
            IsDefault = isDefault
        };

        configure?.Invoke(config);
        config.Validate();

        _configs.Add(config);

        return this;
    }

    /// <summary>
    /// 添加默认应用（便捷方法）
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <param name="appId">飞书应用ID</param>
    /// <param name="appSecret">飞书应用密钥</param>
    /// <param name="configure">额外的配置选项</param>
    /// <returns>配置构建器实例，支持链式调用</returns>
    /// <remarks>
    /// 这是一个便捷方法，等同于 AddApp(appKey, appId, appSecret, true, configure)。
    /// </remarks>
    public FeishuAppConfigBuilder AddDefaultApp(
        string appKey,
        string appId,
        string appSecret,
        Action<FeishuAppConfig>? configure = null)
    {
        return AddApp(appKey, appId, appSecret, true, configure);
    }

    /// <summary>
    /// 添加飞书应用配置（使用已有配置对象）
    /// </summary>
    /// <param name="config">应用配置对象</param>
    /// <returns>配置构建器实例，支持链式调用</returns>
    /// <exception cref="ArgumentException">当应用键已存在时抛出</exception>
    /// <remarks>
    /// 添加一个已构建好的飞书应用配置对象。
    /// </remarks>
    public FeishuAppConfigBuilder AddApp(FeishuAppConfig config)
    {
        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (_configs.Any(c => c.AppKey.Equals(config.AppKey, StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException($"应用 {config.AppKey} 已存在", nameof(config));

        config.Validate();
        _configs.Add(config);

        return this;
    }

    /// <summary>
    /// 移除应用配置
    /// </summary>
    /// <param name="appKey">应用键</param>
    /// <returns>配置构建器实例，支持链式调用</returns>
    /// <remarks>
    /// 从配置列表中移除指定的应用配置。
    /// </remarks>
    public FeishuAppConfigBuilder RemoveApp(string appKey)
    {
        _configs.RemoveAll(c => c.AppKey.Equals(appKey, StringComparison.OrdinalIgnoreCase));
        return this;
    }

    /// <summary>
    /// 清空所有配置
    /// </summary>
    /// <returns>配置构建器实例，支持链式调用</returns>
    /// <remarks>
    /// 清空所有已添加的应用配置。
    /// </remarks>
    public FeishuAppConfigBuilder Clear()
    {
        _configs.Clear();
        return this;
    }

    /// <summary>
    /// 构建配置列表
    /// </summary>
    /// <returns>应用配置列表</returns>
    /// <remarks>
    /// 返回所有已添加的应用配置。
    /// 如果没有应用且没有明确设置默认应用，会自动将第一个应用设为默认应用。
    /// </remarks>
    public List<FeishuAppConfig> Build()
    {
        // 如果没有默认应用且有应用，自动设置第一个为默认
        if (_configs.Count > 0 && !_configs.Any(c => c.IsDefault))
        {
            _configs[0].IsDefault = true;
        }

        return _configs;
    }
}
