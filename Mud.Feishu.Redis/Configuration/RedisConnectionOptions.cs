// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.Configuration;

/// <summary>
/// Redis 连接嵌套配置（C7/R3）。
/// </summary>
public class RedisConnectionOptions
{
    /// <summary>Redis 连接字符串，默认 localhost:6379</summary>
    public string ServerAddress { get; set; } = "localhost:6379";

    /// <summary>Redis 密码</summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>是否启用 TLS，默认 false</summary>
    public bool Ssl { get; set; }

    /// <summary>默认数据库索引</summary>
    public int? DefaultDatabase { get; set; }

    /// <summary>连接超时（毫秒），默认 5000</summary>
    public int ConnectTimeout { get; set; } = 5000;

    /// <summary>同步超时（毫秒），默认 5000</summary>
    public int SyncTimeout { get; set; } = 5000;

    /// <summary>连接失败是否中止启动，默认 true</summary>
    public bool AbortOnConnectFail { get; set; } = true;

    /// <summary>连接重试次数，默认 3</summary>
    public int ConnectRetry { get; set; } = Mud.Feishu.Abstractions.Consts.DefaultRedisConnectRetry;
}

/// <summary>
/// Redis 高级连接旋钮（C7/R3）。生产开启 AllowAdmin 需变更单与审计。
/// </summary>
public class RedisAdvancedOptions
{
    /// <summary>是否允许管理员操作，默认 false</summary>
    public bool AllowAdmin { get; set; } = false;

    /// <summary>客户端名称；默认 Feishu-Deduplicator-{MachineName}，一般不必用户配置</summary>
    public string? ClientName { get; set; }
}
