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
/// 配置来源与优先级：<c>环境变量 &gt; appsettings.local.json &gt; appsettings.{环境}.json &gt; appsettings.json &gt; 代码默认值</c>。
/// </summary>
/// <remarks>
/// 全部用例只操作**内存配置**与**临时目录**，不触碰进程环境变量（并行执行下不可复现），
/// 也不读取真实仓库文件（模板文件的用例单独放在 <see cref="TemplateFileTests"/>）。
/// </remarks>
public class DocAgentConfigurationTests
{
    /// <summary>环境替身（薄转发到 <see cref="TestDoubles.EnvReader"/>，类内少写前缀）。</summary>
    private static Func<string, string?> Env(params (string Name, string Value)[] values)
        => TestDoubles.EnvReader(values);

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

        var settings = DocAgentSettings.FromConfiguration(configuration, Env());

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

    /// <summary>
    /// 环境变量优先于配置文件（同一项两处都有时以环境变量为准）——容器/CI 注入无需改文件。
    /// </summary>
    [Fact]
    public void FromConfiguration_ShouldPreferEnvironment_OverSection()
    {
        var configuration = SectionConfig(
            ("ModelId", "file-model"),
            ("ApiKey", "sk-file"),
            ("Policy", DocAgentSettings.PolicyStrict));

        var settings = DocAgentSettings.FromConfiguration(
            configuration,
            Env(
                (DocAgentSettings.EnvModelKey, "env-model"),
                (DocAgentSettings.EnvApiKey, "sk-env"),
                (DocAgentSettings.EnvPolicy, DocAgentSettings.PolicyReadonly),
                (DocAgentSettings.EnvAppId, "cli_env_0000000000001"),
                (DocAgentSettings.EnvAppSecret, "secret-env")));

        settings.ModelId.Should().Be("env-model");
        settings.ApiKey.Should().Be("sk-env");
        settings.Policy.Should().Be(DocAgentSettings.PolicyReadonly);
        settings.AppId.Should().Be("cli_env_0000000000001");
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
            })
            .Build();

        // 模型两项仍需环境变量或本节提供（FeishuApps 只覆盖租户三项）。
        var settings = DocAgentSettings.FromConfiguration(
            configuration,
            Env((DocAgentSettings.EnvModelKey, "test-model"), (DocAgentSettings.EnvApiKey, "sk-test")));

        settings.AppKey.Should().Be("hr-app");
        settings.AppId.Should().Be("cli_from_apps_00000001");
        settings.AppSecret.Should().Be("secret-from-apps");
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
                ["FeishuDocAgent:AppId"] = "cli_from_section_00001",
                ["FeishuDocAgent:AppSecret"] = "secret-from-section",
                ["FeishuDocAgent:AppKey"] = "section-app",
                ["FeishuApps:0:AppId"] = "cli_from_apps_00000001",
                ["FeishuApps:0:AppSecret"] = "secret-from-apps",
                ["FeishuApps:0:AppKey"] = "apps-app",
            })
            .Build();

        var settings = DocAgentSettings.FromConfiguration(
            configuration,
            Env((DocAgentSettings.EnvModelKey, "test-model"), (DocAgentSettings.EnvApiKey, "sk-test")));

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
                ["FeishuApps:0:AppKey"] = "fallback-app",
                ["FeishuApps:0:AppId"] = "cli_first_000000000001",
                ["FeishuApps:0:AppSecret"] = "secret-first",
                ["FeishuApps:1:AppKey"] = "default-app",
                ["FeishuApps:1:AppId"] = "cli_default_000000001",
                ["FeishuApps:1:AppSecret"] = "secret-default",
                ["FeishuApps:1:IsDefault"] = "true",
            })
            .Build();

        var settings = DocAgentSettings.FromConfiguration(
            configuration,
            Env((DocAgentSettings.EnvModelKey, "test-model"), (DocAgentSettings.EnvApiKey, "sk-test")));

        settings.AppKey.Should().Be("default-app");
        settings.AppId.Should().Be("cli_default_000000001");
    }

    /// <summary>两处都没有必填项时必须 fail-fast，且消息同时给环境变量名与配置文件键。</summary>
    [Fact]
    public void FromConfiguration_ShouldFailFast_WithBothSourceNames()
    {
        var configuration = SectionConfig(("ModelId", "glm-4-flash"), ("ApiKey", "sk-file"));

        var act = () => DocAgentSettings.FromConfiguration(configuration, Env());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DocAgentSettings.EnvAppId}*")
            .WithMessage($"*{DocAgentSettings.SectionName}:AppId*");
    }

    /// <summary>非法整数项的错误消息要指出两套命名（否则用户不知道该改哪处）。</summary>
    [Fact]
    public void FromConfiguration_ShouldReportBothNames_ForInvalidInteger()
    {
        var configuration = SectionConfig(
            ("ModelId", "m"), ("ApiKey", "k"), ("AppId", "a"), ("AppSecret", "s"),
            ("SummaryThreshold", "abc"));

        var act = () => DocAgentSettings.FromConfiguration(configuration, Env());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DocAgentSettings.EnvSummaryThreshold}*")
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
    /// 文件分层：<c>appsettings.local.json</c> 覆盖 <c>appsettings.json</c>，环境变量再覆盖两者；
    /// 且只有真实存在的文件才计入"配置来源"清单。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ShouldLayerLocalOverBase_AndEnvOverAll()
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

            var sources = DocAgentDemo.BuildConfiguration(
                directory,
                Env((DocAgentSettings.EnvApiKey, "sk-env")));

            sources.Files.Should().Equal(DocAgentSettings.AppSettingsFile, DocAgentSettings.LocalAppSettingsFile);

            var settings = DocAgentSettings.FromConfiguration(
                sources.Configuration,
                Env((DocAgentSettings.EnvApiKey, "sk-env")));

            settings.ModelId.Should().Be("base-model", "基础文件提供");
            settings.Policy.Should().Be(DocAgentSettings.PolicyAsk, "local 覆盖 base");
            settings.WikiSpaceId.Should().Be("wikcn_base", "local 未覆盖的键仍来自 base");
            settings.ApiKey.Should().Be("sk-env", "环境变量覆盖所有文件");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary><c>appsettings.{DOTNET_ENVIRONMENT}.json</c> 参与分层（与仓库其他 Demo 同约定）。</summary>
    [Fact]
    public void BuildConfiguration_ShouldIncludeEnvironmentSpecificFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            WriteJson(Path.Combine(directory, DocAgentSettings.AppSettingsFile), """
                { "FeishuDocAgent": { "Policy": "strict" } }
                """);
            WriteJson(Path.Combine(directory, "appsettings.Production.json"), """
                { "FeishuDocAgent": { "Policy": "ask" } }
                """);

            var sources = DocAgentDemo.BuildConfiguration(
                directory,
                Env(("DOTNET_ENVIRONMENT", "Production")));

            sources.Files.Should().Equal(
                DocAgentSettings.AppSettingsFile,
                "appsettings.Production.json");

            DocAgentSettings.FromConfiguration(
                    sources.Configuration,
                    Env(
                        (DocAgentSettings.EnvModelKey, "test-model"),
                        (DocAgentSettings.EnvApiKey, "sk-test"),
                        (DocAgentSettings.EnvAppId, "cli_env_0000000000001"),
                        (DocAgentSettings.EnvAppSecret, "secret-env")))
                .Policy.Should().Be(DocAgentSettings.PolicyAsk);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>
    /// 无配置文件时必须仍然可用（纯环境变量运行是既有用法：容器 / CI / 快速冒烟），
    /// 且"配置来源"清单为空（横幅据此显示"仅环境变量"）。
    /// </summary>
    [Fact]
    public void BuildConfiguration_ShouldBeUsable_WithoutAnyFile()
    {
        var directory = CreateTempDirectory();
        try
        {
            var sources = DocAgentDemo.BuildConfiguration(directory, Env());

            sources.Files.Should().BeEmpty();

            var settings = DocAgentSettings.FromConfiguration(
                sources.Configuration,
                Env(
                    (DocAgentSettings.EnvModelKey, "env-model"),
                    (DocAgentSettings.EnvApiKey, "sk-env"),
                    (DocAgentSettings.EnvAppId, "cli_env_0000000000001"),
                    (DocAgentSettings.EnvAppSecret, "secret-env")));

            settings.ModelId.Should().Be("env-model");
            settings.Policy.Should().Be(DocAgentSettings.PolicyStrict, "无文件时取代码默认值");
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
        var sources = DocAgentDemo.BuildConfiguration(TemplateDirectory(), Env());

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
        var sources = DocAgentDemo.BuildConfiguration(TemplateDirectory(), Env());

        var settings = DocAgentSettings.FromConfiguration(
            sources.Configuration,
            TestDoubles.EnvReader(
                (DocAgentSettings.EnvModelKey, "glm-4-flash"),
                (DocAgentSettings.EnvApiKey, "sk-test"),
                (DocAgentSettings.EnvAppId, "cli_test_000000000001"),
                (DocAgentSettings.EnvAppSecret, "secret-test")));

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
        DocAgentSettings.IsEnabledByConfiguration(
            DocAgentDemo.BuildConfiguration(TemplateDirectory(), Env()).Configuration)
            .Should().BeFalse();
    }

    /// <summary>模板里的密钥字段必须留空（提交真实密钥是最高危的疏漏）。</summary>
    [Fact]
    public void Template_ShouldNotContainAnySecret()
    {
        var section = DocAgentDemo
            .BuildConfiguration(TemplateDirectory(), Env())
            .Configuration
            .GetSection(DocAgentSettings.SectionName);

        section["ApiKey"].Should().BeNullOrWhiteSpace();
        section["AppSecret"].Should().BeNullOrWhiteSpace();
        section["AppId"].Should().BeNullOrWhiteSpace();
    }

    private static IReadOnlyCollection<string> SectionKeys()
        => DocAgentDemo
            .BuildConfiguration(TemplateDirectory(), Env())
            .Configuration
            .GetSection(DocAgentSettings.SectionName)
            .GetChildren()
            .Select(static child => child.Key)
            .ToArray();

    private static string TemplateDirectory()
        => Path.Combine(TestDoubles.RepositoryRoot(), "Demos", "Mud.Feishu.Agent.Demo");

    /// <summary>环境替身：模板用例不提供任何环境变量（走纯文件路径，才能证明"配置真来自文件"）。</summary>
    private static Func<string, string?> Env() => TestDoubles.EnvReader();
}
