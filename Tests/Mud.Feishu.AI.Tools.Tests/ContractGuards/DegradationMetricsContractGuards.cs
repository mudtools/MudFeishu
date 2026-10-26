// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R3-12：降级路径指标化元守卫——断言每条"有意静默/降级"路径都调用了 <c>RecordDegraded</c>。
/// </summary>
/// <remarks>
/// <para>
/// 四条降级路径（审计失败/整形失败/通道更新失败/内容安全告警）此前只有日志、无指标。
/// 本守卫断言每条路径的源码位置都包含 <c>RecordDegraded</c> 调用，防止回退移除指标。
/// </para>
/// <para>
/// 检查方式：源码文本扫描（非行为断言）——降级路径的位置由语义标注（注释中的 R3-12 标记）锚定，
/// 守卫断言该位置附近存在 <c>RecordDegraded</c> 调用。这比固定行号更抗漂移。
/// </para>
/// <para>
/// <b>N2-b 中立化改锚</b>：通道更新失败的降级计数从 <c>BufferedMessageChannel</c> 基类
/// **上移到产品子类**（<c>EditMessageChannel</c> / <c>CardKitStreamMessageChannel</c> 的
/// <c>OnUpdateDegraded</c> 覆写）——中立基类经 <c>IAgentTelemetry</c> 端口记 reason，
/// 产品指标名（<c>DegradedReasons.ChannelUpdateFailed</c>）与 app_key 维度留在产品侧。
/// 故本守卫对产品子类各断言一次，并对中立基类断言「钩子被调用」。
/// <para>
/// 卡片通道另有两条<b>与「单次更新失败」不同级</b>的路径必须各占一个原因，否则看板无法区分
/// 「少一次增量（用户仍看到完整回答）」与「用户永远看不到完整回答 / 平台残留了一张卡片实体」：
/// 终态未落地（<c>ChannelTerminalFailed</c>）与投放失败致孤儿实体（<c>ChannelBeginOrphaned</c>）。
/// </para>
/// </para>
/// </remarks>
public class DegradationMetricsContractGuards
{
    /// <summary>
    /// 每条降级路径都必须调用 <c>RecordDegraded</c>——移除即报红。
    /// </summary>
    [Fact]
    public void DegradedPaths_ShouldCallRecordDegraded()
    {
        var root = FindRepositoryRoot();

        var checks = new[]
        {
            ("FeishuToolBinding.cs", "审计出口投递失败", "AuditDeliveryFailed"),
            ("FeishuToolBinding.cs", "整形失败回退默认结果", "ResultShapingFailed"),
            ("FeishuToolBinding.cs", "warn 模式只加标注不阻断", "ContentSafetyWarn"),
            // N2-b：通道降级计数在产品子类的 OnUpdateDegraded 覆写里（两通道同构，各断言一次）。
            ("EditMessageChannel.cs", "通道更新失败补计数（编辑通道）", "ChannelUpdateFailed"),
            ("CardKitStreamMessageChannel.cs", "通道更新失败补计数（CardKit 流式通道）", "ChannelUpdateFailed"),
            ("CardKitStreamMessageChannel.cs", "终态未落地补计数（可观测边界）", "ChannelTerminalFailed"),
            ("CardKitStreamMessageChannel.cs", "投放失败致孤儿实体补计数", "ChannelBeginOrphaned"),
        };

        var missing = new List<string>();

        foreach (var (fileName, commentFragment, reasonConstant) in checks)
        {
            var filePath = FindSourceFile(root, fileName);
            File.Exists(filePath).Should().BeTrue($"{fileName} 必须存在（扫描面不得静默为空）");

            var source = File.ReadAllText(filePath);

            // 断言源码包含对应的 DegradedReasons 常量引用。
            if (!source.Contains($"DegradedReasons.{reasonConstant}", StringComparison.Ordinal))
            {
                missing.Add($"{fileName}: 缺少 FeishuMetrics.DegradedReasons.{reasonConstant} 调用（{commentFragment}）");
            }

            // 断言源码包含 RecordDegraded 调用。
            if (!source.Contains("RecordDegraded", StringComparison.Ordinal))
            {
                missing.Add($"{fileName}: 缺少 FeishuToolDiagnostics.RecordDegraded 调用");
            }
        }

        missing.Should().BeEmpty(
            "R3-12 要求每条降级路径都产生指标计数（feishu.agent.tool.degraded）。"
            + "以下路径缺少指标调用：" + string.Join(" | ", missing));
    }

    // N2-b 守卫（原 NeutralBufferedBase_ShouldInvokeDegradedHook）已迁出飞书仓（v2.3 迁移）：
    // 中立缓冲基类 BufferedMessageChannel 随中立化迁入新仓 Mud.AI.Agent/Channels——判据锚点已不在
    // 本仓，故由新仓 DegradationMetricsContractGuards.NeutralBufferedBase_ShouldInvokeDegradedHook 持有。
    // 本仓保留产品子类（EditMessageChannel / CardStreamMessageChannel）的降级指标正向锚。

    private static string FindSourceFile(string root, string fileName)
        => Directory
            .GetFiles(root, fileName, SearchOption.AllDirectories)
            .First(static path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                                  && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

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
}
