// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 会话历史 token 计数器（AI-FD-D12 P2D-3a）：net8+ 经<b>显式声明</b>的
/// <c>Microsoft.ML.Tokenizers</c>（Tiktoken BPE，离线计数、无网络依赖）精确计数；
/// 初始化失败或低版本 TFM 回退字符估算（方案 §十一 风险表授权的降级路径，
/// 行为差异仅影响触发时机精度，不影响正确性）。估算比区分 CJK 与其他字符（P2-5）。
/// </summary>
internal static class ChatTokenCounter
{
    /// <summary>编码器锚定模型（BPE 词表近似通用，仅估算精度差异）。</summary>
    private const string EncoderModel = "gpt-4o";

    /// <summary>估算回退：非 CJK 字符的平均字符/token 比。</summary>
    private const int CharsPerToken = 4;

#if NET8_0_OR_GREATER
    private static readonly Lazy<Microsoft.ML.Tokenizers.Tokenizer?> Tiktoken = new(() =>
    {
        try
        {
            return Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel(EncoderModel);
        }
        catch (Exception)
        {
            // 编码器初始化失败（资源缺失等）→ 静默回退估算；不让 token 窗口成为启动风险。
            return null;
        }
    });
#endif

    /// <summary>当前 TFM 上是否走精确计数（net8+ 且编码器初始化成功）；估算路径为 <see langword="false"/>。</summary>
    /// <remarks>P2-5 可断言面：让「精确 or 估算」可被测试与诊断观测，替代不可观测的静默降级。</remarks>
    internal static bool IsExactCount
    {
        get
        {
#if NET8_0_OR_GREATER
            return Tiktoken.Value is not null;
#else
            return false;
#endif
        }
    }

    /// <summary>统计单条消息文本 token 数。</summary>
    public static int Count(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

#if NET8_0_OR_GREATER
        var tokenizer = Tiktoken.Value;
        if (tokenizer is not null)
        {
            return tokenizer.CountTokens(text!);
        }
#endif

        return Estimate(text!);
    }

    /// <summary>统计会话历史总 token 数（只计文本；工具调用噪声不计）。</summary>
    public static int CountMessages(IEnumerable<Microsoft.Extensions.AI.ChatMessage> history)
    {
        var total = 0;
        foreach (var message in history)
        {
            total += Count(message.Text);
        }

        return total;
    }

    /// <summary>
    /// 估算 token 数（编码器不可用时的回退路径）：CJK 表意文字按 ~1 token/字符，
    /// 其余字符按 <see cref="CharsPerToken"/> 字符/token。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 原实现整体按「4 字符/token」估算，会把中文 token 量低估约 3-4 倍
    /// （cl100k 词表下常用汉字接近 1 token/字），导致中文会话的 token 维度摘要窗口迟迟不触发
    /// （P2-5 校正）。校正方向是「更接近真实 token 量」，故中文长会话的摘要触发会变早——这是正确行为。
    /// </para>
    /// <para>
    /// <b>可见性</b>：net8+ 且编码器可用时 <see cref="Count"/> 恒走精确路径，估算分支在测试中不可达——
    /// 故本方法为 <c>internal</c> 以直接单测（与 <see cref="IsExactCount"/> 同属 P2-5 可断言面）。
    /// </para>
    /// </remarks>
    internal static int Estimate(string text)
    {
        var cjk = 0;
        foreach (var ch in text)
        {
            if (IsCjk(ch))
            {
                cjk++;
            }
        }

        var other = text.Length - cjk;
        var otherTokens = other / CharsPerToken;
        if (other % CharsPerToken != 0)
        {
            otherTokens++;
        }

        return cjk + otherTokens;
    }

    /// <summary>是否 CJK 表意文字/假名/谚文/全角标点（按 ~1 token/字符 估算的字符集）。</summary>
    private static bool IsCjk(char ch)
        => (ch >= '\u2E80' && ch <= '\u9FFF')   // CJK 部首扩展 ~ 统一表意文字
           || (ch >= '\uAC00' && ch <= '\uD7AF') // 谚文音节
           || (ch >= '\uF900' && ch <= '\uFAFF') // CJK 兼容表意文字
           || (ch >= '\uFF00' && ch <= '\uFF60'); // 全角形式
}
