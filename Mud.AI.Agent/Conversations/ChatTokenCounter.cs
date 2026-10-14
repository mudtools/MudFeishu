// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Conversations;

/// <summary>
/// 会话历史 token 计数器：net8+ 经<b>显式声明</b>的 <c>Microsoft.ML.Tokenizers</c>
/// （Tiktoken BPE，离线计数、无网络依赖）精确计数；初始化失败或低版本 TFM 回退字符估算
/// （降级路径：行为差异仅影响触发时机精度，不影响正确性）。估算比区分 CJK 与其他字符。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么是 public（H2 中立化决策）</b>：本类型是<b>产品中立</b>的可复用能力
/// （token 预算判定与任何生态无关），由 <c>Mud.Feishu.AI</c> 的 <c>ConversationSummarizer</c> /
/// <c>FeishuAgent</c> 消费。用 <c>InternalsVisibleTo</c> 会把产品程序集名焊进包族契约，
/// 故整类公开并由 <c>PublicAPI.Unshipped.txt</c> 登记。
/// </para>
/// <para>
/// <b>为什么不引入 <c>ILogger</c></b>：本类型是 <c>static</c>，拿不到 logger。初始化失败原因写入
/// <see cref="InitializationFailure"/>，由持有 logger 的宿主（<c>FeishuAgent</c> 构造期）读取并告警一次——
/// 可观测性由那条告警 + <see cref="InitializationFailure"/> 断言承载，不是静默丢弃。
/// </para>
/// </remarks>
public static class ChatTokenCounter
{
    /// <summary>编码器锚定模型（BPE 词表近似通用，仅估算精度差异）。</summary>
    private const string EncoderModel = "gpt-4o";

    /// <summary>估算回退：非 CJK 字符的平均字符/token 比。</summary>
    private const int CharsPerToken = 4;

    /// <summary>
    /// 编码器初始化失败原因（成功为 <see langword="null"/>）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 可观测面：编码器初始化失败（词表资源缺失等）原先被静默吞掉，
    /// 「token 预算实际走字符估算」这一依赖不匹配完全不可见。现保留原因供宿主构造期告警 + 用例断言。
    /// </para>
    /// <para>
    /// <b>为什么本属性跨 TFM 声明（而非 <c>#if</c> 包裹）</b>：公开面门禁（RS0016/RS0017）
    /// 按 <c>PublicAPI.Unshipped.txt</c> 单一清单比对，TFM 条件成员在低 TFM 上会被判为
    /// 「已声明但不存在」（RS0017）——那需要引入 per-TFM 清单文件这一新约定。
    /// 本属性在低 TFM 上恒为 <see langword="null"/>（该 TFM 根本没尝试初始化编码器，"无失败"是诚实取值），
    /// 与同样跨 TFM 的 <see cref="IsExactCount"/> 保持同形。
    /// </para>
    /// </remarks>
    public static string? InitializationFailure { get; private set; }

#if NET8_0_OR_GREATER
    private static readonly Lazy<Microsoft.ML.Tokenizers.Tokenizer?> Tiktoken = new(() =>
    {
        try
        {
            return Microsoft.ML.Tokenizers.TiktokenTokenizer.CreateForModel(EncoderModel);
        }
        catch (Exception ex)
        {
            // 编码器初始化失败（词表资源缺失等）→ 保留原因回退估算；不让 token 窗口成为启动风险。
            // 有意不在此处抛/记日志：失败原因写入 InitializationFailure，由宿主构造期读取并告警一次。
            InitializationFailure = $"{ex.GetType().Name}: {ex.Message}";
            return null;
        }
    });
#endif

    /// <summary>当前 TFM 上是否走精确计数（net8+ 且编码器初始化成功）；估算路径为 <see langword="false"/>。</summary>
    /// <remarks>可断言面：让「精确 or 估算」可被测试与诊断观测，替代不可观测的静默降级。</remarks>
    public static bool IsExactCount
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
    /// <param name="text">消息文本（可空/空串 ⇒ 0）。</param>
    /// <returns>token 数（精确或估算，取决于 <see cref="IsExactCount"/>）。</returns>
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
    /// <param name="history">会话历史。</param>
    /// <returns>累计 token 数。</returns>
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
    /// <param name="text">待估算文本（非空）。</param>
    /// <returns>估算 token 数。</returns>
    /// <remarks>
    /// <para>
    /// 原实现整体按「4 字符/token」估算，会把中文 token 量低估约 3-4 倍
    /// （cl100k 词表下常用汉字接近 1 token/字），导致中文会话的 token 维度摘要窗口迟迟不触发。
    /// 校正方向是「更接近真实 token 量」，故中文长会话的摘要触发会变早——这是正确行为。
    /// </para>
    /// <para>
    /// <b>为什么公开</b>：net8+ 且编码器可用时 <see cref="Count"/> 恒走精确路径，
    /// 估算分支在生产路径上不可达——故本方法必须可直接单测（与 <see cref="IsExactCount"/>
    /// 同属可断言面），否则"中文密度校正"这一修复会失去回归防线。
    /// </para>
    /// </remarks>
    public static int Estimate(string text)
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
