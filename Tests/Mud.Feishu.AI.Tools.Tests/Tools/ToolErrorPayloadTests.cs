// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.Tools;

/// <summary>
/// B2 错误契约载荷测试：分类映射矩阵、subtype 穷举、载荷首行 JSON、截断保护。
/// </summary>
/// <remarks>
/// 方案 §3.B2 测试要求：扩展 <c>ErrorRecoverabilityTests</c> + <c>ToolErrorClassifierTests</c>
/// + 新增 <c>ToolErrorPayloadTests</c>（分类映射矩阵、subtype 穷举、载荷首行、截断保护、写工具/读工具同路径）。
/// </remarks>
public class ToolErrorPayloadTests
{
    // ────────── 分类映射矩阵 ──────────

    [Theory]
    [InlineData(99991663, nameof(ToolErrorCategory.Authorization), "authorization_denied")]
    [InlineData(99991661, nameof(ToolErrorCategory.Authorization), "authorization_denied")]
    [InlineData(0, nameof(ToolErrorCategory.Api), "upstream_error")]
    [InlineData(99991400, nameof(ToolErrorCategory.Api), "upstream_error")]
    [InlineData(null, nameof(ToolErrorCategory.Api), "upstream_error")]
    public void ClassifyCode_ShouldMapKnownApiCodes(int? apiCode, string expectedCategory, string expectedSubtype)
    {
        var (category, subtype) = ToolErrorClassifier.ClassifyCode(apiCode);

        category.ToString().Should().Be(expectedCategory);
        subtype.Should().Be(expectedSubtype);
    }

    [Fact]
    public void Classify_Exception_ShouldMapArgumentExceptionToValidation()
    {
        var ex = new ArgumentException("missing required field");
        var (category, subtype) = ToolErrorClassifier.Classify(ex);

        category.Should().Be(ToolErrorCategory.Validation);
        subtype.Should().Be(ToolErrorSubtype.InvalidArgs);
    }

    [Fact]
    public void Classify_Exception_ShouldMapTimeoutToRetryable()
    {
        var ex = new TimeoutException("request timed out");
        var (category, subtype) = ToolErrorClassifier.Classify(ex);

        category.Should().Be(ToolErrorCategory.Retryable);
        subtype.Should().Be(ToolErrorSubtype.Timeout);
    }

    // ────────── 载荷首行 JSON ──────────

    [Fact]
    public void Serialize_ShouldProduceValidJson_WithToolErrorWrapper()
    {
        var error = new ToolError(
            Category: "validation",
            Subtype: "invalid_args",
            Retryable: false,
            Tool: "bitable.query_records");

        var json = ToolErrorPayloadSerializer.Serialize(error);

        // 解析 JSON 并验证外层结构
        using var doc = JsonDocument.Parse(json);
        doc.RootElement.TryGetProperty("tool_error", out var toolErrorProp).Should().BeTrue(
            "载荷外层必须有 tool_error 键");
        toolErrorProp.GetProperty("category").GetString().Should().Be("validation");
        toolErrorProp.GetProperty("subtype").GetString().Should().Be("invalid_args");
        toolErrorProp.GetProperty("retryable").GetBoolean().Should().BeFalse();
        toolErrorProp.GetProperty("attempts").GetInt32().Should().Be(1);
        toolErrorProp.GetProperty("tool").GetString().Should().Be("bitable.query_records");
    }

    [Fact]
    public void Serialize_ShouldOmitNullFields()
    {
        var error = new ToolError(
            Category: "api",
            Subtype: "upstream_error",
            Retryable: false);  // RetryAfterSeconds/ApiCode/Trace/Tool 全部 null

        var json = ToolErrorPayloadSerializer.Serialize(error);

        using var doc = JsonDocument.Parse(json);
        var toolError = doc.RootElement.GetProperty("tool_error");

        // 必填字段存在
        toolError.GetProperty("category").GetString().Should().Be("api");
        toolError.GetProperty("subtype").GetString().Should().Be("upstream_error");
        toolError.GetProperty("retryable").GetBoolean().Should().BeFalse();
        toolError.GetProperty("attempts").GetInt32().Should().Be(1);

        // 可空字段不应出现
        toolError.TryGetProperty("retry_after_seconds", out _).Should().BeFalse(
            "RetryAfterSeconds 为 null 时不应出现在载荷中");
        toolError.TryGetProperty("api_code", out _).Should().BeFalse(
            "ApiCode 为 null 时不应出现在载荷中");
        toolError.TryGetProperty("trace", out _).Should().BeFalse(
            "Trace 为 null 时不应出现在载荷中");
        toolError.TryGetProperty("tool", out _).Should().BeFalse(
            "Tool 为 null 时不应出现在载荷中");
    }

