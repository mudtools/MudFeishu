// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using FluentAssertions;
using Mud.Feishu.Webhook.Configuration;

namespace Mud.Feishu.Webhook.Tests.Configuration;

/// <summary>
/// R5.2/X10：性能监控开关清理与「耗时日志无条件 Debug」不变量。
/// </summary>
/// <remarks>
/// <para>
// audit-allow: guard must name the removed property
/// 改判依据（§0.5.3）：<c>EnablePerformanceMonitoring</c> 的<b>唯一</b>效果是门控一条
/// <c>Information</c> 级耗时日志。处置为「先把耗时日志无条件降为 <c>Debug</c>，再删开关」——
/// 既保留可诊断性（需要时调 <c>Logging:LogLevel</c>），又与 R4 起
/// 「日志级别只由 <c>Logging:LogLevel</c> 控制，不设模块私有开关」的口径一致。
/// </para>
/// <para>
/// 若直接删开关而不降级，用户会永久失去该耗时日志——本测试正是锁定「不丢可诊断性」这一半。
/// </para>
/// </remarks>
public class DiagnosticsSwitchTests
{
    private static string? GetSolutionRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir) && !File.Exists(Path.Combine(dir, "Mud.Feishu.slnx")))
        {
            dir = Path.GetDirectoryName(dir);
        }
        return dir;
    }

    [Theory]
    [InlineData(typeof(FeishuWebhookOptions), "EnablePerformanceMonitoring")] // audit-allow: guard must name the removed property
    [InlineData(typeof(FeishuWebhookOptions), "GetEffectiveEnablePerformanceMonitoring")] // audit-allow: guard must name the removed member
    [InlineData(typeof(FeishuAppWebhookOptions), "EnablePerformanceMonitoring")] // audit-allow: guard must name the removed property
    [InlineData(typeof(FeishuAppWebhookOptions), "GetEffectiveEnablePerformanceMonitoring")] // audit-allow: guard must name the removed member
    public void RemovedPerformanceMonitoringMember_ShouldNotExist(Type type, string memberName)
    {
        var member = (System.Reflection.MemberInfo?)type.GetProperty(memberName)
            ?? type.GetMethod(memberName);

        member.Should().BeNull(
            $"{type.Name}.{memberName} 属 R5.2/X10 判定删除的诊断开关（耗时日志已无条件降为 Debug）");
    }

    [Fact]
    public void TimingLog_ShouldBeUnconditional_AndAtDebugLevel()
    {
        var root = GetSolutionRoot();
        root.Should().NotBeNull("测试需定位解决方案根目录以做源码断言");

        var path = Path.Combine(root!, "Mud.Feishu.Webhook", "Services", "FeishuWebhookService.cs");
        File.Exists(path).Should().BeTrue($"耗时日志所在源文件必须存在：{path}");

        var lines = File.ReadAllLines(path);
        var logLineIndex = Array.FindIndex(lines, l => l.Contains("事件处理耗时"));
        logLineIndex.Should().BeGreaterThan(0, "耗时日志必须仍然存在（删除开关的前提是保留可诊断性）");

        // 该日志必须是 LogDebug 调用（其调用点通常在消息字符串前 1–3 行）
        var window = string.Join('\n', lines.Skip(Math.Max(0, logLineIndex - 3)).Take(3));
        window.Should().Contain("LogDebug",
            "耗时日志必须无条件以 Debug 级别输出（X10：删开关不丢可诊断性）");
        window.Should().NotContain("LogInformation",
            "耗时日志不得回到 Information（否则日志噪音问题原样复现）");
    }

    [Fact]
    public void TimingLog_ShouldNotBeGatedByAnyPerformanceSwitch()
    {
        var root = GetSolutionRoot();
        root.Should().NotBeNull();

        var path = Path.Combine(root!, "Mud.Feishu.Webhook", "Services", "FeishuWebhookService.cs");
        var text = File.ReadAllText(path);

        // 迁移说明注释仍会提到已删键名（带 audit-allow），故只断言「不得再被 if 条件门控」
        System.Text.RegularExpressions.Regex
            .IsMatch(text, @"if\s*\([^)]*PerformanceMonitoring")
            .Should().BeFalse("耗时日志不得再依赖任何性能监控开关（X10 清理目标）");
    }
}
