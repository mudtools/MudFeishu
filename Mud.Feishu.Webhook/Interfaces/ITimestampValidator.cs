// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026   
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Webhook;

/// <summary>
/// 飞书事件时间戳验证器接口
/// 负责验证请求时间戳的有效性，支持秒级和毫秒级时间戳的自动识别
/// </summary>
public interface ITimestampValidator
{
    /// <summary>
    /// 设置当前应用键（多应用场景）
    /// </summary>
    /// <param name="appKey">应用键，用于多应用场景下的上下文标识</param>
    void SetCurrentAppKey(string appKey);

    /// <summary>
    /// 验证时间戳是否在有效范围内
    /// </summary>
    /// <param name="timestamp">时间戳，支持秒级（10位）和毫秒级（13位）格式</param>
    /// <param name="toleranceSeconds">容错时间范围（秒），默认为 300 秒。当使用默认值时，会尝试从配置中读取</param>
    /// <returns>如果时间戳在有效范围内返回 true，否则返回 false</returns>
    /// <remarks>
    /// <para>时间戳格式自动识别：</para>
    /// <para>- 小于 10,000,000,000 的值被视为秒级时间戳</para>
    /// <para>- 大于等于 10,000,000,000 的值被视为毫秒级时间戳</para>
    /// <para>- 时间戳为 0 时允许跳过验证（兼容某些飞书请求类型）</para>
    /// <para>验证逻辑：|当前时间 - 请求时间| ≤ toleranceSeconds</para>
    /// <para>配置支持：</para>
    /// <para>- 多应用场景：优先使用应用特定配置，然后使用全局配置</para>
    /// <para>- 单应用场景：使用全局配置中的 TimestampToleranceSeconds</para>
    /// </remarks>
    bool ValidateTimestamp(long timestamp, int toleranceSeconds = 300);
}