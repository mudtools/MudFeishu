// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Abstractions;

/// <summary>
/// 全局常量。
/// </summary>
internal class Consts
{
    /// <summary>
    /// 用户ID类型的默认值。
    /// </summary>
    public const string User_Id_Type = "open_id";

    /// <summary>
    /// 部门ID类型的默认值。
    /// </summary>
    public const string Department_Id_Type = "open_department_id";

    /// <summary>
    /// 授权Header名称。
    /// </summary>
    public const string Authorization = "Authorization";

    /// <summary>
    /// 每页的默认10条记录。
    /// </summary>
    public const int PageSize_10 = 10;

    /// <summary>
    /// 每页的默认15条记录。
    /// </summary>
    public const int PageSize_15 = 15;

    /// <summary>
    /// 每页的默认20条记录。
    /// </summary>
    public const int PageSize_20 = 20;

    /// <summary>
    /// 每页的默认50条记录。
    /// </summary>
    public const int PageSize_50 = 50;

    /// <summary>
    /// 每页的默认500条记录。
    /// </summary>
    public const int PageSize_500 = 500;

    public const string HandlerNamespace = "Mud.Feishu.EventCallback";
    public const string InheritedFrom = "IdempotentFeishuEventHandler";
    public const string InheritedFromObject = "DefaultFeishuObjectEventHandler";
    public const string DefaultHeader = "FeishuEventHeader";

    /// <summary>
    /// 默认重试次数
    /// </summary>
    public const int DefaultRetryCount = 3;

    /// <summary>
    /// 默认重试延迟时间（毫秒）
    /// </summary>
    public const int DefaultRetryDelayMs = 1000;

    /// <summary>
    /// 默认熔断失败率阈值（百分比）
    /// </summary>
    public const int DefaultCircuitBreakerFailureThreshold = 20;

    /// <summary>
    /// 默认熔断采样窗口时间（秒）
    /// </summary>
    public const int DefaultCircuitBreakerSamplingDurationSeconds = 60;

    /// <summary>
    /// 默认熔断持续时间（秒）
    /// </summary>
    public const int DefaultCircuitBreakerBreakDurationSeconds = 60;

    /// <summary>
    /// 默认熔断最小吞吐量
    /// </summary>
    public const int DefaultCircuitBreakerMinimumThroughput = 10;

    /// <summary>
    /// 事件去重键默认前缀
    /// </summary>
    public const string DefaultEventKeyPrefix = "feishu:event:";

    /// <summary>
    /// 默认事件缓存过期时间（毫秒）：48 小时
    /// </summary>
    public const int DefaultCacheExpirationMs = 48 * 60 * 60 * 1000;

    /// <summary>
    /// 默认缓存清理间隔（毫秒）：5 分钟
    /// </summary>
    public const int DefaultCleanupIntervalMs = 5 * 60 * 1000;

    /// <summary>
    /// 默认处理中超时时间（毫秒）：10 分钟
    /// </summary>
    public const int DefaultProcessingTimeoutMs = 10 * 60 * 1000;

    /// <summary>
    /// 默认最大缓存容量
    /// </summary>
    public const int DefaultMaxCacheSize = 100000;
}
