// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.Json.Nodes;

using Mud.Feishu.AI.FeishuTools.Tools;

namespace Mud.Feishu.AI.FeishuTools.Tests.Tools;

/// <summary>
/// R5 / F-8（错误可恢复性）+ S-10（工具结果中文可读性）断言。
/// </summary>
/// <remarks>
/// <para>
/// <b>F-8</b> 的判据是"每个错误态都必须给模型一条<b>可执行</b>的下一步"。
/// 官方 <c>lark-cli</c> 把这点做成了机器可读的 <c>Suggestions []string</c>，
/// 并写明其目的是 "so an agent can retry <b>without parsing the human-facing hint</b>"。
/// </para>
/// <para>
/// <b>S-10 为何与 F-8 同批</b>：建议文案若是 <c>\uXXXX</c> 转义串，可读性大幅下降，
/// 直接抵消 F-8 的收益 —— 二者必须一起落地才有意义。
/// </para>
/// </remarks>
public class ErrorRecoverabilityTests
{
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

    /// <summary>
    /// <b>F-8 核心</b>：五种错误态<b>每一种</b>都必须带下一步（此前 <c>InvalidArgs</c> 与
    /// <c>ApiError</c> 两态没有）。
    /// </summary>
    // 参数用 string 而非 ToolErrorKind：后者是 internal，不能出现在 public 测试方法签名上
    // （CS0051 可访问性不一致）。
    [Theory]
    [InlineData(nameof(ToolErrorKind.InvalidArgs))]
    [InlineData(nameof(ToolErrorKind.ApiError))]
    [InlineData(nameof(ToolErrorKind.Retryable))]
    [InlineData(nameof(ToolErrorKind.Forbidden))]
    [InlineData(nameof(ToolErrorKind.NeedsConfirmation))]
    public void EveryErrorKind_ShouldCarryAnExecutableNextStep(string kindName)
    {
        var kind = Enum.Parse<ToolErrorKind>(kindName);
        var text = FeishuToolBinding.StructuredError("t.x", kind, "原因占位");

        text.Should().StartWith("[tool_error]");
        text.Should().Contain("——", $"{kind} 态缺少下一步建议，模型只能盲试（F-8）");
    }

    /// <summary>
    /// <b>F-8 的机械化来源</b>：闭集参数的非法取值错误必须<b>列出全部合法取值</b>。
    /// </summary>
    /// <remarks>
    /// 这条把"F-2 的 enum 闭集是 F-8 的前置"变成可执行断言：
    /// 闭集提供了候选集合，读取器把它直接输出 —— 模型无需猜。
    /// </remarks>
    /// <remarks>
    /// 端到端走真实工具（而非直接调生成的 <c>ToolArgs</c>——它由生成器发射，
    /// 测试工程不可见），顺带覆盖"闭集 → 解包 → 错误回填"整条链路。
    /// </remarks>
    [Fact]
    public async Task ClosedSetInvalidValue_ShouldListAllLegalValues()
    {
        var tools = new Mud.Feishu.AI.FeishuTools.Internal.DocxWriteTools(
            new Moq.Mock<Mud.Feishu.IFeishuTenantV1Docx>().Object,
            new Moq.Mock<Mud.Feishu.IFeishuTenantV1DocxBlocks>().Object);

        var result = await tools.AppendBlocksAsync(
            new Dictionary<string, object?>(StringComparer.Ordinal)
            {
                ["document_id"] = "doxcn1",
                ["block_type"] = "Nope",
            },
            CancellationToken.None);

        result.Text.Should().Contain("block_type", "必须指明是哪个参数");
        result.Text.Should().Contain("Nope", "必须回显收到的非法值（便于模型发现拼写错误）");
        result.Text.Should().Contain("合法取值", "必须给出合法取值清单");

        // 抽样验证清单里确实是**真实**取值（若清单为空或占位，抽样会红）。
        foreach (var legal in new[] { "Text", "Heading1", "Divider" })
        {
            result.Text.Should().Contain(legal, "合法取值清单必须包含真实取值，否则模型无从纠正");
        }
    }

    /// <summary>
    /// <b>S-10</b>：工具结果 JSON 必须<b>保留中文原文</b>（不得转义成 <c>\uXXXX</c>）。
    /// </summary>
    [Fact]
    public void ToolResultJson_ShouldPreserveCjk()
    {
        var json = ToolResultJson.ToText(new JsonObject
        {
            ["message"] = "文档未被修改（旧内容完整、新内容未写入），可安全重试。",
        });

        json.Should().Contain("可安全重试", "工具结果必须保留中文原文（S-10）");
        json.Should().NotContain("\\u", "JSON 里不应出现 \\uXXXX 转义");

        // 仍是合法 JSON（放宽转义不等于破坏结构）。
        using var document = JsonDocument.Parse(json);
        document.RootElement.GetProperty("message").GetString().Should().Contain("可安全重试");
    }

    /// <summary>
    /// <b>S-10 防回潮</b>：工具结果的<b>中心序列化点</b>（<c>ToolExecutor</c>）不得再直接
    /// 调用 <c>ToJsonString()</c>（那会重新引入转义）。
    /// </summary>
    [Fact]
    public void ToolExecutor_ShouldSerializeViaToolResultJson_NotRawToJsonString()
    {
        var source = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.FeishuTools", "Internal", "ToolExecutor.cs"));

        source.Should().Contain(
            "ToolResultJson.ToText",
            "ToolExecutor 是工具结果的中心序列化点，必须走 ToolResultJson（否则中文被转义，抵消 F-8 收益）");

        source.Should().NotContain(
            ".ToJsonString()",
            "ToolExecutor 出现裸 ToJsonString() —— 会绕过保留中文的编码器（S-10 回潮）");
    }
}
