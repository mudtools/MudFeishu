// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Globalization;
using Microsoft.Extensions.Configuration;

namespace Mud.Feishu.Agent.Demo.Tests;

/// <summary>
/// 配置来源与优先级：<c>appsettings.local.json &gt; appsettings.json &gt; 代码默认值</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>参数全部来自配置文件</b>（R4 配置面约定）：环境变量已不再参与参数解析，
/// 因此本节不再有任何"环境覆盖配置"类用例——配置的唯一事实源是 <c>appsettings*.json</c>。
/// </para>
/// <para>
/// 全部用例只操作**内存配置**与**临时目录**，不触碰进程环境变量（并行执行下不可复现），
/// 也不读取真实仓库文件（模板文件的用例单独放在 <see cref="TemplateFileTests"/>）。
/// </para>
/// </remarks>
public class DocAgentConfigurationTests
{
    /// <summary>内存配置文件（模拟 appsettings 的某个节）。</summary>
    private static IConfigurationRoot SectionConfig(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.ToDictionary(
                v => $"{DocAgentSettings.SectionName}:{v.Key}",
                static v => (string?)v.Value,
                StringComparer.Ordinal))
            .Build();

    /// <summary>节里的每一项都必须被读到（键名映射的完整覆盖）。</summary>
    [Fact]
    public void FromConfiguration_ShouldReadEveryKeyFromSection()
    {
        var configuration = SectionConfig(
            ("ModelId", "glm-4-flash"),
            ("ApiKey", "sk-from-file"),
            ("Endpoint", "https://example.com/v1/"),
            ("AppId", "cli_from_file_00000001"),
            ("AppSecret", "secret-from-file"),
            ("AppKey", "hr-app"),
            ("UserId", "ou_from_file"),
            ("WikiSpaceId", "wikcn_from_file"),
            ("Policy", DocAgentSettings.PolicyAsk),
            ("SummaryThreshold", "8"),
            ("AuditExportPath", "D:/tmp/audit.jsonl"),
            ("AttachmentMaxMb", "7"));

        var settings = DocAgentSettings.FromConfiguration(configuration);

        settings.ModelId.Should().Be("glm-4-flash");
        settings.ApiKey.Should().Be("sk-from-file");
        settings.Endpoint.Should().Be("https://example.com/v1/");
        settings.AppId.Should().Be("cli_from_file_00000001");
        settings.AppSecret.Should().Be("secret-from-file");
        settings.AppKey.Should().Be("hr-app");
        settings.UserId.Should().Be("ou_from_file");
        settings.WikiSpaceId.Should().Be("wikcn_from_file");
        settings.Policy.Should().Be(DocAgentSettings.PolicyAsk);
        settings.SummaryThreshold.Should().Be(8);
        settings.AuditExportPath.Should().Be("D:/tmp/audit.jsonl");
        settings.AttachmentMaxBytes.Should().Be(7L * 1024 * 1024);
    }

    /// <summary>只写 SDK 标准写法（<c>FeishuApps</c> 数组）也必须能跑通（三项等价回退）。</summary>
    [Fact]
    public void FromConfiguration_ShouldFallbackToFeishuAppsSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["FeishuApps:0:AppKey"] = "hr-app",
                ["FeishuApps:0:AppId"] = "cli_from_apps_00000001",
                ["FeishuApps:0:AppSecret"] = "secret-from-apps",

