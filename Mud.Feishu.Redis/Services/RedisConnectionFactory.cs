// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Redis.Configuration;
using StackExchange.Redis;

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// Redis 连接选项装配的单一出口（R2-26）。
/// </summary>
/// <remarks>
/// <para>
/// 从 <c>AddFeishuRedis</c> 抽出，使「连接串 → <see cref="ConfigurationOptions"/>」可被**单元测试**
/// 直接验证（无需真实连接）——TLS/超时/口令/ClientName 等映射此前只能靠端到端连接证明。
/// </para>
/// <para>
/// <b>为什么必须显式推导 TLS</b>：实测 StackExchange.Redis 3.3.0 中
/// <c>ConfigurationOptions.Parse("rediss://host:6380").Ssl == false</c> —— 该 API **不会**因
/// <c>rediss://</c> scheme 自动启用 TLS。若仅写 <c>config.Ssl = config.Ssl || options.Connection.Ssl</c>，
/// 则按 README 配置 <c>"ServerAddress": "rediss://…"</c>（且未显式设置 <c>Ssl=true</c>）的宿主
/// 会以**明文**连接 TLS 端口，表现为"必然连接失败"或以明文传输（R-13 声称的修复并未闭环）。
/// </para>
/// </remarks>
internal static class RedisConnectionFactory
{
    /// <summary>TLS scheme 前缀。</summary>
    internal const string RedissScheme = "rediss://";

    /// <summary>
    /// 由 <see cref="RedisOptions"/> 构建 <see cref="ConfigurationOptions"/>。
    /// </summary>
    /// <param name="options">Redis 配置</param>
    /// <returns>可直接用于 <c>ConnectionMultiplexer.Connect</c> 的配置</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> 为 null</exception>
    /// <remarks>
    /// 语义与 R1 ADR-7 §1 一致，仅补 TLS scheme 推导：
    /// <list type="bullet">
    /// <item>连接串形态（<c>host:port</c> / <c>redis://</c> / <c>rediss://</c> / 内联 <c>password=</c>）由
    /// <see cref="ConfigurationOptions.Parse(string)"/> 原生处理；</item>
    /// <item>显式配置项优先于连接串（<c>Password</c> 非空时覆盖内联口令）；</item>
    /// <item><c>Ssl</c>：连接串 scheme 推导 <b>||</b> 配置项（取或，任一为真即启用 TLS）。</item>
    /// </list>
    /// </remarks>
    public static ConfigurationOptions Build(RedisOptions options)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        var connection = options.Connection ?? new RedisConnectionOptions();
        var advanced = options.Advanced ?? new RedisAdvancedOptions();

        var config = ConfigurationOptions.Parse(connection.ServerAddress);
        config.ConnectTimeout = connection.ConnectTimeout;
        config.SyncTimeout = connection.SyncTimeout;
        // R2-26：scheme 推导必须显式（Parse 不会因 rediss:// 自动置 Ssl）
        config.Ssl = config.Ssl || connection.Ssl || RequiresSsl(connection.ServerAddress);
        config.Password = string.IsNullOrEmpty(connection.Password) ? config.Password : connection.Password;
        config.AllowAdmin = advanced.AllowAdmin;
        config.AbortOnConnectFail = connection.AbortOnConnectFail;
        config.ConnectRetry = connection.ConnectRetry;
        config.DefaultDatabase = connection.DefaultDatabase;
        config.ClientName = advanced.ClientName ?? $"Feishu-Deduplicator-{Environment.MachineName}";

        return config;
    }

    /// <summary>
    /// 判断连接串是否要求 TLS（<c>rediss://</c> scheme，大小写不敏感）。
    /// </summary>
    /// <param name="serverAddress">连接串</param>
    /// <returns>需要 TLS 返回 <c>true</c></returns>
    public static bool RequiresSsl(string? serverAddress) =>
        !string.IsNullOrEmpty(serverAddress)
        && serverAddress!.StartsWith(RedissScheme, StringComparison.OrdinalIgnoreCase);
}
