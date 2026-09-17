// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mud.Feishu.Abstractions.Authentication;
using Mud.Feishu.Abstractions.Authentication.MultiApp;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Utilities;
using Mud.HttpUtils;

namespace Mud.Feishu.Abstractions.Tests.ContractGuards;

/// <summary>
/// TMA2-21 / §7.4：令牌与多应用域的 6 条契约守卫。
/// 防止"静默漂移"回退——修复落地后通过反射/扫描断言守护不变式。
/// </summary>
public class TokenMultiAppContractGuards
{
    // ────────────────────────────────────────────────────────────────────
    // 守卫 1：TokenKeyLayout_ShouldBeSingleSourceOfTruth
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 对 (appKey, tokenType, userId) 的笛卡尔积，Memory 与 Redis 实现产生逐字节相同的键；
    /// 且 TryParse* 可还原出原始 tokenType。
    /// </summary>
    [Fact]
    public void TokenKeyLayout_ShouldBeSingleSourceOfTruth()
    {
        var appKeys = new[] { "normal_app", "app:with:colon" };
        var tokenTypes = new[] { "tenant", "tenant:cli_xxx", "user:cli_yyy" };
        var userIds = new[] { "ou_normal", "ou_with:colon" };

        var prefixes = new[] { "feishu:normal_app:token", "feishu:app\\:with\\:colon:token" };

        foreach (var prefix in prefixes)
        {
            foreach (var tokenType in tokenTypes)
            {
                // 租户令牌键
                var accessKey = TokenKeyBuilder.TenantAccessKey(prefix, tokenType);
                var refreshKey = TokenKeyBuilder.TenantRefreshKey(prefix, tokenType);

                // 反解析必须还原原始 tokenType
                TokenKeyBuilder.TryParseTenantTokenType(accessKey, prefix, out var parsedAccess).Should().BeTrue();
                parsedAccess.Should().Be(tokenType,
                    $"TenantAccessKey 反解析应还原原始 tokenType '{tokenType}'，前缀 '{prefix}'");

                TokenKeyBuilder.TryParseTenantTokenType(refreshKey, prefix, out var parsedRefresh).Should().BeTrue();
                parsedRefresh.Should().Be(tokenType,
                    $"TenantRefreshKey 反解析应还原原始 tokenType '{tokenType}'，前缀 '{prefix}'");
            }

            foreach (var userId in userIds)
            {
                foreach (var tokenType in tokenTypes)
                {
                    var userAccessKey = TokenKeyBuilder.UserAccessKey(prefix, userId, tokenType);
                    var userRefreshKey = TokenKeyBuilder.UserRefreshKey(prefix, userId, tokenType);

                    TokenKeyBuilder.TryParseUserTokenType(userAccessKey, prefix, userId, out var parsedAccess).Should().BeTrue();
                    parsedAccess.Should().Be(tokenType,
                        $"UserAccessKey 反解析应还原原始 tokenType '{tokenType}'");

                    TokenKeyBuilder.TryParseUserTokenType(userRefreshKey, prefix, userId, out var parsedRefresh).Should().BeTrue();
                    parsedRefresh.Should().Be(tokenType,
                        $"UserRefreshKey 反解析应还原原始 tokenType '{tokenType}'");
                }
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 2：MudHttpUtils_PackageReference_ShouldBeSingleVersion
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 扫描全部 csproj，Mud.HttpUtils* 版本唯一且等于 AGENTS.md 声明值（2.0.4）。
    /// </summary>
    [Fact]
    public void MudHttpUtils_PackageReference_ShouldBeSingleVersion()
    {
        var solutionRoot = GetSolutionRoot();
        var csprojFiles = Directory.GetFiles(solutionRoot, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin"))
            .ToList();

        csprojFiles.Should().NotBeEmpty("解决方案中应至少有一个 csproj 文件");

        var versions = new HashSet<string>(StringComparer.Ordinal);

        foreach (var csproj in csprojFiles)
        {
            var content = File.ReadAllText(csproj);
            var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.Contains("Mud.HttpUtils", StringComparison.Ordinal) &&
                    trimmed.Contains("Version=", StringComparison.OrdinalIgnoreCase))
                {
                    // 提取 Version="x.y.z" 或 Version="x.y.z"
                    var versionStart = trimmed.IndexOf("Version=", StringComparison.OrdinalIgnoreCase);
                    if (versionStart < 0)
                        continue;

                    versionStart += "Version=".Length;
                    var quoteChar = trimmed[versionStart];
                    if (quoteChar != '"' && quoteChar != '\'')
                        continue;

                    var versionEnd = trimmed.IndexOf(quoteChar, versionStart + 1);
                    if (versionEnd < 0)
                        continue;

                    var version = trimmed.Substring(versionStart + 1, versionEnd - versionStart - 1);
                    if (!string.IsNullOrWhiteSpace(version))
                        versions.Add(version);
                }
            }
        }

        // AGENTS.md 声明的当前固定版本为 2.0.6。
        const string ExpectedVersion = "2.0.6";

        versions.Should().NotBeEmpty("应至少有一个 Mud.HttpUtils* 包引用");
        versions.Should().ContainSingle(
            $"Mud.HttpUtils* 包版本应唯一（期望 {ExpectedVersion}），实际发现: [{string.Join(", ", versions)}]");
        versions.Single().Should().Be(ExpectedVersion,
            $"AGENTS.md 声明当前固定版本为 {ExpectedVersion}");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 3：FeishuAppConfig_PropertySet_ShouldMatchThrottleComparisonContract
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 反射遍历 FeishuAppConfig 全部 public 可写属性，逐一"只改一个字段"后
    /// FeishuAppManager.IsSameAs 必须为 false。
    /// </summary>
    [Fact]
    public void FeishuAppConfig_PropertySet_ShouldMatchThrottleComparisonContract()
    {
        var baseline = new FeishuAppConfig
        {
            AppKey = "test",
            AppId = "cli_test",
            AppSecret = "dsk_test",
            BaseUrl = "https://open.feishu.cn",
            AllowCustomBaseUrl = false,
            TimeOut = 30,
            RetryCount = 3,
            RetryDelayMs = 1000,
            CircuitBreakerEnabled = false,
            CircuitBreakerFailureThreshold = 10,
            CircuitBreakerSamplingDurationSeconds = 30,
            CircuitBreakerBreakDurationSeconds = 60,
            CircuitBreakerMinimumThroughput = 5,
            TokenRefreshThreshold = 300,
            EnableLogging = false,
            IsDefault = false
        };

        var writableProperties = typeof(FeishuAppConfig)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetSetMethod() != null)
            .ToList();

        writableProperties.Should().NotBeEmpty("FeishuAppConfig 应有可写属性");

        foreach (var prop in writableProperties)
        {
            // AppKey 不参与 IsSameAs 比较（它是键，不是节流字段）
            if (prop.Name == nameof(FeishuAppConfig.AppKey))
                continue;

            var modified = new FeishuAppConfig
            {
                AppKey = baseline.AppKey,
                AppId = baseline.AppId,
                AppSecret = baseline.AppSecret,
                BaseUrl = baseline.BaseUrl,
                AllowCustomBaseUrl = baseline.AllowCustomBaseUrl,
                TimeOut = baseline.TimeOut,
                RetryCount = baseline.RetryCount,
                RetryDelayMs = baseline.RetryDelayMs,
                CircuitBreakerEnabled = baseline.CircuitBreakerEnabled,
                CircuitBreakerFailureThreshold = baseline.CircuitBreakerFailureThreshold,
                CircuitBreakerSamplingDurationSeconds = baseline.CircuitBreakerSamplingDurationSeconds,
                CircuitBreakerBreakDurationSeconds = baseline.CircuitBreakerBreakDurationSeconds,
                CircuitBreakerMinimumThroughput = baseline.CircuitBreakerMinimumThroughput,
                TokenRefreshThreshold = baseline.TokenRefreshThreshold,
                EnableLogging = baseline.EnableLogging,
                IsDefault = baseline.IsDefault
            };

            // 修改当前属性值
            var originalValue = prop.GetValue(modified);
            var newValue = GetDifferentValue(prop.PropertyType, originalValue);
            prop.SetValue(modified, newValue);

            var isSame = FeishuAppManager.IsSameAs(baseline, modified);
            isSame.Should().BeFalse(
                $"修改 FeishuAppConfig.{prop.Name} 后 IsSameAs 必须返回 false（节流比较必须覆盖此字段）");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 4：FeishuApiResultJsonContext_ShouldCoverAuthenticationDtos
    // ────────────────────────────────────────────────────────────────────

#if NET8_0_OR_GREATER
    /// <summary>
    /// 反射断言 AOT 上下文覆盖 AppCredentials / OAuthTokenRequest /
    /// OAuthRefreshTokenRequest / OAuthCredentialsResult。
    /// </summary>
    [Fact]
    public void FeishuApiResultJsonContext_ShouldCoverAuthenticationDtos()
    {
        var context = FeishuApiResultJsonContext.Default;

        var requiredTypes = new[]
        {
            "AppCredentials",
            "OAuthTokenRequest",
            "OAuthRefreshTokenRequest",
            "OAuthCredentialsResult"
        };

        var contextAssembly = context.GetType().Assembly;
        var allTypes = contextAssembly.GetTypes().Select(t => t.Name).ToHashSet();

        foreach (var required in requiredTypes)
        {
            allTypes.Should().Contain(required,
                $"FeishuApiResultJsonContext 应覆盖认证 DTO '{required}'（AOT 安全通道）");
        }

        // 也检查 JsonTypeInfo 属性
        var jsonTypeInfoProperties = typeof(FeishuApiResultJsonContext)
            .GetProperties()
            .Where(p => p.PropertyType.IsGenericType &&
                        p.PropertyType.GetGenericTypeDefinition().Name.StartsWith("JsonTypeInfo"))
            .Select(p => p.Name)
            .ToList();

        foreach (var required in requiredTypes)
        {
            jsonTypeInfoProperties.Should().ContainMatch($"*{required}*",
                $"FeishuApiResultJsonContext 应暴露 JsonTypeInfo 属性覆盖 '{required}'");
        }
    }
#endif

    // ────────────────────────────────────────────────────────────────────
    // 守卫 5：ConfigDtos_ShouldNotUseRequired
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 反射断言 FeishuAppConfig / FeishuAppOptions / TokenRecoveryOptions 无 required
    /// （源生成配置绑定约束——生成器通过 new T() 构造，required 会编译失败 CS9035）。
    /// </summary>
    [Fact]
    public void ConfigDtos_ShouldNotUseRequired()
    {
        var configDtoTypes = new[]
        {
            typeof(FeishuAppConfig),
            typeof(FeishuAppOptions),
        };

        // TokenRecoveryOptions 在 Mud.HttpUtils 中，检查其是否可用
        var tokenRecoveryOptionsType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "TokenRecoveryOptions");

        if (tokenRecoveryOptionsType != null)
            configDtoTypes = configDtoTypes.Append(tokenRecoveryOptionsType).ToArray();

        foreach (var dtoType in configDtoTypes)
        {
            var properties = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in properties)
            {
                var requiredModifiers = prop.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>();
                requiredModifiers.Should().BeNull(
                    $"{dtoType.Name}.{prop.Name} 不得使用 required 修饰符——" +
                    "源生成配置绑定（EnableConfigurationBindingGenerator）通过 new T() 构造，" +
                    "required 会编译失败 CS9035");
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 6：FeishuAppOptions_ShouldNotDeclareUnconsumedSwitches
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 反射遍历 FeishuAppOptions 全部属性，断言每个属性名在同一程序集内
    /// 存在至少一处读取（源扫描），守护 TMA2-13（删除死配置后不应回退）。
    /// </summary>
    [Fact]
    public void FeishuAppOptions_ShouldNotDeclareUnconsumedSwitches()
    {
        var optionsType = typeof(FeishuAppOptions);
        var properties = optionsType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .ToList();

        properties.Should().NotBeEmpty("FeishuAppOptions 应有可写属性");

        var assembly = optionsType.Assembly;
        var assemblyDir = Path.GetDirectoryName(assembly.Location) ?? AppContext.BaseDirectory;
        var sourceFiles = Directory.GetFiles(
            assemblyDir,
            "*.dll"
        ).ToList();

        // 扫描 Abstractions 程序集中的 IL，检查属性是否有 getter 调用
        // 简化策略：扫描源码文件而非 IL
        var solutionRoot = GetSolutionRoot();
        var allCsFiles = Directory.GetFiles(solutionRoot, "*.cs", SearchOption.AllDirectories)
            .Where(p => !p.Contains("obj") && !p.Contains("bin") && !p.Contains("Tests"))
            .ToList();

        foreach (var prop in properties)
        {
            var propName = prop.Name;

            // 在源码中搜索 ".PropertyName" 或 "PropertyName =" 模式
            var found = false;
            foreach (var csFile in allCsFiles)
            {
                var content = File.ReadAllText(csFile);
                // 搜索属性读取模式：.PropertyName（不含等号，排除赋值左侧）
                if (content.Contains($".{propName}", StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }

            found.Should().BeTrue(
                $"FeishuAppOptions.{propName} 应在源码中有至少一处消费方——" +
                "TMA2-13 删除死配置后不应回退。" +
                "若此属性为新引入且尚未消费，请添加消费方或删除此属性。");
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 辅助方法
    // ────────────────────────────────────────────────────────────────────

    private static string GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir ?? AppContext.BaseDirectory;
    }

    private static object? GetDifferentValue(Type propertyType, object? originalValue)
    {
        if (propertyType == typeof(string))
            return (originalValue as string) + "_modified";
        if (propertyType == typeof(int))
            return (int)(originalValue ?? 0) + 1;
        if (propertyType == typeof(bool))
            return !(bool)(originalValue ?? false);
        if (propertyType == typeof(double))
            return (double)(originalValue ?? 0.0) + 1.0;
        if (propertyType == typeof(long))
            return (long)(originalValue ?? 0L) + 1L;

        return Activator.CreateInstance(propertyType);
    }
}