    [Fact]
    public void Serialize_ShouldIncludeRetryAfterSeconds_WhenProvided()
    {
        var error = new ToolError(
            Category: "retryable",
            Subtype: "rate_limited",
            Retryable: true,
            RetryAfterSeconds: 5);

        var json = ToolErrorPayloadSerializer.Serialize(error);

        using var doc = JsonDocument.Parse(json);
        var toolError = doc.RootElement.GetProperty("tool_error");
        toolError.GetProperty("retry_after_seconds").GetInt32().Should().Be(5);
    }

    // ────────── 截断保护 ──────────

    [Fact]
    public void Serialize_ShouldStayUnderMaxBytes_EvenWithLongTrace()
    {
        // 模拟超长 trace（会被序列化器自动截断）
        var error = new ToolError(
            Category: "internal",
            Subtype: "unexpected",
            Retryable: false,
            Trace: new string('x', 500),  // 远超 MaxPayloadBytes
            Tool: "feishu.api_call");

        var json = ToolErrorPayloadSerializer.Serialize(error);

        json.Length.Should().BeLessThanOrEqualTo(ToolErrorPayloadSerializer.MaxPayloadBytes,
            $"即使 trace 超长，载荷也必须 ≤ {ToolErrorPayloadSerializer.MaxPayloadBytes} 字节（自动截断 trace）");

        // 截断后仍必须是合法 JSON
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow("截断后的载荷必须保持 JSON 合法性");
    }

    // ────────── FeishuToolResult.FromError 行为 ──────────

    [Fact]
    public void FromError_WithToolError_ShouldSetErrorProperty()
    {
        var error = new ToolError(
            Category: "retryable",
            Subtype: "rate_limited",
            Retryable: true,
            Tool: "test.tool");

        var result = FeishuToolResult.FromError(error, "human readable text");

        result.Error.Should().NotBeNull();
        result.Error!.Category.Should().Be("retryable");
        result.Error.Subtype.Should().Be("rate_limited");
        result.Error.Retryable.Should().BeTrue();
        result.Error.Tool.Should().Be("test.tool");
        result.Text.Should().Be("human readable text");
    }

    [Fact]
    public void FromError_WithStringOnly_ShouldHaveNullError()
    {
        var result = FeishuToolResult.FromError("just a text error");

        result.Error.Should().BeNull("FromError(string) 不设置结构化载荷");
        result.Text.Should().Be("just a text error");
    }

    [Fact]
    public void FromText_ShouldHaveNullError()
    {
        var result = FeishuToolResult.FromText("success text");

        result.Error.Should().BeNull("成功路径不应有错误载荷");
        result.Text.Should().Be("success text");
    }

    // ────────── subtype 穷举 ──────────

    [Fact]
    public void ToolErrorSubtype_ShouldHaveAllExpectedConstants()
    {
        // Validation
        ToolErrorSubtype.InvalidArgs.Should().Be("invalid_args");
        ToolErrorSubtype.MissingRequired.Should().Be("missing_required");
        ToolErrorSubtype.ShapeMismatch.Should().Be("shape_mismatch");

        // Policy
        ToolErrorSubtype.RiskExceeded.Should().Be("risk_exceeded");
        ToolErrorSubtype.IdentityMismatch.Should().Be("identity_mismatch");
        ToolErrorSubtype.ToolNotAllowed.Should().Be("tool_not_allowed");

        // Authorization
        ToolErrorSubtype.AuthorizationDenied.Should().Be("authorization_denied");
        ToolErrorSubtype.MissingScope.Should().Be("missing_scope");

        // Confirmation
        ToolErrorSubtype.NeedsUserConfirmation.Should().Be("needs_user_confirmation");

        // Retryable
        ToolErrorSubtype.RateLimited.Should().Be("rate_limited");
        ToolErrorSubtype.Transient.Should().Be("transient");
        ToolErrorSubtype.Timeout.Should().Be("timeout");

        // Api
        ToolErrorSubtype.UpstreamError.Should().Be("upstream_error");

        // Internal
        ToolErrorSubtype.Unexpected.Should().Be("unexpected");
        ToolErrorSubtype.SanitizerRejected.Should().Be("sanitizer_rejected");

        // ContentSafety
        ToolErrorSubtype.InjectedContentBlocked.Should().Be("injected_content_blocked");
    }
}
