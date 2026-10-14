// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Tools.Errors;

/// <summary>
/// 中立传输故障端口（<see cref="ITransportFault"/>）用例：错误分类器只吃<b>中立输入</b>
/// （异常 + 端口），不认识任何具体 HTTP 组件的异常类型。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么这些用例必须在本工程</b>（而不是继续挂在 <c>Mud.Feishu.AI.Tools.Tests</c>）：
/// 本工程只引用 <c>Mud.AI.Agent</c> / <c>Mud.AI.Abstractions</c>（无任何 Mud 组件、无飞书工程），
/// 用例能过即证明"中立路径不依赖产品类型"。P3 上游化（迁独立 Mud.AI 仓）时它们随实现一起走，
/// 覆盖不丢。适配层的产品侧证据（<c>Mud.HttpUtils.ApiException</c> → 端口）留在飞书侧用例。
/// </para>
/// <para>
/// <b>枚举数值</b>（<c>InlineData</c> 里不能用转换表达式）：
/// <c>Validation=0 / Policy=1 / Authorization=2 / Confirmation=3 / Retryable=4 / Api=5 / Internal=6 / ContentSafety=7</c>。
/// </para>
/// </remarks>
public class ToolErrorClassifierTransportFaultTests
{
    /// <summary>
    /// 端口命中 ⇒ 按 HTTP 状态段位分类（异常类型不再参与）。
    /// 用 <see cref="System.Net.Http.HttpRequestException"/> 作载体是有意的：它同时命中 BCL 兜底分支
    /// （<c>retryable/transient</c>），故 401/403 用例同时证明<b>端口优先于异常类型</b>。
    /// </summary>
    [Theory]
    [InlineData(401, 2)]
    [InlineData(403, 2)]
    [InlineData(429, 4)]
    [InlineData(500, 4)]
    [InlineData(503, 4)]
    [InlineData(400, 5)]
    [InlineData(null, 5)]
    public void Classify_WithTransportFault_ShouldMapByHttpStatusSegment(int? statusCode, int expectedCategory)
    {
        var classified = ToolErrorClassifier.Classify(
            new System.Net.Http.HttpRequestException("传输失败"),
            new StubTransportFault(statusCode));

        ((int)classified.Category).Should().Be(expectedCategory);
    }

    /// <summary>
    /// 端口缺席 ⇒ 落到 BCL 类型分支。这既是中立层的既有语义，也是"必须由适配层供给端口"的理由
    /// （产品传输异常多派生自 <c>HttpRequestException</c>，漏接适配会让 403 变成可重试）。
    /// </summary>
    [Fact]
    public void Classify_WithoutTransportFault_ShouldFallBackToBclTypeBranch()
    {
        var classified = ToolErrorClassifier.Classify(new System.Net.Http.HttpRequestException("网络中断"));

        classified.Category.Should().Be(ToolErrorCategory.Retryable);
        classified.Subtype.Should().Be(ToolErrorSubtype.Transient);
    }

    /// <summary>无传输语义的异常仍按类型分类（端口不得改变既有分支的语义）。</summary>
    [Fact]
    public void Classify_WithoutTransportFault_ShouldStillHonorExceptionTypes()
    {
        ToolErrorClassifier.Classify(new ArgumentException("缺少必填参数")).Category
            .Should().Be(ToolErrorCategory.Validation);
        ToolErrorClassifier.Classify(new TimeoutException("执行超时")).Category
            .Should().Be(ToolErrorCategory.Retryable);
        ToolErrorClassifier.Classify(new InvalidOperationException("业务失败")).Category
            .Should().Be(ToolErrorCategory.Internal);
    }

    /// <summary>测试自有端口实现——中立用例不得引用任何产品异常类型（那会让本工程重新依赖产品）。</summary>
    private sealed class StubTransportFault : ITransportFault
    {
        internal StubTransportFault(int? statusCode) => StatusCode = statusCode;

        /// <inheritdoc />
        public int? StatusCode { get; }
    }
}
