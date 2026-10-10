// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// 三个新模式（裸模型 / 工具冒烟 / IM 事件接入）的配置解析与 fail-fast 校验。
/// </summary>
/// <remarks>
/// 参数只从配置文件读取（R4 配置面约定）；每个模式有独立配置节与独立 <c>Enabled</c> 开关。
/// </remarks>
public class DemoModeSettingsTests
{
    /// <summary>内存配置（键名即完整路径，如 <c>FeishuToolsDemo:ModelId</c>）。</summary>
    private static IConfigurationRoot Config(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                v => v.Key,
                static v => (string?)v.Value,
                StringComparer.Ordinal))
            .Build();

    // ──────────────────────────── ChatModelSettings ────────────────────────────

    /// <summary>模型三参数必须完整读到（含可选 Endpoint）。</summary>
    [Fact]
    public void ChatModelSettings_FromSection_ShouldReadModelIdApiKeyEndpoint()
    {
        var config = Config(
            ("FeishuDemo:ModelId", "glm-4-flash"),
            ("FeishuDemo:ApiKey", "sk-test"),
            ("FeishuDemo:Endpoint", "https://example.com/v1"));

        var model = ChatModelSettings.FromSection(config, FeishuDemoSettings.SectionName);

        model.ModelId.Should().Be("glm-4-flash");
        model.ApiKey.Should().Be("sk-test");
        model.Endpoint.Should().Be("https://example.com/v1");
    }

    /// <summary>缺必填项 fail-fast，消息指明配置键（节:键）。</summary>
    [Theory]
    [InlineData("ModelId")]
    [InlineData("ApiKey")]
    public void ChatModelSettings_FromSection_ShouldFailFast_WhenMissingRequired(string missing)
    {
        var config = Config(
            ("FeishuDemo:ModelId", "glm-4-flash"),
            ("FeishuDemo:ApiKey", "sk-test"));

        // 抽掉一键（用不含该键的等价集合重建）。
        var pruned = config.AsEnumerable()
            .Where(kv => !kv.Key.EndsWith($":{missing}", StringComparison.Ordinal))
            .ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.Ordinal);

        var act = () => ChatModelSettings.FromSection(
            new ConfigurationBuilder().AddInMemoryCollection(pruned).Build(),
            FeishuDemoSettings.SectionName);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{FeishuDemoSettings.SectionName}:{missing}*");
    }

    /// <summary>非 HTTPS 且非环回端点必须被拒。</summary>
    [Fact]
    public void ChatModelSettings_Validate_ShouldRejectNonHttpsEndpoint()
    {
        var model = new ChatModelSettings
        {
            ModelId = "glm-4-flash",
            ApiKey = "sk-test",
            Endpoint = "http://api.example.com/v1",
        };

        var act = () => model.Validate(FeishuDemoSettings.SectionName);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{FeishuDemoSettings.SectionName}:Endpoint*");
    }

    /// <summary>环回地址走明文是允许的（本地自建模型端点）。</summary>
    [Fact]
    public void ChatModelSettings_Validate_ShouldAcceptLoopbackEndpoint()
    {
        var model = new ChatModelSettings
        {
            ModelId = "glm-4-flash",
            ApiKey = "sk-test",
            Endpoint = "http://127.0.0.1:11434/v1",
        };

        var act = () => model.Validate(FeishuDemoSettings.SectionName);

        act.Should().NotThrow();
    }

    // ──────────────────────────── ToolsDemoSettings ────────────────────────────

    /// <summary>工具冒烟：开关 + 模型三参数 + 飞书租户三项 + 可选流式目标。模型与凭证从 <c>FeishuDemo</c> 统一节读取。</summary>
    [Fact]
    public void ToolsDemoSettings_FromConfiguration_ShouldReadEveryKey()
    {
        var config = Config(
            ("FeishuDemo:ModelId", "glm-4-flash"),
            ("FeishuDemo:ApiKey", "sk-test"),
            ("FeishuDemo:Endpoint", "https://example.com/v1"),
            ("FeishuDemo:AppId", "cli_test"),
            ("FeishuDemo:AppSecret", "secret-test"),
            ("FeishuToolsDemo:Enabled", "true"),
            ("FeishuToolsDemo:StreamChatId", "oc_test_chat"));

        var settings = ToolsDemoSettings.FromConfiguration(config);

        settings.Enabled.Should().BeTrue();
        settings.Model.ModelId.Should().Be("glm-4-flash");
        settings.AppId.Should().Be("cli_test");
        settings.AppSecret.Should().Be("secret-test");
        settings.StreamChatId.Should().Be("oc_test_chat");
    }

    /// <summary>工具冒烟缺飞书 AppId / AppSecret 时 fail-fast（工具执行需要租户身份），消息指向 <c>FeishuDemo</c> 节。</summary>
    [Theory]
    [InlineData("AppId")]
    [InlineData("AppSecret")]
    public void ToolsDemoSettings_FromConfiguration_ShouldFailFast_WhenMissingTenant(string missing)
    {
        var config = Config(
            ("FeishuDemo:ModelId", "glm-4-flash"),
            ("FeishuDemo:ApiKey", "sk-test"),
            ("FeishuDemo:AppId", "cli_test"),
            ("FeishuDemo:AppSecret", "secret-test"));

        var pruned = config.AsEnumerable()
            .Where(kv => !kv.Key.EndsWith($":{missing}", StringComparison.Ordinal))
            .ToDictionary(static kv => kv.Key, static kv => kv.Value, StringComparer.Ordinal);

        var act = () => ToolsDemoSettings.FromConfiguration(
            new ConfigurationBuilder().AddInMemoryCollection(pruned).Build());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{FeishuDemoSettings.SectionName}:{missing}*");
    }

    // ──────────────────────────── ImHandlerDemoSettings ────────────────────────────

    /// <summary>IM 事件接入：开关 + 模型三参数（无需飞书租户三项）。模型从 <c>FeishuDemo</c> 统一节读取。</summary>
    [Fact]
    public void ImHandlerDemoSettings_FromConfiguration_ShouldReadEveryKey()
    {
        var config = Config(
            ("FeishuDemo:ModelId", "glm-4-flash"),
            ("FeishuDemo:ApiKey", "sk-test"),
            ("FeishuDemo:Endpoint", "https://example.com/v1"),
            ("FeishuImHandlerDemo:Enabled", "true"));

        var settings = ImHandlerDemoSettings.FromConfiguration(config);

        settings.Enabled.Should().BeTrue();
        settings.Model.ModelId.Should().Be("glm-4-flash");
        settings.Model.ApiKey.Should().Be("sk-test");
    }

    // ──────────────────────────── 节名契约 ────────────────────────────

    /// <summary>三个模式的配置节/开关键必须互异（模式分派按节名判定，撞名会静默串线）。</summary>
    [Fact]
    public void ModeSections_ShouldBeDistinct()
    {
        string[] sections =
        [
            FeishuDemoSettings.SectionName,
            DocAgentSettings.SectionName,
            ToolsDemoSettings.SectionName,
            ImHandlerDemoSettings.SectionName,
        ];

        sections.Should().OnlyHaveUniqueItems();

        string[] enabledKeys =
        [
            DocAgentSettings.ConfigEnabledKey,
            ToolsDemoSettings.EnabledKey,
            ImHandlerDemoSettings.EnabledKey,
        ];

        enabledKeys.Should().OnlyHaveUniqueItems();
    }
}