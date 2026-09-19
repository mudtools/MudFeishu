// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.ContractGuards;

/// <summary>
/// R5/G2：Webhook 配置面契约守卫。
/// </summary>
/// <remarks>
/// <para>
/// 设计要点（见 <c>.docs/配置面可用性修复与收敛方案-R5.md</c> §0.5.2 G-04、§5.3）：
/// </para>
/// <list type="bullet">
///   <item>
///     <b>主护栏是精确的反射断言</b>（守卫 1/2）——零误报，是真正的回归锁。
///   </item>
///   <item>
///     <b>源码扫描只作辅助</b>（守卫 3）且限定显式登记表。把「每个公开配置属性都必须有
///     非 Validate/ToString 消费点」推广到全部配置类型会产生大量误报，最终会被
///     <c>#pragma</c> 架空，反而降低护栏可信度，故不采纳。
///   </item>
/// </list>
/// </remarks>
public class ConfigSurfaceContractGuards
{
    // ────────────────────────────────────────────────────────────────────
    // 守卫 1：已删除的死配置属性不得复活
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 反射断言 R5 判定为「死配置」并删除的属性不得再出现在配置类型上。
    /// </summary>
    /// <remarks>
    /// 为什么用反射而不是源码扫描：源码扫描会把注释、文档字符串、测试夹具一并计入，
    /// 而“类型上不存在该属性”是唯一无歧义的判据。
    /// </remarks>
    [Theory]
    [InlineData(typeof(FeishuWebhookOptions), "EnableRequestLogging")] // audit-allow: guard must name the removed property
    [InlineData(typeof(FeishuAppWebhookOptions), "Description")] // audit-allow: guard must name the removed property
    public void ConfigType_ShouldNotDeclare_RemovedDeadSwitch(Type type, string propertyName)
    {
        type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)
            .Should().BeNull(
                $"{type.Name}.{propertyName} 已被 R5 判定为死配置并删除——" +
                "该属性从未被运行时读取，重新引入会再次制造「配了但无效」的假配置面。");
    }

    /// <summary>
    /// 断言 R5 删除的旧日志常量不得复活（X14）。
    /// </summary>
    [Theory]
    [InlineData("DefaultDeduplicationRetryCount")] // audit-allow: guard must name the removed constant
    [InlineData("DefaultDeduplicationInitialRetryDelayMs")] // audit-allow: guard must name the removed constant
    [InlineData("DefaultDeduplicationMaxRetryDelayMs")] // audit-allow: guard must name the removed constant
    public void AbstractionsConsts_ShouldNotDeclare_RemovedDedupConstant(string constName)
    {
        var constsType = typeof(Mud.Feishu.Abstractions.FeishuAppConfig).Assembly
            .GetType("Mud.Feishu.Abstractions.Consts");

        constsType.Should().NotBeNull("Abstractions 中的 internal Consts 类型是去重默认值的单一真相源");

        constsType!.GetField(constName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .Should().BeNull(
                $"Consts.{constName} 零引用（R5/X14 已删除）——" +
                "重新引入会让「常量与 Options 默认值双源」问题复发。");
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 2：已 Obsolete / 待淘汰的配置项必须仍能从配置绑定（appsettings 兼容）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Obsolete 化只是「下线预告」，**不得**顺带切断配置绑定——否则升级即静默失效。
    /// </summary>
    [Fact]
    public void ObsoleteProperties_ShouldStillBind_FromConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FeishuWebhook:AutoRegisterEndpoint"] = "false",
                ["FeishuWebhook:LegacyGlobalTimeoutOnly"] = "true",
                ["FeishuWebhook:Apps:default:VerificationToken"] = "token",
                ["FeishuWebhook:Apps:default:EncryptKey"] = "0123456789abcdef0123456789abcdef",
                ["FeishuWebhook:Apps:default:AppKey"] = "legacy-app-key"
            })
            .Build();

        var options = new FeishuWebhookOptions();
#pragma warning disable CS0618 // 本守卫的目的就是验证 Obsolete 成员仍可绑定
        configuration.GetSection("FeishuWebhook").Bind(options);

        options.AutoRegisterEndpoint.Should().BeFalse();
        options.LegacyGlobalTimeoutOnly.Should().BeTrue();
        options.GetAppConfig("default")!.AppKey.Should().Be("legacy-app-key");
#pragma warning restore CS0618
    }

    // ────────────────────────────────────────────────────────────────────
    // 守卫 3：已 Obsolete 的成员集合必须与文档/迁移表一致（防止「说了要下线却忘了标」）
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// R5 计划 Obsolete 的成员一旦标注，其 ObsoleteAttribute 不得被悄悄移除
    /// （移除等于把「一个 minor 的过渡期」变成无预告的 breaking）。
    /// </summary>
    [Theory]
    [InlineData(typeof(FeishuWebhookOptions), "AutoRegisterEndpoint")]
    [InlineData(typeof(FeishuWebhookOptions), "LegacyGlobalTimeoutOnly")]
    public void PlannedObsoleteMembers_ShouldKeepObsoleteAttribute(Type type, string memberName)
    {
        var member = (MemberInfo?)type.GetProperty(memberName) ?? type.GetMethod(memberName);

        if (member is null)
        {
            return; // 成员已被删除（下个 major），守卫自动退役
        }

        member.GetCustomAttribute<ObsoleteAttribute>().Should().NotBeNull(
            $"{type.Name}.{memberName} 属 R5 判定「无运行时效果 / 过渡开关」的成员——" +
            "一旦进入 Obsolete 过渡期，不得在删除前擅自移除 Obsolete 标记。");
    }
}