                // 模型两项只能来自本节（FeishuApps 只覆盖租户三项）。
                ["FeishuDocAgent:ModelId"] = "test-model",
                ["FeishuDocAgent:ApiKey"] = "sk-test",
            })
            .Build();

        var settings = DocAgentSettings.FromConfiguration(configuration);

        settings.AppKey.Should().Be("hr-app");
        settings.AppId.Should().Be("cli_from_apps_00000001");
        settings.AppSecret.Should().Be("secret-from-apps");
        settings.ModelId.Should().Be("test-model");
    }

    /// <summary>
    /// 两种写法都出现时以 <c>FeishuApps</c> 为准——那才是 <c>AddFeishuApp</c> 真正消费的配置，
    /// 若取本节值，工具执行上下文的 appKey 会与实际默认应用不一致（被授权器以 appKey 不匹配拒绝）。
    /// </summary>
    [Fact]
    public void FromConfiguration_ShouldPreferFeishuApps_OverSection()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["FeishuDocAgent:ModelId"] = "test-model",
                ["FeishuDocAgent:ApiKey"] = "sk-test",
                ["FeishuDocAgent:AppId"] = "cli_from_section_00001",
                ["FeishuDocAgent:AppSecret"] = "secret-from-section",
                ["FeishuDocAgent:AppKey"] = "section-app",
                ["FeishuApps:0:AppId"] = "cli_from_apps_00000001",
                ["FeishuApps:0:AppSecret"] = "secret-from-apps",
                ["FeishuApps:0:AppKey"] = "apps-app",
            })
            .Build();

        var settings = DocAgentSettings.FromConfiguration(configuration);

        settings.AppId.Should().Be("cli_from_apps_00000001");
        settings.AppSecret.Should().Be("secret-from-apps");
        settings.AppKey.Should().Be("apps-app");
    }

    /// <summary>多应用时取 <c>IsDefault=true</c> 的那个（而不是硬编码第 0 个）。</summary>
    [Fact]
    public void FromConfiguration_ShouldPreferDefaultApp_InFeishuApps()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["FeishuDocAgent:ModelId"] = "test-model",
                ["FeishuDocAgent:ApiKey"] = "sk-test",
                ["FeishuApps:0:AppKey"] = "fallback-app",
                ["FeishuApps:0:AppId"] = "cli_first_000000000001",
                ["FeishuApps:0:AppSecret"] = "secret-first",
                ["FeishuApps:1:AppKey"] = "default-app",
                ["FeishuApps:1:AppId"] = "cli_default_000000001",
                ["FeishuApps:1:AppSecret"] = "secret-default",
                ["FeishuApps:1:IsDefault"] = "true",
            })
            .Build();

        var settings = DocAgentSettings.FromConfiguration(configuration);

        settings.AppKey.Should().Be("default-app");
        settings.AppId.Should().Be("cli_default_000000001");
    }

    /// <summary>两处都没有必填项时必须 fail-fast，且消息同时给配置文件键。</summary>
    [Fact]
    public void FromConfiguration_ShouldFailFast_WhenTenantMissing()
    {
        var configuration = SectionConfig(("ModelId", "glm-4-flash"), ("ApiKey", "sk-file"));

        var act = () => DocAgentSettings.FromConfiguration(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DocAgentSettings.SectionName}:AppId*");
    }

    /// <summary>非法整数项的错误消息要指出配置键（否则用户不知道该改哪处）。</summary>
    [Fact]
    public void FromConfiguration_ShouldReportKey_ForInvalidInteger()
    {
        var configuration = SectionConfig(
            ("ModelId", "m"), ("ApiKey", "k"), ("AppId", "a"), ("AppSecret", "s"),
            ("SummaryThreshold", "abc"));

        var act = () => DocAgentSettings.FromConfiguration(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DocAgentSettings.SectionName}:SummaryThreshold*");
    }

    /// <summary>模式开关：默认不启用；配置文件置 true 才启用。</summary>
    [Fact]
    public void IsEnabledByConfiguration_ShouldReflectEnabledKey()
    {
        DocAgentSettings.IsEnabledByConfiguration(SectionConfig(("ModelId", "m"))).Should().BeFalse();

        DocAgentSettings.IsEnabledByConfiguration(
            new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
                {
                    [DocAgentSettings.ConfigEnabledKey] = "true",
                })
                .Build()).Should().BeTrue();
    }

    /// <summary>
    /// 文件分层：<c>appsettings.local.json</c> 覆盖 <c>appsettings.json</c>；
    /// 且只有真实存在的文件才计入"配置来源"清单。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ShouldLayerLocalOverBase()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteJson(Path.Combine(directory, DocAgentSettings.AppSettingsFile), """
                {
                  "FeishuDocAgent": {
                    "ModelId": "base-model",
                    "ApiKey": "sk-base",
                    "AppId": "cli_base_00000000001",
                    "AppSecret": "secret-base",
                    "Policy": "readonly",
                    "WikiSpaceId": "wikcn_base"
                  }
                }
                """);

            WriteJson(Path.Combine(directory, DocAgentSettings.LocalAppSettingsFile), """
                {
                  "FeishuDocAgent": {
                    "Policy": "ask",
                    "ApiKey": "sk-local"
                  }
                }
                """);

            var sources = DocAgentDemo.BuildConfiguration(directory);

            sources.Files.Should().Equal(DocAgentSettings.AppSettingsFile, DocAgentSettings.LocalAppSettingsFile);

            var settings = DocAgentSettings.FromConfiguration(sources.Configuration);

            settings.ModelId.Should().Be("base-model", "基础文件提供");
            settings.Policy.Should().Be(DocAgentSettings.PolicyAsk, "local 覆盖 base");
            settings.WikiSpaceId.Should().Be("wikcn_base", "local 未覆盖的键仍来自 base");
            settings.ApiKey.Should().Be("sk-local", "local 覆盖 base");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>只有真实存在的配置文件才计入"配置来源"清单（local 可选，未提供时不列出）。</summary>
    [Fact]
    public void BuildConfiguration_ShouldListOnlyExistingFiles()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteJson(Path.Combine(directory, DocAgentSettings.AppSettingsFile), """
                { "FeishuDocAgent": { "Policy": "ask" } }
                """);

            var sources = DocAgentDemo.BuildConfiguration(directory);

            sources.Files.Should().Equal(DocAgentSettings.AppSettingsFile);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// <c>appsettings.json</c> 是必填模板（<c>optional: false</c>）——缺失时 fail-fast 而非静默空配置。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ShouldThrow_WhenBaseFileMissing()
    {
        var directory = CreateTempDirectory();
        try
        {
            var act = () => DocAgentDemo.BuildConfiguration(directory);

            act.Should().Throw<FileNotFoundException>("appsettings.json 是必填模板，缺失时 fail-fast");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>配置已提供 <c>FeishuApps</c> 时不得被合成的单应用配置覆盖。</summary>
    [Fact]
    public void EnsureAppSection_ShouldKeepConfiguredApps()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                ["FeishuApps:0:AppKey"] = "hr-app",
                ["FeishuApps:0:AppId"] = "cli_configured_00000001",
                ["FeishuApps:0:AppSecret"] = "secret-configured",
            })
            .Build();

        var settings = TestDoubles.CreateSettings() with
        {
            AppId = "cli_synthesized_000001",
            AppSecret = "secret-synthesized",
            AppKey = "demo-app",
        };

        var result = DocAgentDemo.EnsureAppSection(configuration, settings);

        result["FeishuApps:0:AppKey"].Should().Be("hr-app");
        result["FeishuApps:0:AppId"].Should().Be("cli_configured_00000001");
    }

    /// <summary>配置未提供 <c>FeishuApps</c> 时由三项配置合成单应用（并标记为默认应用）。</summary>
    [Fact]
    public void EnsureAppSection_ShouldSynthesize_WhenAbsent()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var settings = TestDoubles.CreateSettings() with
        {
            AppKey = "demo-app",
            AppId = "cli_synth_000000000001",
            AppSecret = "secret-synth",
        };

        var result = DocAgentDemo.EnsureAppSection(configuration, settings);

        result["FeishuApps:0:AppKey"].Should().Be("demo-app");
        result["FeishuApps:0:AppId"].Should().Be("cli_synth_000000000001");
        result["FeishuApps:0:AppSecret"].Should().Be("secret-synth");
        result["FeishuApps:0:IsDefault"].Should().Be("true");
    }

    private static string CreateTempDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            string.Concat("mud-docagent-config-", Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)));

        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteJson(string path, string content)
        => File.WriteAllText(path, content, new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
}

/// <summary>
/// 随仓库提交的 <c>appsettings.json</c> 模板的守卫：可被真实配置提供程序解析、键名不漂移、取值合法。
/// </summary>
/// <remarks>
/// 这类"模板文件"最容易静默失效：改了代码键名或写错一个字，用户照抄模板后表现为"配置不生效"，
/// 而启动不报错。故用真实文件做断言（含注释与尾逗号 —— 配置 JSON 提供程序允许二者）。
/// </remarks>
public class TemplateFileTests
{
    /// <summary>模板必须能被配置提供程序解析（注释 / 尾逗号合法）。</summary>
    [Fact]
    public void Template_ShouldBeLoadable()
    {
        var sources = DocAgentDemo.BuildConfiguration(TemplateDirectory());

        sources.Files.Should().Contain(DocAgentSettings.AppSettingsFile);

        var section = sources.Configuration.GetSection(DocAgentSettings.SectionName);
        section.Exists().Should().BeTrue("模板必须含 FeishuDocAgent 节");
        section.GetChildren().Should().NotBeEmpty();
    }

    /// <summary>模板里的每个键都必须是受支持的键（防拼写错误静默失效）。</summary>
    [Fact]
    public void Template_ShouldOnlyContainSupportedKeys()
    {
        var keys = SectionKeys();

        var supported = DocAgentSettings.ConfigurationKeys
            .Append("Enabled")
            .ToHashSet(StringComparer.Ordinal);

        keys.Should().BeSubsetOf(supported);
    }

    /// <summary>每个受支持的键都必须在模板里出现（改了代码键名就必须同批改模板）。</summary>
    [Fact]
    public void Template_ShouldCoverAllSupportedKeys()
    {
        SectionKeys().Should().Contain(
            DocAgentSettings.ConfigurationKeys,
            "模板必须覆盖所有受支持的键（改了代码键名就必须同批改模板）");
    }

    /// <summary>模板里的非密钥默认值必须能通过 <c>Validate()</c>（照抄模板 + 只补密钥即可跑）。</summary>
    [Fact]
    public void Template_Defaults_ShouldPassValidation()
    {
        // 模板只留空密钥；叠加必填密钥（等效于用户照抄模板后只在本地覆盖文件补密钥）。
        var overlaid = new ConfigurationBuilder()
            .AddConfiguration(TemplateConfiguration())
            .AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal)
            {
                [$"{DocAgentSettings.SectionName}:{DocAgentSettings.KeyModelId}"] = "glm-4-flash",
                [$"{DocAgentSettings.SectionName}:{DocAgentSettings.KeyApiKey}"] = "sk-test",
                [$"{DocAgentSettings.SectionName}:{DocAgentSettings.KeyAppId}"] = "cli_test_000000000001",
                [$"{DocAgentSettings.SectionName}:{DocAgentSettings.KeyAppSecret}"] = "secret-test",
            })
            .Build();

        var settings = DocAgentSettings.FromConfiguration(overlaid);

        var act = () => settings.Validate();

        act.Should().NotThrow("模板只留空密钥，其余默认值必须自洽（Policy/SummaryThreshold/AppKey 等）");
        settings.Policy.Should().Be(DocAgentSettings.PolicyStrict);
        settings.AppKey.Should().Be(DocAgentSettings.DefaultAppKey);
        settings.UserId.Should().Be(DocAgentSettings.DefaultConsoleUserId);
        settings.SummaryThreshold.Should().Be(DocAgentSettings.DefaultSummaryThreshold);
    }

    /// <summary>模板默认不启用本模式（避免"拉下代码就自动进 Demo 模式"）。</summary>
    [Fact]
    public void Template_ShouldNotEnableTheDemo()
    {
        DocAgentSettings.IsEnabledByConfiguration(TemplateConfiguration())
            .Should().BeFalse();
    }

    /// <summary>模板里的密钥字段必须留空（提交真实密钥是最高危的疏漏）。</summary>
    [Fact]
    public void Template_ShouldNotContainAnySecret()
    {
        var section = TemplateConfiguration()
            .GetSection(DocAgentSettings.SectionName);

        section["ApiKey"].Should().BeNullOrWhiteSpace();
        section["AppSecret"].Should().BeNullOrWhiteSpace();
        section["AppId"].Should().BeNullOrWhiteSpace();
    }

    private static IReadOnlyCollection<string> SectionKeys()
        => TemplateConfiguration()
            .GetSection(DocAgentSettings.SectionName)
            .GetChildren()
            .Select(static child => child.Key)
            .ToArray();

    /// <summary>
    /// 仅加载随仓库提交的 <c>appsettings.json</c> 模板（不含本地覆盖文件——模板守卫的对象是
    /// <b>提交进仓库的文件</b>，而非开发者本机的 <c>appsettings.local.json</c>）。
    /// </summary>
    private static IConfigurationRoot TemplateConfiguration()
        => new ConfigurationBuilder()
            .AddJsonFile(
                Path.Combine(TemplateDirectory(), DocAgentSettings.AppSettingsFile),
                optional: false,
                reloadOnChange: false)
            .Build();

    private static string TemplateDirectory()
        => Path.Combine(TestDoubles.RepositoryRoot(), "Demos", "Mud.Feishu.Agent.Demo");
}