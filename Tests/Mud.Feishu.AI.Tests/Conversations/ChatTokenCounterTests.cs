// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.Conversations;

namespace Mud.Feishu.AI.Tests.Conversations;

/// <summary>
/// 会话历史 token 计数（P2-5）：net8+ 走 Tiktoken 精确路径，
/// 估算回退路径的<b>中文密度校正</b>（原实现按 4 字符/token 会把中文低估约 3-4 倍）直接单测。
/// </summary>
/// <remarks>
/// 精确路径存在时 <see cref="ChatTokenCounter.Count"/> 不会走到估算分支，
/// 故估算比由 <c>internal</c> 的 <c>Estimate</c> 直测（与 <c>IsExactCount</c> 同属可断言面）。
/// </remarks>
public class ChatTokenCounterTests
{
    [Fact]
    public void Estimate_ShouldCountChineseAsOneTokenPerChar()
    {
        ChatTokenCounter.Estimate(new string('多', 100)).Should().Be(100,
            "CJK 表意文字按 ~1 token/字符 估算（原实现按 4 字符/token 会低估约 4 倍，使中文会话的摘要窗口迟迟不触发）");
    }

    [Fact]
    public void Estimate_ShouldCountAsciiAsFourCharsPerToken()
    {
        ChatTokenCounter.Estimate(new string('x', 400)).Should().Be(100);
        ChatTokenCounter.Estimate(new string('x', 401)).Should().Be(101, "向上取整（残留字符占一个 token）");
    }

    [Fact]
    public void Estimate_ShouldMixCjkAndAscii()
    {
        // "多" = 1（CJK）；"abc" = ceil(3/4) = 1。
        ChatTokenCounter.Estimate("多abc").Should().Be(2);
    }

    [Fact]
    public void Estimate_ShouldBeZero_ForEmptyString()
    {
        ChatTokenCounter.Estimate(string.Empty).Should().Be(0);
    }

    [Fact]
    public void Count_Chinese_ShouldBeWithinFactorOfTwo()
    {
        // 不绑定具体实现（精确/估算均须落在此区间）：中文 100 字符约 50-200 token。
        var tokens = ChatTokenCounter.Count(new string('多', 100));

        tokens.Should().BeInRange(50, 200, "计数不得再低估一个数量级（那是 token 窗口失效的根因）");
    }

    [Fact]
    public void Count_ShouldReturnZero_ForNullOrEmpty()
    {
        ChatTokenCounter.Count(null).Should().Be(0);
        ChatTokenCounter.Count(string.Empty).Should().Be(0);
    }

    // ===== R2-4：精确计数可用性与降级可观测 =====

#if NET8_0_OR_GREATER
    [Fact]
    public void ExactCounting_ShouldBeAvailable_OnNet8OrGreater()
    {
        ChatTokenCounter.IsExactCount.Should().BeTrue(
            "net8+ 且已声明 Microsoft.ML.Tokenizers.Data.O200kBase 词表包后必须走 Tiktoken 精确计数；"
            + "若为 false 说明词表资源缺失，MaxHistoryTokens 维度会静默退化为字符估算"
            + $"（失败原因：{ChatTokenCounter.InitializationFailure ?? "<未记录>"}）");
    }

    [Fact]
    public void InitializationFailure_ShouldBeNull_WhenTokenizerAvailable()
    {
        // 精确计数可用时不得残留失败原因（可观测面与状态必须一致）。
        _ = ChatTokenCounter.IsExactCount;

        ChatTokenCounter.InitializationFailure.Should().BeNull();
    }

    [Fact]
    public void Count_ShouldMatchEstimatesOrderOfMagnitude_WithExactTokenizer()
    {
        // 精确路径下的中文计数应与估算同一量级（防止词表错配导致数量级偏差）。
        var exact = ChatTokenCounter.Count(new string('多', 100));

        exact.Should().BeInRange(50, 200);
    }
#else
    [Fact]
    public void ExactCounting_ShouldBeDisabled_OnLegacyTfm()
    {
        ChatTokenCounter.IsExactCount.Should().BeFalse("netstandard2.0 / net6.0 走字符估算回退（无 Tiktoken 依赖）");
    }
#endif

    [Fact]
    public void CountMessages_ShouldSumTextOnly()
    {
        // 不绑定具体计数实现：只断言「工具调用消息不贡献 token」与「按条累加」。
        var single = ChatTokenCounter.Count(new string('x', 400));
        var history = new List<ChatMessage>
        {
            new(ChatRole.User, new string('x', 400)),
            new(ChatRole.Assistant, new string('x', 400)),
            new(ChatRole.Tool, [new FunctionResultContent("call_1", "工具结果不计入")]),
        };

        ChatTokenCounter.CountMessages(history).Should().Be(single * 2, "只计文本（工具调用噪声不计入 token 窗口）");
    }
}
