// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;

namespace Mud.Feishu.Abstractions.Conversations;

/// <summary>
/// 会话存储编解码工具：将过期时间戳与会话 JSON 合并编码为 <c>{expireTimestampMs}|{payload}</c> 格式。
/// </summary>
/// <remarks>
/// <para>
/// 与 <c>TokenStoreHelper</c> 的令牌存储格式约定保持同一惯例（Phase 0 §3.3）：
/// 存储值必须自带过期时间戳，<b>无时间戳视为 miss</b>（TMA-15 治理语义），由调用方重建会话。
/// 服务器端 TTL 只是清理手段，读取侧的时间戳才是有效性权威（双写兜底：Redis 键过期被
/// 外部淘汰/提前逐出时，读侧仍能拒绝过期载荷）。
/// </para>
/// </remarks>
public static class SessionStoreEncoding
{
    private const char Separator = '|';

    /// <summary>
    /// 编码会话载荷。
    /// </summary>
    /// <param name="payload">会话 JSON 字符串（不得为空）。</param>
    /// <param name="expireTimestampMs">过期时间戳（Unix 毫秒，必须为正）。</param>
    /// <returns>编码后的存储值。</returns>
    /// <exception cref="ArgumentException"><paramref name="payload"/> 为空。</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="expireTimestampMs"/> 非正。</exception>
    public static string Encode(string payload, long expireTimestampMs)
    {
        if (string.IsNullOrEmpty(payload))
            throw new ArgumentException("会话载荷不能为空", nameof(payload));

        if (expireTimestampMs <= 0)
            throw new ArgumentOutOfRangeException(nameof(expireTimestampMs),
                "过期时间戳必须为正数（Unix 毫秒），实际值: " + expireTimestampMs.ToString(CultureInfo.InvariantCulture));

        return $"{expireTimestampMs.ToString(CultureInfo.InvariantCulture)}{Separator}{payload}";
    }

    /// <summary>
    /// 解码会话存储值。
    /// </summary>
    /// <param name="storedValue">存储值。</param>
    /// <param name="payload">解码出的会话 JSON。</param>
    /// <param name="expireTimestampMs">解码出的过期时间戳（Unix 毫秒）。</param>
    /// <returns>
    /// 成功解码返回 <see langword="true"/>；无有效过期时间戳（旧格式/损坏数据/空值）返回
    /// <see langword="false"/>——按 TMA-15 语义视为 miss，调用方必须丢弃并重建会话。
    /// </returns>
    public static bool TryDecode(string? storedValue, out string? payload, out long expireTimestampMs)
    {
        payload = null;
        expireTimestampMs = 0;

        if (string.IsNullOrEmpty(storedValue))
            return false;

        // netstandard2.0 的 BCL 引用程序集不带 IsNullOrEmpty 的 NotNullWhen 注解，此处需显式断言。
        var value = storedValue!;
        var separatorIndex = value.IndexOf(Separator);
        var timestampText = separatorIndex > 0 ? value.Substring(0, separatorIndex) : string.Empty;
        if (!long.TryParse(
                timestampText,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out var expireMs)
            || expireMs <= 0)
        {
            return false;
        }

        payload = value.Substring(separatorIndex + 1);
        if (payload.Length == 0)
        {
            payload = null;
            return false;
        }

        expireTimestampMs = expireMs;
        return true;
    }
}
