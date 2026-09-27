// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;

namespace Mud.Feishu.Abstractions.Conversations;

/// <summary>
/// 会话键构造的单一真相（D8 契约的会话版）。
/// <para>
/// 布局：<c>{keyPrefix}:{appKey}:conversation:{chat|user}:{subjectId}</c>。
/// 租户/应用维度（appKey）位于前缀之后（对齐 FU-2 教训：裸维度键会在多租户下串会话）；
/// 群聊走 <c>chat</c> 段、单聊走 <c>user</c> 段（路线图主线一 §4.3）。
/// 各段经转义（<c>\</c> <c>:</c> <c>*</c> <c>?</c> <c>[</c> <c>]</c>），含分隔符的
/// appKey/subjectId 不会与段间分隔符混淆，同时防止 Redis SCAN 模式通配注入。
/// </para>
/// <para>
/// 键布局变更约束（D8）：变更须同步 Memory/Redis 两条后端与等价、回灌测试；
/// 禁止在各存储点内联拼接会话键。
/// </para>
/// </summary>
public static class ConversationKeyBuilder
{
    /// <summary>默认键前缀（与令牌键前缀 <c>feishu</c> 同源风格）。</summary>
    public const string DefaultKeyPrefix = "feishu";

    /// <summary>键的固定形态段（诊断/解析用）。</summary>
    public const string ConversationSegment = "conversation";

    /// <summary>单段最大长度（字符），防止超长键 DoS（对齐 TokenKeyBuilder）。</summary>
    private const int MaxSegmentLength = 256;

    private const string Separator = ":";
    private const string ChatDimension = "chat";
    private const string UserDimension = "user";

    /// <summary>
    /// 构造会话键。
    /// </summary>
    /// <param name="appKey">应用唯一标识（空白视为 <c>default</c>，对齐 TokenKeyBuilder 兜底语义）。</param>
    /// <param name="scope">会话维度范围（群聊/单聊）。</param>
    /// <param name="subjectId">会话主体 ID（群聊为 chat_id，单聊为 user_id）。</param>
    /// <param name="keyPrefix">键前缀（空白回落 <see cref="DefaultKeyPrefix"/>）。</param>
    /// <returns>规范化后的会话键。</returns>
    /// <exception cref="ArgumentException"><paramref name="subjectId"/> 为空或任一段超长。</exception>
    public static string Build(string appKey, ConversationScope scope, string subjectId, string? keyPrefix = null)
    {
        if (string.IsNullOrWhiteSpace(subjectId))
            throw new ArgumentException("会话主体 ID 不能为空", nameof(subjectId));

        var safeAppKey = string.IsNullOrWhiteSpace(appKey) ? "default" : appKey;
        var safePrefix = string.IsNullOrWhiteSpace(keyPrefix) ? DefaultKeyPrefix : keyPrefix!;
        var dimension = scope.IsGroup ? ChatDimension : UserDimension;

        return string.Join(
            Separator,
            NormalizeSegment(safePrefix),
            NormalizeSegment(safeAppKey),
            ConversationSegment,
            dimension,
            NormalizeSegment(subjectId));
    }

