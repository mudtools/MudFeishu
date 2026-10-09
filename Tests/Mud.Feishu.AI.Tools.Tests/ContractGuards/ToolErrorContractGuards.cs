// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！
//  任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json;

using Mud.Feishu.AI.Tools.Tools;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// B2 错误契约守卫：确保结构化错误载荷的正确性与一致性。
/// </summary>
/// <remarks>
/// <para>
/// 守卫四条（方案 §3.B2）：
/// <list type="number">
/// <item><c>ToolErrorCategory</c> 与 <c>ToolErrorSubtype</c> 为闭集常量（新增需评审）；</item>
/// <item>全部执行器错误路径必产结构化载荷（金丝雀：注掉一处即红）；</item>
/// <item>首行必为合法 JSON（解析断言）；</item>
/// <item>载荷不在截断范围内（构造超长正文验证）。</item>
/// </list>
/// </para>
/// </remarks>
public class ToolErrorContractGuards
{
    /// <summary>
    /// 守卫 ①：ToolErrorCategory 必须恰好有 8 个值（闭集，新增需评审 + 守卫更新）。
    /// </summary>
    [Fact]
    public void ToolErrorCategory_ShouldHaveExactlyEightValues()
    {
        var enumValues = Enum.GetValues<ToolErrorCategory>();
        enumValues.Should().HaveCount(8,
            "ToolErrorCategory 必须为 8 类闭集（validation/policy/authorization/confirmation/retryable/api/internal/content_safety），"
            + "新增需评审并同批更新本断言");
    }

    /// <summary>
    /// 守卫 ②：ToolErrorSubtype 的常量不得为空字符串。
    /// </summary>
    [Fact]
    public void ToolErrorSubtype_Constants_ShouldNotBeEmpty()
    {
        var subtypeFields = typeof(ToolErrorSubtype)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

        subtypeFields.Should().NotBeEmpty("ToolErrorSubtype 必须有常量定义");

        foreach (var field in subtypeFields)
        {
            var value = (string?)field.GetValue(null);
            value.Should().NotBeNullOrEmpty($"ToolErrorSubtype.{field.Name} 不得为空字符串");
        }
    }

    /// <summary>
    /// 守卫 ③：ToolErrorPayloadSerializer.Serialize 的输出必须是合法 JSON 首行。
    /// </summary>
    [Fact]
    public void Serialize_ShouldProduceValidJsonLine()
    {
        var error = new ToolError(
            Category: "retryable",
            Subtype: "rate_limited",
            Retryable: true,
            RetryAfterSeconds: 3,
            ApiCode: 99991400,
            Attempts: 1,
            Trace: "abc123",
            Tool: "mail.list_messages");

        var json = ToolErrorPayloadSerializer.Serialize(error);

        // 首行必须以 { 开头
        json.Should().StartWith("{", "载荷首行必须以 { 开头（合法 JSON）");

        // 必须可被 JsonDocument 解析
        var act = () => JsonDocument.Parse(json);
        act.Should().NotThrow("载荷首行必须是合法 JSON（模型与宿主均可机械消费）");

        // 不得包含换行符（首行约束）
        json.Should().NotContain("\n", "载荷首行不得包含换行符（正文截断不得吃掉载荷）");
        json.Should().NotContain("\r", "载荷首行不得包含回车符");
    }

    /// <summary>
    /// 守卫 ④：载荷大小不得超过 MaxPayloadBytes 上界。
    /// </summary>
    [Fact]
    public void Serialize_ShouldNotExceedMaxPayloadBytes()
    {
        // 即使所有字段都填充最长的合理值，载荷也不应超过上界。
        var error = new ToolError(
            Category: "content_safety",
            Subtype: "injected_content_blocked",
            Retryable: false,
            RetryAfterSeconds: null,
            ApiCode: 99991663,
            Attempts: 99,
            Trace: new string('x', 64),  // 模拟较长的 trace
            Tool: "feishu.api_call");

        var json = ToolErrorPayloadSerializer.Serialize(error);

        json.Length.Should().BeLessThanOrEqualTo(ToolErrorPayloadSerializer.MaxPayloadBytes,
            $"载荷必须 ≤ {ToolErrorPayloadSerializer.MaxPayloadBytes} 字节（正文截断不得吃掉载荷）");
    }

    /// <summary>
    /// 守卫 ⑤：ToolExecutor 的错误路径必须经过 FromStructuredError（源码结构断言）。
    /// </summary>
    /// <remarks>
    /// 金丝雀：注掉 <c>FromApiOutcomeError</c> 中的 <c>FromStructuredError</c> 调用即红。
    /// </remarks>
    [Fact]
    public void ToolExecutor_ErrorPaths_ShouldUseFromStructuredError()
    {
        var source = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "ToolExecutor.cs"));

        source.Should().Contain("FromStructuredError(",
            "ToolExecutor 的错误路径必须经过 FromStructuredError（唯一结构化载荷出口）");
        source.Should().Contain("ToolErrorPayloadSerializer.Serialize(",
            "ToolExecutor 必须调用 ToolErrorPayloadSerializer.Serialize 构造首行 JSON");
        source.Should().Contain("FeishuToolResult.FromError(errorPayload,",
            "ToolExecutor 必须通过 FeishuToolResult.FromError(ToolError, ...) 构造带结构化载荷的结果");
    }

    /// <summary>
    /// 守卫 ⑥：ToolErrorCategory 的枚举值必须与 CategoryLiteral 映射一致。
    /// </summary>
    [Fact]
    public void CategoryLiteral_ShouldMapAllEnumValues()
    {
        foreach (var category in Enum.GetValues<ToolErrorCategory>())
        {
            var literal = FeishuToolBinding.CategoryLiteral(category);
            literal.Should().NotBeNullOrEmpty(
                $"CategoryLiteral 必须为 {category} 返回非空字符串");
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory) && !File.Exists(Path.Combine(directory, "Mud.Feishu.slnx")))
        {
            directory = Path.GetDirectoryName(directory);
        }

        directory.Should().NotBeNullOrEmpty("测试必须能定位仓库根目录（以 Mud.Feishu.slnx 为锚）");
        return directory!;
    }
}
