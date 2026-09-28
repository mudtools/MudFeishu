// -----------------------------------------------------------------------
//  作者:Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tests.Agents;

/// <summary>
/// <see cref="FeishuAgentOptions"/> 校验边界（Phase 0 §3.2）与配置面约束（R5）。
/// </summary>
public class FeishuAgentOptionsTests
{
    [Fact]
    public void Validate_ShouldPass_WhenDefaultsOverriddenWithValidValues()
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "你是飞书助手",
            MaxHistoryMessages = 20,
        }.Validate();

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldRejectEmptyInstructions()
    {
        var act = () => new FeishuAgentOptions { Instructions = " " }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*Instructions*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldRejectNonPositiveMaxHistoryMessages(int maxHistory)
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "x",
            MaxHistoryMessages = maxHistory,
        }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxHistoryMessages*");
    }

    // ───────────────── Phase 2 新增属性（WriteAllowList / MaxStreamChunkLength / SummaryThreshold） ─────────────────

    [Fact]
    public void Validate_ShouldRejectEmptyEntryInWriteAllowList()
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "x",
            WriteAllowList = ["im.send_message", ""],
        }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*WriteAllowList*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Validate_ShouldRejectNonPositiveMaxStreamChunkLength(int chunkLength)
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "x",
            MaxStreamChunkLength = chunkLength,
        }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxStreamChunkLength*");
    }

    [Theory]
    [InlineData(0)]     // 0 = 禁用摘要（合法）
    [InlineData(30)]    // 常规阈值
    public void Validate_ShouldAcceptLegalSummaryThreshold(int summaryThreshold)
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "x",
            SummaryThreshold = summaryThreshold,
        }.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Validate_ShouldRejectSummaryThresholdBetweenOneAndThree(int summaryThreshold)
    {
        var act = () => new FeishuAgentOptions
        {
            Instructions = "x",
            SummaryThreshold = summaryThreshold,
        }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*SummaryThreshold*",
            "阈值 1~3 无法保证「重建后条数低于阈值」，会导致每轮重复摘要——必须 0（禁用）或 ≥ 4");
    }

    /// <summary>
    /// 会话 TTL 阈值归属 <see cref="FeishuConversationOptions"/>（Abstractions——
    /// Memory/Redis 双后端单一阈值源，包间纵向引用治理），不再位于 FeishuAgentOptions。
    /// </summary>
    [Fact]
    public void FeishuConversationOptions_Validate_ShouldRejectNonPositiveSessionTtl()
    {
        var invalid = () => new FeishuConversationOptions { SessionTtl = TimeSpan.Zero }.Validate();
        invalid.Should().Throw<InvalidOperationException>().WithMessage("*SessionTtl*");

        var valid = () => new FeishuConversationOptions().Validate();
        valid.Should().NotThrow();
    }

    // ───────────────── R3/AT-B13 + AT-F14：策略轴配置键（§8.2 #18） ─────────────────

    /// <summary>
    /// <c>MaxToolRisk</c> 取值必须落在 Schema 的 <c>x-feishu.risk</c> 词汇闭集内。
    /// </summary>
    /// <remarks>
    /// <b>为什么是字符串而不是 C# 枚举</b>：Schema 的字面量是连字符风格（<c>high-risk-write</c>），
    /// 枚举绑定无法消费它。用字符串 + fail-fast 校验，换来"配置词汇 == Schema 词汇"这一单一来源。
    /// </remarks>
    [Theory]
    [InlineData("read")]
    [InlineData("write")]
    [InlineData("high-risk-write")]
    public void Validate_ShouldAcceptLegalMaxToolRisk(string risk)
    {
        var act = () => new FeishuAgentOptions { Instructions = "x", MaxToolRisk = risk }.Validate();

        act.Should().NotThrow($"{risk} 是 Schema 的 x-feishu.risk 合法字面量");
    }

    [Theory]
    [InlineData("HighRiskWrite")]   // C# 枚举名（常见误写：来自 ToolRisk 枚举）
    [InlineData("highriskwrite")]
    [InlineData("all")]
    [InlineData("")]
    public void Validate_ShouldRejectIllegalMaxToolRisk(string risk)
    {
        var act = () => new FeishuAgentOptions { Instructions = "x", MaxToolRisk = risk }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*MaxToolRisk*",
            "非法取值会让风险轴静默失效（枚举名 high-risk-write 与 C# 名 HighRiskWrite 的混淆是最常见的一种）");
    }

    [Fact]
    public void Validate_ShouldRejectEmptyAllowedIdentities()
    {
        var empty = () => new FeishuAgentOptions { Instructions = "x", AllowedIdentities = [] }.Validate();
        empty.Should().Throw<InvalidOperationException>().WithMessage("*AllowedIdentities*",
            "空集会在策略轴上拒绝全部工具（且失败信息指向配置而非能力缺失，极难排查）");

        var blank = () => new FeishuAgentOptions { Instructions = "x", AllowedIdentities = ["tenant", " "] }.Validate();
        blank.Should().Throw<InvalidOperationException>().WithMessage("*AllowedIdentities*");

        var ok = () => new FeishuAgentOptions { Instructions = "x", AllowedIdentities = ["tenant", "user"] }.Validate();
        ok.Should().NotThrow();
    }

    [Theory]
    [InlineData(ContentSafetyModes.Off)]
    [InlineData(ContentSafetyModes.Warn)]
    [InlineData(ContentSafetyModes.Block)]
    public void Validate_ShouldAcceptLegalContentSafetyMode(string mode)
    {
        var act = () => new FeishuAgentOptions { Instructions = "x", ContentSafetyMode = mode }.Validate();

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("Warn")]    // 大小写敏感：拼错会静默降级（warn 与 off 的差别是"标不标注"）
    [InlineData("blocked")]
    [InlineData("none")]
    public void Validate_ShouldRejectIllegalContentSafetyMode(string mode)
    {
        var act = () => new FeishuAgentOptions { Instructions = "x", ContentSafetyMode = mode }.Validate();

        act.Should().Throw<InvalidOperationException>().WithMessage("*ContentSafetyMode*");
    }

    /// <summary>
    /// 策略轴四个新键的<b>默认值必须保持现行为不收紧</b>（方案 §10.2 R-1）。
    /// </summary>
    /// <remarks>
    /// 误把 <c>MaxToolRisk</c> 默认设成 <c>read</c> 会让宿主的写工具<b>突然全部不可用</b>——
    /// 这是本轮引入的最容易踩的回归，故用断言钉住默认值。
    /// </remarks>
    [Fact]
    public void PolicyDefaults_ShouldNotTightenExistingBehaviour()
    {
        var options = new FeishuAgentOptions();

        options.MaxToolRisk.Should().Be(FeishuToolRiskNames.HighRiskWrite,
            "现行为 = 写工具需显式 WriteAllowList + 授权器（已足够严）；本键是宿主的显式收敛工具，不是新增默认限制");
        options.AllowedIdentities.Should().Equal(["tenant"], "当前工具面全部为租户令牌工具");
        options.ContentSafetyMode.Should().Be(ContentSafetyModes.Warn,
            "默认 warn（比官方 CLI 的默认 off 更安全），且不阻断工具语义");
    }

    /// <summary>
    /// R5：配置 DTO 不得使用 <c>required</c>——源生成配置绑定器经 <c>new T()</c> 构造，
    /// 带 required 会产生 CS9035。反射断言（比源码扫描更硬）。
    /// </summary>
    [Fact]
    public void Options_ShouldNotUseRequiredModifier()
    {
        var optionsTypes = new[] { typeof(FeishuAgentOptions), typeof(FeishuConversationOptions) };
        foreach (var optionsType in optionsTypes)
            foreach (var property in optionsType.GetProperties())
            {
                var isRequired = property.GetCustomAttributesData()
                    .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute")
                    || optionsType.GetCustomAttributesData()
                        .Any(a => a.AttributeType.FullName == "System.Runtime.CompilerServices.RequiredMemberAttribute");

                isRequired.Should().BeFalse(
                    $"{optionsType.Name}.{property.Name} 不得标记 required（配置绑定源生成器 CS9035）");
            }
    }
}
