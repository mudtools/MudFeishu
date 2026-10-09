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
    /// <summary>
    /// 内存配置：共享键（模型 + 飞书凭证）放 <c>FeishuDemo</c> 节，模式专属键放 <c>FeishuDocAgent</c> 节。
    /// </summary>
    private static IConfigurationRoot BuildConfig(
        (string Key, string Value)[] shared,
        (string Key, string Value)[] modeSpecific)
    {
        var dict = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var (key, value) in shared)
            dict[$"{FeishuDemoSettings.SectionName}:{key}"] = value;
        foreach (var (key, value) in modeSpecific)
            dict[$"{DocAgentSettings.SectionName}:{key}"] = value;
        return new ConfigurationBuilder().AddInMemoryCollection(dict).Build();
    }

    /// <summary>节里的每一项都必须被读到（键名映射的完整覆盖）。</summary>
    [Fact]
    public void FromConfiguration_ShouldReadEveryKeyFromSection()
    {
        var configuration = BuildConfig(
            shared:
            [
                ("ModelId", "glm-4-flash"),
                ("ApiKey", "sk-from-file"),
                ("Endpoint", "https://example.com/v1/"),
                ("AppId", "cli_from_file_00000001"),
                ("AppSecret", "secret-from-file"),
            ],
            modeSpecific:
            [
                ("AppKey", "hr-app"),
                ("UserId", "ou_from_file"),
                ("WikiSpaceId", "wikcn_from_file"),
                ("Policy", DocAgentSettings.PolicyAsk),
                ("SummaryThreshold", "8"),
                ("AuditExportPath", "D:/tmp/audit.jsonl"),
                ("AttachmentMaxMb", "7"),
            ]);

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

    /// <summary>缺飞书凭证时必须 fail-fast，且消息指向 <c>FeishuDemo</c> 节。</summary>
    [Fact]
    public void FromConfiguration_ShouldFailFast_WhenTenantMissing()
    {
        var configuration = BuildConfig(
            shared: [("ModelId", "glm-4-flash"), ("ApiKey", "sk-file")],
            modeSpecific: []);

        var act = () => DocAgentSettings.FromConfiguration(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{FeishuDemoSettings.SectionName}:AppId*");
    }

    /// <summary>非法整数项的错误消息要指出配置键（否则用户不知道该改哪处）。</summary>
    [Fact]
    public void FromConfiguration_ShouldReportKey_ForInvalidInteger()
    {
        var configuration = BuildConfig(
            shared: [("ModelId", "m"), ("ApiKey", "k"), ("AppId", "a"), ("AppSecret", "s")],
            modeSpecific: [("SummaryThreshold", "abc")]);

        var act = () => DocAgentSettings.FromConfiguration(configuration);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage($"*{DocAgentSettings.SectionName}:SummaryThreshold*");
    }

    /// <summary>模式开关：默认不启用；配置文件置 true 才启用。</summary>
    [Fact]
    public void IsEnabledByConfiguration_ShouldReflectEnabledKey()
    {
        DocAgentSettings.IsEnabledByConfiguration(
            new ConfigurationBuilder().AddInMemoryCollection().Build()).Should().BeFalse();

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
                  "FeishuDemo": {
                    "ModelId": "base-model",
                    "ApiKey": "sk-base",
                    "AppId": "cli_base_00000000001",
                    "AppSecret": "secret-base"
                  },
                  "FeishuDocAgent": {
                    "Policy": "readonly",
                    "WikiSpaceId": "wikcn_base"
                  }
                }
                """);

            WriteJson(Path.Combine(directory, DocAgentSettings.LocalAppSettingsFile), """
                {
                  "FeishuDemo": {
                    "ApiKey": "sk-local"
                  },
                  "FeishuDocAgent": {
                    "Policy": "ask"
                  }
                }
                """);

            var sources = DemoConfiguration.Build(directory);

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

            var sources = DemoConfiguration.Build(directory);

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
            var act = () => DemoConfiguration.Build(directory);

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
    /// <summary>模板必须能被配置提供程序解析（注释 / 尾逗号合法），且含 <c>FeishuDemo</c> 与 <c>FeishuDocAgent</c> 两节。</summary>
    [Fact]
    public void Template_ShouldBeLoadable()
    {
        var sources = DemoConfiguration.Build(TemplateDirectory());

        sources.Files.Should().Contain(DocAgentSettings.AppSettingsFile);

        var sharedSection = sources.Configuration.GetSection(FeishuDemoSettings.SectionName);
        sharedSection.Exists().Should().BeTrue("模板必须含 FeishuDemo 节（统一连接配置）");
        sharedSection.GetChildren().Should().NotBeEmpty();

        var modeSection = sources.Configuration.GetSection(DocAgentSettings.SectionName);
        modeSection.Exists().Should().BeTrue("模板必须含 FeishuDocAgent 节");
        modeSection.GetChildren().Should().NotBeEmpty();
    }

    /// <summary>模板里的每个键都必须是受支持的键（防拼写错误静默失效）。</summary>
    [Fact]
    public void Template_ShouldOnlyContainSupportedKeys()
    {
        var modeKeys = ModeSectionKeys();
        var sharedKeys = SharedSectionKeys();

        var supportedMode = DocAgentSettings.ConfigurationKeys
            .Append("Enabled")
            .ToHashSet(StringComparer.Ordinal);

        var supportedShared = FeishuDemoSettings.ConfigurationKeys.ToHashSet(StringComparer.Ordinal);

        modeKeys.Should().BeSubsetOf(supportedMode, "FeishuDocAgent 节的键必须受支持");
        sharedKeys.Should().BeSubsetOf(supportedShared, "FeishuDemo 节的键必须受支持");
    }

    /// <summary>每个受支持的键都必须在模板里出现（改了代码键名就必须同批改模板）。</summary>
    [Fact]
    public void Template_ShouldCoverAllSupportedKeys()
    {
        ModeSectionKeys().Should().Contain(
            DocAgentSettings.ConfigurationKeys,
            "FeishuDocAgent 节必须覆盖所有受支持的模式专属键");

        SharedSectionKeys().Should().Contain(
            FeishuDemoSettings.ConfigurationKeys,
            "FeishuDemo 节必须覆盖所有受支持的共享键");
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
                [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyModelId}"] = "glm-4-flash",
                [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyApiKey}"] = "sk-test",
                [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyAppId}"] = "cli_test_000000000001",
                [$"{FeishuDemoSettings.SectionName}:{DocAgentSettings.KeyAppSecret}"] = "secret-test",
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
            .GetSection(FeishuDemoSettings.SectionName);

        section["ApiKey"].Should().BeNullOrWhiteSpace();
        section["AppSecret"].Should().BeNullOrWhiteSpace();
        section["AppId"].Should().BeNullOrWhiteSpace();
    }

    private static IReadOnlyCollection<string> ModeSectionKeys()
        => TemplateConfiguration()
            .GetSection(DocAgentSettings.SectionName)
            .GetChildren()
            .Select(static child => child.Key)
            .ToArray();

    private static IReadOnlyCollection<string> SharedSectionKeys()
        => TemplateConfiguration()
            .GetSection(FeishuDemoSettings.SectionName)
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