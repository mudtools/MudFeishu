// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.WebSocket;

/// <summary>
/// 事件去重配置
/// </summary>
public class EventDeduplicationOptions
{
    /// <summary>
    /// 默认缓存过期时间（毫秒）：48 小时
    /// </summary>
    public const int DefaultCacheExpirationMs = Mud.Feishu.Abstractions.Consts.DefaultCacheExpirationMs;

    /// <summary>
    /// 默认缓存清理间隔（毫秒）：5 分钟
    /// </summary>
    public const int DefaultCleanupIntervalMs = Mud.Feishu.Abstractions.Consts.DefaultCleanupIntervalMs;

    /// <summary>
    /// 去重模式，默认为内存去重
    /// </summary>
    public EventDeduplicationMode Mode { get; set; } = EventDeduplicationMode.InMemory;

    /// <summary>
    /// 缓存过期时间（毫秒），默认为48小时
    /// <para>建议设置为与飞书官方事件重试窗口期一致，避免长延时场景下的重复处理</para>
    /// </summary>
    public int CacheExpirationMs
    {
        get => _cacheExpirationMs;
        set => _cacheExpirationMs = Math.Max(60000, value);
    }
    private int _cacheExpirationMs = DefaultCacheExpirationMs;

    /// <summary>
    /// 缓存清理间隔（毫秒），默认为5分钟
    /// </summary>
    public int CleanupIntervalMs
    {
        get => _cleanupIntervalMs;
        set => _cleanupIntervalMs = Math.Max(60000, value);
    }
    private int _cleanupIntervalMs = DefaultCleanupIntervalMs;
}