    /// <summary>
    /// 尝试从完整会话键中解析回原始构成（回灌语义：解析结果再喂给 <see cref="Build"/> 得到逐字节相同的键）。
    /// </summary>
    /// <param name="key">完整会话键。</param>
    /// <param name="appKey">解析出的应用标识（已反转义）。</param>
    /// <param name="scope">解析出的会话维度。</param>
    /// <param name="subjectId">解析出的会话主体 ID（已反转义）。</param>
    /// <param name="keyPrefix">解析出的键前缀（已反转义）。</param>
    /// <returns>是否成功解析。</returns>
    public static bool TryParse(
        string? key,
        out string? appKey,
        out ConversationScope scope,
        out string? subjectId,
        out string? keyPrefix)
    {
        appKey = null;
        scope = default;
        subjectId = null;
        keyPrefix = null;

        if (string.IsNullOrEmpty(key))
            return false;

        // 逐段拆分：仅按未转义的 ':' 切分，段内 '\:' 视为字面量。
        var segments = SplitUnescaped(key!);
        if (segments.Count != 5)
            return false;

        if (segments[2] != ConversationSegment)
            return false;

        var isGroup = segments[3] switch
        {
            ChatDimension => true,
            UserDimension => false,
            _ => (bool?)null,
        };
        if (isGroup is null)
            return false;

        appKey = UnescapeSegment(segments[1]);
        subjectId = UnescapeSegment(segments[4]);
        keyPrefix = UnescapeSegment(segments[0]);
        scope = isGroup.Value
            ? ConversationScope.Group()
            : ConversationScope.P2P();
        return appKey.Length > 0 && subjectId!.Length > 0 && keyPrefix.Length > 0;
    }

    /// <summary>
    /// 组合「存储命名空间前缀 + 完整会话键」（Redis 后端环境隔离用）。
    /// </summary>
    /// <remarks>
    /// <paramref name="key"/> 必须是 <see cref="Build"/> 的产物（各段已转义）；
    /// <paramref name="namespacePrefix"/> 为宿主配置的环境命名空间（与 <c>TokenKeyPrefix</c>
    /// 同语义，允许形如 <c>feishu:conversation</c> 的多段前缀），本方法仅护栏校验非空与非通配。
    /// </remarks>
    /// <param name="namespacePrefix">存储命名空间前缀（如 <c>feishu:conversation</c>）。</param>
    /// <param name="key">完整会话键。</param>
    /// <returns>带命名空间的最终存储键。</returns>
    /// <exception cref="ArgumentException">前缀为空或以 <c>*</c> 开头。</exception>
    public static string ComposeNamespace(string namespacePrefix, string key)
    {
        if (string.IsNullOrWhiteSpace(namespacePrefix))
            throw new ArgumentException("存储命名空间前缀不能为空", nameof(namespacePrefix));

        if (namespacePrefix.StartsWith("*", StringComparison.Ordinal))
            throw new ArgumentException("存储命名空间前缀不能以 '*' 开头（R-01 护栏）", nameof(namespacePrefix));

        return $"{namespacePrefix}{Separator}{key}";
    }

    private static List<string> SplitUnescaped(string value)
    {
        var segments = new List<string>();
        var current = new StringBuilder(value.Length);
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] == '\\' && i + 1 < value.Length)
            {
                current.Append(value[i]).Append(value[i + 1]);
                i++;
                continue;
            }

            if (value[i] == ':')
            {
                segments.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(value[i]);
        }

        segments.Add(current.ToString());
        return segments;
    }

    /// <summary>
    /// 规范化键段：单遍扫描转义 <c>\</c> <c>:</c> <c>*</c> <c>?</c> <c>[</c> <c>]</c>（对齐 TokenKeyBuilder 语义）。
    /// </summary>
    private static string NormalizeSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return string.Empty;

        if (segment.Length > MaxSegmentLength)
            throw new ArgumentException(
                $"键段长度 {segment.Length} 超过上限 {MaxSegmentLength}", nameof(segment));

        var sb = new StringBuilder(segment.Length * 2);
        foreach (var ch in segment)
        {
            switch (ch)
            {
                case '\\':
                case ':':
                case '*':
                case '?':
                case '[':
                case ']':
                    sb.Append('\\');
                    break;
            }

            sb.Append(ch);
        }

        return sb.ToString();
    }

    private static string UnescapeSegment(string segment)
    {
        if (string.IsNullOrEmpty(segment))
            return string.Empty;

        var sb = new StringBuilder(segment.Length);
        for (var i = 0; i < segment.Length; i++)
        {
            if (segment[i] == '\\' && i + 1 < segment.Length)
            {
                sb.Append(segment[i + 1]);
                i++;
            }
            else
            {
                sb.Append(segment[i]);
            }
        }

        return sb.ToString();
    }
}
