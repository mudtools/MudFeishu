// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook.Utilities;

/// <summary>
/// 时间戳辅助工具类
/// 提供时间戳格式转换和标准化功能
/// </summary>
public static class TimestampHelper
{
    /// <summary>
    /// 毫秒级时间戳阈值（10位秒级 vs 13位毫秒级）
    /// </summary>
    private const long MillisecondThreshold = 10000000000L;

    /// <summary>
    /// 将时间戳转换为 DateTimeOffset
    /// 自动识别秒级（10位）和毫秒级（13位）时间戳
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <returns>DateTimeOffset 对象</returns>
    public static DateTimeOffset ToDateTimeOffset(long timestamp)
    {
        return timestamp < MillisecondThreshold
            ? DateTimeOffset.FromUnixTimeSeconds(timestamp)
            : DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
    }

    /// <summary>
    /// 将时间戳标准化为秒级
    /// 如果是毫秒级时间戳，则除以1000转换为秒级
    /// </summary>
    /// <param name="timestamp">原始时间戳</param>
    /// <returns>秒级时间戳</returns>
    public static long NormalizeToSeconds(long timestamp)
    {
        return timestamp < MillisecondThreshold ? timestamp : timestamp / 1000;
    }

    /// <summary>
    /// 将时间戳标准化为毫秒级
    /// 如果是秒级时间戳，则乘以1000转换为毫秒级
    /// </summary>
    /// <param name="timestamp">原始时间戳</param>
    /// <returns>毫秒级时间戳</returns>
    public static long NormalizeToMilliseconds(long timestamp)
    {
        return timestamp < MillisecondThreshold ? timestamp * 1000 : timestamp;
    }

    /// <summary>
    /// 检查时间戳是否为毫秒级
    /// </summary>
    /// <param name="timestamp">时间戳</param>
    /// <returns>如果是毫秒级返回 true，否则返回 false</returns>
    public static bool IsMilliseconds(long timestamp)
    {
        return timestamp >= MillisecondThreshold;
    }
}
