// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2025
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.Configuration;

/// <summary>
/// Redis 配置选项
/// </summary>
public class RedisOptions
{
    /// <summary>
    /// Redis 连接字符串
    /// <para>示例: "localhost:6379", "127.0.0.1:6379,password=xxx", "rediss://secure.redis.com:6380"</para>
    /// </summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>
    /// 事件去重缓存过期时间，默认 24 小时
    /// </summary>
    public TimeSpan EventCacheExpiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Nonce 有效期，默认 5 分钟
    /// </summary>
    public TimeSpan NonceTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// SeqID 去重缓存过期时间，默认 24 小时
    /// </summary>
    public TimeSpan SeqIdCacheExpiration { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// 事件去重键前缀，默认为 "feishu:event:"
    /// </summary>
    public string EventKeyPrefix { get; set; } = "feishu:event:";

    /// <summary>
    /// Nonce 去重键前缀，默认为 "feishu:nonce:"
    /// </summary>
    public string NonceKeyPrefix { get; set; } = "feishu:nonce:";

    /// <summary>
    /// SeqID 去重键前缀，默认为 "feishu:seqid:"
    /// </summary>
    public string SeqIdKeyPrefix { get; set; } = "feishu:seqid:";

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
    /// 是否允许管理员操作，默认 true
    /// </summary>
    public bool AllowAdmin { get; set; } = true;
}
