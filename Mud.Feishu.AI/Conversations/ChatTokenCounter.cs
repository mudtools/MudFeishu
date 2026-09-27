// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Conversations;

/// <summary>
/// 会话历史 token 计数器（AI-FD-D12 P2D-3a）：net8+ 经 MAF 元包传递的
/// <c>Microsoft.ML.Tokenizers</c>（Tiktoken BPE，离线计数、无网络依赖）精确计数；
/// 初始化失败或低版本 TFM 回退「字符/4」估算（方案 §十一 风险表授权的降级路径，
/// 行为差异仅影响触发时机精度，不影响正确性）。
/// </summary>
internal static class ChatTokenCounter
{
    /// <summary>编码器锚定模型（BPE 词表近似通用，仅估算精度差异）。</summary>
    private const string EncoderModel = "gpt-4o";

    /// <summary>估算回退：平均字符/ token 比（中文语料按词元经验值取 4）。</summary>
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

    private static int Estimate(string text)
    {
        var tokens = text.Length / CharsPerToken;
        return text.Length % CharsPerToken == 0 ? tokens : tokens + 1;
    }
}
