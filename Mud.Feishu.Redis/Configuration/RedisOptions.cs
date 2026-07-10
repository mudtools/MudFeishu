// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.Abstractions;
using Mud.Feishu.Abstractions.Utilities;

namespace Mud.Feishu.Redis.Configuration;

/// <summary>
/// Redis 配置选项
/// </summary>
public class RedisOptions
{
    // Nonce 和 SeqID 键前缀已统一至 Consts.DefaultNonceKeyPrefix 和 Consts.DefaultSeqIdKeyPrefix

    /// <summary>
    /// Redis 连接字符串
    /// <para>示例: "localhost:6379", "127.0.0.1:6379", "rediss://secure.redis.com:6380"</para>
    /// </summary>
    public string ServerAddress { get; set; } = "localhost:6379";

    /// <summary>
    /// Redis 密码
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// 事件去重缓存过期时间，默认 48 小时
    /// <para><b>已弃用</b>：请改用 <see cref="Mud.Feishu.Abstractions.Configuration.DeduplicationOptions.CacheExpiration"/> 配置去重缓存过期时间。
    /// 此属性仍可用于 Redis 特定覆盖，但建议统一使用 DeduplicationOptions。</para>
    /// </summary>
    [Obsolete("请改用 DeduplicationOptions.CacheExpiration 统一配置去重缓存过期时间。此属性仍可使用但建议迁移。")]
    public TimeSpan EventCacheExpiration { get; set; } = TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// Nonce 有效期，默认 5 分钟
    /// </summary>
    public TimeSpan NonceTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// SeqID 去重缓存过期时间，默认 48 小时
    /// <para><b>已弃用</b>：请改用 <see cref="Mud.Feishu.Abstractions.Configuration.DeduplicationOptions.CacheExpiration"/> 配置去重缓存过期时间。
    /// 此属性仍可用于 Redis 特定覆盖，但建议统一使用 DeduplicationOptions。</para>
    /// </summary>
    [Obsolete("请改用 DeduplicationOptions.CacheExpiration 统一配置去重缓存过期时间。此属性仍可使用但建议迁移。")]
    public TimeSpan SeqIdCacheExpiration { get; set; } = TimeSpan.FromMilliseconds(Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs);

    /// <summary>
    /// 事件去重键前缀
    /// <para><b>已弃用</b>：请改用 <see cref="Mud.Feishu.Abstractions.Configuration.DeduplicationOptions.KeyPrefix"/> 统一配置去重键前缀。
    /// 此属性仍可用于 Redis 特定覆盖，但建议统一使用 DeduplicationOptions。</para>
    /// </summary>
    [Obsolete("请改用 DeduplicationOptions.KeyPrefix 统一配置去重键前缀。此属性仍可使用但建议迁移。")]
    public string EventKeyPrefix { get; set; } = Mud.Feishu.Abstractions.Consts.DefaultEventKeyPrefix;

    /// <summary>
    /// Nonce 去重键前缀
    /// </summary>
    public string NonceKeyPrefix { get; set; } = Mud.Feishu.Abstractions.Consts.DefaultNonceKeyPrefix;

    /// <summary>
    /// SeqID 去重键前缀
    /// </summary>
    public string SeqIdKeyPrefix { get; set; } = Mud.Feishu.Abstractions.Consts.DefaultSeqIdKeyPrefix;

    /// <summary>
    /// 连接超时时间，默认 5000 毫秒
    /// </summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>
    /// 同步超时时间，默认 5000 毫秒
    /// </summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>
    /// 是否启用 TLS/SSL，默认 false
    /// </summary>
    public bool Ssl { get; set; }

    /// <summary>
    /// 是否允许管理员操作，默认 false
    /// <para>仅在生产环境需要执行 FLUSHDB 等管理命令时才应启用</para>
    /// </summary>
    public bool AllowAdmin { get; set; } = false;

    /// <summary>
    /// 是否在连接失败时中止，默认 true
    /// </summary>
    public bool AbortOnConnectFail { get; set; } = true;

    /// <summary>
    /// 连接重试次数，默认 3 次
    /// </summary>
    public int ConnectRetry { get; set; } = Consts.DefaultRedisConnectRetry;

    /// <summary>
    /// 默认数据库索引
    /// </summary>
    public int? DefaultDatabase { get; set; }

    /// <summary>
    /// 客户端名称
    /// </summary>
    public string? ClientName { get; set; }

    /// <summary>
    /// 验证配置的有效性
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(ServerAddress))
            throw new InvalidOperationException("ServerAddress 不能为空");

        bool isValidFormat = ServerAddress.Contains(':') ||
                            ServerAddress.StartsWith("redis://", StringComparison.OrdinalIgnoreCase) ||
                            ServerAddress.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);

        if (!isValidFormat)
            throw new InvalidOperationException("ServerAddress 格式无效，应为 'host:port' 或 'redis://host:port' 或 'rediss://host:port'");

        if (ConnectTimeout < 1000)
            throw new InvalidOperationException("ConnectTimeout 必须至少为 1000 毫秒");

        if (SyncTimeout < 1000)
            throw new InvalidOperationException("SyncTimeout 必须至少为 1000 毫秒");

        if (ConnectRetry < 0)
            throw new InvalidOperationException("ConnectRetry 不能为负数");
    }

    /// <summary>
    /// 返回配置的字符串表示（敏感信息已掩码）
    /// </summary>
    public override string ToString()
    {
        return $"RedisOptions {{ ServerAddress: {ServerAddress}, Password: {SensitiveDataUtils.MaskSensitiveData(Password)}, DefaultDatabase: {DefaultDatabase?.ToString() ?? "默认"}, ConnectTimeout: {ConnectTimeout}ms, SyncTimeout: {SyncTimeout}ms, Ssl: {Ssl} }}";
    }
}
