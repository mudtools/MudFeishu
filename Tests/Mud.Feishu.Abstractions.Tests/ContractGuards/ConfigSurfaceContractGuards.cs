// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Mud.Feishu.Abstractions.Configuration;
using Mud.Feishu.Abstractions.Extensions;

namespace Mud.Feishu.Abstractions.Tests.ContractGuards;

/// <summary>
/// R5/G2：Abstractions 配置面契约守卫。
/// </summary>
/// <remarks>
/// 与 <c>Mud.Feishu.Webhook.Tests/ContractGuards/ConfigSurfaceContractGuards.cs</c> 同批落地，
/// 分别守护各包自身的配置类型（反射断言必须是**强断言**，源码扫描只作辅助）。
/// </remarks>
public class ConfigSurfaceContractGuards
{
    // ────────────────────────────────────────────────────────────────────
    // 守卫 A：配置节常量必须真的被用于 GetSection（防止 X1 类「不可绑定 Options」回归）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 声明了 <c>SectionName</c> 的配置类型，其常量必须在生产源码中被 <c>GetSection</c> 使用。
    /// </summary>
    /// <remarks>
    /// 这是 X1 的直接回归锁：<see cref="FeishuAppOptions.SectionName"/> 曾长期零引用，意味着
    /// 该 Options **根本无法从配置绑定**，而 README 的「读取点」表会让集成方以为可以。
    /// 用源码扫描而非反射是因为「被 GetSection 使用」没有运行期可观测的等价物；
    /// 为避免误报，只扫描生产源码目录（排除 Tests / Demos / bin / obj）。
    /// </remarks>
    [Fact]
    public void SectionNameConstant_ShouldBeUsedByGetSection_InProductionSource()
    {
        var (sectionName, ownerTypeName) = (FeishuAppOptions.SectionName, nameof(FeishuAppOptions));

        var solutionRoot = GetSolutionRoot();
        solutionRoot.Should().NotBeNull("找不到解决方案根目录时本守卫无法工作（宁可失败，也不要假绿）");

        var consumed = Directory
            .GetFiles(solutionRoot!, "*.cs", SearchOption.AllDirectories)
            .Where(path => !IsExcluded(path))
            .Any(path => File.ReadAllText(path)
                .Contains($"{ownerTypeName}.SectionName", StringComparison.Ordinal)
                && File.ReadAllText(path).Contains("GetSection", StringComparison.Ordinal));

        consumed.Should().BeTrue(
            $"{ownerTypeName}.SectionName ('{sectionName}') 必须被 GetSection(...) 真正使用——" +
            "仅声明常量 + AddOptions<T>() 不会绑定任何配置节，该 Options 将永久取默认值（R5/X1）。");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 B：SDK 内部状态位不得对外可写（X11）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="FeishuDeduplicationOptions.IsConfiguredFromConfiguration"/> 必须「外读内写」。
    /// </summary>
    /// <remarks>
    /// 该属性表示「<c>FeishuDeduplication</c> 节是否存在」这一 SDK 内部事实。若对外可写，
    /// 外部宿主只需 <c>options.IsConfiguredFromConfiguration = true</c> 即可在**没有任何节字段**时
    /// 让 SDK 认定统一节有效，从而绕过「新节不存在 → 回退旧键」的双读逻辑（R5/X11）。
    /// </remarks>
    [Fact]
    public void IsConfiguredFromConfiguration_ShouldBeReadOnlyOutsideTheAssembly()
    {
        var property = typeof(FeishuDeduplicationOptions)
            .GetProperty(nameof(FeishuDeduplicationOptions.IsConfiguredFromConfiguration));

        property.Should().NotBeNull();

        property!.GetSetMethod(nonPublic: false).Should().BeNull(
            "该属性是 SDK 内部状态位，不得对外可写——外部赋值可在无配置节时伪造「节存在」，绕过旧键回退。");

        property.GetSetMethod(nonPublic: true).Should().NotBeNull(
            "SDK 绑定器（同程序集）仍需赋值，故 setter 必须是 internal 而非 private。");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 C：X1 的启动期校验器必须存在且被注册
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// <see cref="FeishuAppOptionsValidator"/> 的边界常量必须与实际校验一致（防止有人放宽边界）。
    /// </summary>
    [Fact]
    public void FeishuAppOptionsValidator_ShouldRejectOutOfRangeRetireDelay()
    {
        var validator = new FeishuAppOptionsValidator();

        validator.Validate(null, new FeishuAppOptions { ContextRetireDelaySeconds = 300 })
            .Succeeded.Should().BeTrue("默认 300 秒是合法值");

        validator.Validate(null, new FeishuAppOptions { ContextRetireDelaySeconds = 0 })
            .Succeeded.Should().BeFalse("0 秒会使退休队列立即 Dispose 在途请求持有的上下文");

        validator.Validate(null, new FeishuAppOptions { ContextRetireDelaySeconds = 3601 })
            .Succeeded.Should().BeFalse("超过 3600 秒超出文档承诺的有效范围");
    }

    /// <summary>
    /// X1 的绑定入口必须在源码中存在——防止有人把 <c>RegisterFeishuAppOptionsBinding</c> 的调用删掉。
    /// </summary>
    [Fact]
    public void FeishuAppOptions_ShouldBeBoundOnTheConfigurationPath()
    {
        var solutionRoot = GetSolutionRoot();
        solutionRoot.Should().NotBeNull();

        var multiAppExtensionsPath = Directory
            .GetFiles(solutionRoot!, "FeishuMultiAppExtensions.cs", SearchOption.AllDirectories)
            .FirstOrDefault(path => !IsExcluded(path));

        multiAppExtensionsPath.Should().NotBeNull("绑定入口应位于 FeishuMultiAppExtensions");

        var content = File.ReadAllText(multiAppExtensionsPath!);

        content.Should().Contain("Configure<FeishuAppOptions>",
            "FeishuAppOptions 必须经 Configure<T>(委托) 绑定配置节（R5/X1）。");
        content.Should().NotContain("Configure<FeishuAppOptions>(configuration.GetSection",
            "不得使用 Configure<T>(IConfiguration) 重载——该重载的反射绑定调用点无法被配置绑定源生成器拦截，" +
            "会破坏 IL2026/IL3050 净零（见 AOT-3 记录）。");
    }

    private static bool IsExcluded(string path) =>
        path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
        || path.Contains($"{Path.DirectorySeparatorChar}Demos{Path.DirectorySeparatorChar}", StringComparison.Ordinal);

    private static string? GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir;
    }
}
