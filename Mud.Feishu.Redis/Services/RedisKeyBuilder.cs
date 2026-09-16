// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.Redis.Services;

/// <summary>
/// 统一键构造器——四类 Redis 键（事件/Nonce/SeqID/令牌）的唯一权威构造入口。
/// <para>解决 R-01/R-20/R-21 的共同根因：键由四处裸字符串拼接，前缀兜底规则三套，
/// 分隔符 <c>:</c> 未转义导致键碰撞。</para>
/// </summary>
internal static class RedisKeyBuilder
{
    /// <summary>
    /// 分隔符——所有键段之间使用 <c>:</c> 连接。
    /// </summary>
    private const string Separator = ":";

    /// <summary>
    /// 转义分隔符——将段内的 <c>:</c> 替换为 <c>\:</c>，杜绝跨段碰撞（R-20）。
    /// <para>例：appKey="a:b"、eventId="c" 与 appKey="a"、eventId="b:c" 产生不同的键。</para>
    /// </summary>
    private const string EscapedSeparator = @"\:";

    /// <summary>
    /// 单段最大长度（字节），防止超长键 DoS。
    /// </summary>
    private const int MaxSegmentLength = 256;

    /// <summary>
    /// 转义键段中的分隔符，使段内 <c>:</c> 不会与段间分隔符混淆。
    /// </summary>
    /// <param name="segment">原始段值</param>
    /// <returns>转义后的段值</returns>
    public static string Escape(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return string.Empty;

        return segment.Replace(Separator, EscapedSeparator);
    }

    /// <summary>
    /// 校验键段有效性：非空、长度不超 256。
    /// </summary>
    /// <param name="segment">待校验的段值</param>
    /// <param name="paramName">参数名（用于异常消息）</param>
    /// <exception cref="ArgumentException">段为空或长度超限</exception>
    public static void EnsureValid(string segment, string paramName)
    {
        if (string.IsNullOrEmpty(segment))
            throw new ArgumentException("键段不能为空", paramName);

        if (segment.Length > MaxSegmentLength)
            throw new ArgumentException($"键段长度 {segment.Length} 超过上限 {MaxSegmentLength}", paramName);
    }

    /// <summary>
    /// 组合键——强制非空前缀 + 转义各段 + <c>:</c> 分隔。
    /// <para>空前缀或以 <c>*</c> 开头的前缀抛 <see cref="InvalidOperationException"/>（R-01/R-21 护栏）。</para>
    /// </summary>
    /// <param name="prefix">键前缀（必须非空且不以 <c>*</c> 开头）</param>
    /// <param name="segments">零或多个键段</param>
    /// <returns>组合后的 Redis 键</returns>
    /// <exception cref="InvalidOperationException">前缀为空或以 <c>*</c> 开头</exception>
    public static string Combine(string prefix, params string?[] segments)
    {
        if (string.IsNullOrEmpty(prefix))
            throw new InvalidOperationException(
                "键前缀不能为空——空前缀会导致 pattern={prefix}* 退化为 * 并清空整个 Redis 库（R-01 护栏）");

        if (prefix.StartsWith("*"))
            throw new InvalidOperationException(
                "键前缀不能以 '*' 开头——通配符前缀会导致 SCAN 匹配所有键（R-01 护栏）");

        var parts = new List<string> { prefix };

        foreach (var segment in segments)
        {
            if (string.IsNullOrEmpty(segment))
                continue;

            if (segment!.Length > MaxSegmentLength)
                throw new InvalidOperationException(
                    $"键段长度 {segment.Length} 超过上限 {MaxSegmentLength}");

            parts.Add(Escape(segment));
        }

        return string.Join(Separator, parts);
    }
}
