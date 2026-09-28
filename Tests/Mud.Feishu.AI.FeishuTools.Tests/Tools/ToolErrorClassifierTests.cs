// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// 工具错误分类器测试（AI-FD-D12 P1D-2b）：纯函数表驱动——异常类型 × code 段位 → 分类；
/// 回填文本分类前缀可区分（授权拒绝 / 参数错误 / 可重试路径各有断言）。
/// </summary>
/// <remarks>ToolErrorKind 为 internal（宿主不可见），断言以数值形态进出 Theory。</remarks>
public class ToolErrorClassifierTests
{
    private static readonly int ApiErrorKind = (int)ToolErrorKind.ApiError;
    private static readonly int InvalidArgsKind = (int)ToolErrorKind.InvalidArgs;
    private static readonly int RetryableKind = (int)ToolErrorKind.Retryable;
    private static readonly int ForbiddenKind = (int)ToolErrorKind.Forbidden;

    [Fact]
    public void Classify_ArgumentException_ShouldBeInvalidArgs()
        => ((int)ToolErrorClassifier.Classify(new ArgumentException("缺少必填参数 chat_id")))
            .Should().Be(InvalidArgsKind);

    [Fact]
    public void Classify_NetworkExceptions_ShouldBeRetryable()
    {
        ((int)ToolErrorClassifier.Classify(new HttpRequestException("网络中断"))).Should().Be(RetryableKind);
        ((int)ToolErrorClassifier.Classify(new TimeoutException("执行超时"))).Should().Be(RetryableKind);
    }

    [Fact]
    public void Classify_ApiException_ShouldFollowHttpStatusSegment()
    {
        // Mud.HttpUtils.ApiException 携带 StatusCode：5xx/429 → 可重试，401/403 → 权限类。
        var serverError = new Mud.HttpUtils.ApiException(System.Net.HttpStatusCode.InternalServerError, "内部错误");
        ((int)ToolErrorClassifier.Classify(serverError)).Should().Be(RetryableKind);
    }

    [Fact]
    public void Classify_UnknownException_ShouldBeApiError()
        => ((int)ToolErrorClassifier.Classify(new InvalidOperationException("业务失败"))).Should().Be(ApiErrorKind);

    /// <summary>
    /// 飞书业务 code → 分类（数值形态进出 Theory：<c>ToolErrorKind</c> 是 internal，且枚举值不能在
    /// <c>InlineData</c> 里用转换表达式）。
    /// </summary>
    /// <remarks>
    /// 数值：<c>Retryable=0 / InvalidArgs=1 / Forbidden=2 / NeedsConfirmation=3 / ApiError=4</c>。
    /// <b>AT-B12 新增 <c>NeedsConfirmation=3</c> 后 <c>ApiError</c> 由 3 变 4</b>——本表随之更新
    /// （这正是"三态语义不得混用"的断言面）。
    /// </remarks>
    [Theory]
    [InlineData(99991663, 2)]
    [InlineData(99991661, 2)]
    [InlineData(230001, 4)]
    [InlineData(null, 4)]
    public void ClassifyCode_ShouldMapPermissionCodes(int? code, int expectedKind)
        => ((int)ToolErrorClassifier.ClassifyCode(code)).Should().Be(expectedKind);

    /// <summary>
    /// 三态语义各有一套文案：待确认<b>既不是</b> forbidden（放弃）<b>也不是</b> invalid_args（改参重试）。
    /// </summary>
    [Fact]
    public void StructuredError_NeedsConfirmation_ShouldHaveItsOwnSemantics()
    {
        var needsConfirmation = FeishuToolBinding.StructuredError(
            "approval.create_instance", ToolErrorKind.NeedsConfirmation, "需要用户批准后才可发起");

        needsConfirmation.Should().Contain("(needs_confirmation)");
        needsConfirmation.Should().Contain("需要用户确认");
        needsConfirmation.Should().NotContain("(forbidden)");
        needsConfirmation.Should().NotContain("(invalid_args)");
        needsConfirmation.Should().NotContain("请修正参数", "待确认 ≠ 参数错：误导会让模型陷入无意义的重试循环");

        ((int)ToolErrorKind.NeedsConfirmation).Should().NotBe(ForbiddenKind,
            "三态必须是互不相同的枚举成员（否则 switch 分支会互相吞并）");
    }

    [Fact]
    public void StructuredError_ShouldDistinguishForbidden_FromInvalidArgs()
    {
        var forbidden = FeishuToolBinding.StructuredError(
            "bitable.add_record", ToolErrorKind.Forbidden, "授权被拒绝");
        var invalidArgs = FeishuToolBinding.StructuredError(
            "bitable.query_records", ToolErrorKind.InvalidArgs, "filter 语法不支持");
        var retryable = FeishuToolBinding.StructuredError(
            "bitable.query_records", ToolErrorKind.Retryable, "服务端繁忙");
        var apiError = FeishuToolBinding.StructuredError("bitable.query_records", "其余错误");

        forbidden.Should().Contain("(forbidden)", "授权拒绝必须可区分——模型应放弃或改用只读方案");
        invalidArgs.Should().Contain("(invalid_args)", "参数错误必须可区分——模型应换参数而非重试");
        retryable.Should().Contain("(retryable)", "可重试错误——模型可稍后重试同一调用");
        apiError.Should().NotContain("(", "api_error 维持既有行为（无分类前缀）");

        forbidden.Should().StartWith("[tool_error] bitable.add_record");
        invalidArgs.Should().StartWith("[tool_error] bitable.query_records");
    }

    [Fact]
    public void StructuredError_WithApiCode_ShouldClassifyByCodeSegment()
    {
        var byCode = FeishuToolBinding.StructuredError("im.get_history_messages", 99991663, "无权限");
        byCode.Should().Contain("(forbidden)", "code=99991663 权限类错误按 forbidden 分类");
    }
}
