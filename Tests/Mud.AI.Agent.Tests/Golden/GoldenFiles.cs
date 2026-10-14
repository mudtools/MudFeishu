// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.AI.Agent.Tests.Golden;

/// <summary>
/// 黄金快照比对设施（中立化重构方案 §7.0 阶段 0 / N6 · D2 迁移）：把「确定性纯机制」的当前行为
/// 固化为 <c>*.golden.txt</c>——「搬迁时顺手改了语义」由此可见。
/// </summary>
/// <remarks>
/// <para>
/// <b>D2 迁移说明</b>：快照与断言自 <c>Tests/Mud.Feishu.AI.Tests/Golden</c> 整体迁入
/// （测试随类型走，P3 上游化时覆盖跟着中立包离开飞书仓）；迁移要求快照文本<b>零漂移</b>——
/// 依赖 <c>FeishuAgent</c> 的会话工厂与产品事实（<c>FeishuSummarizerFacts</c> /
/// <c>KnowledgeContextAssembler</c> 头尾文案）改为中立等价形态（见 <see cref="NeutralizationGoldenTests"/>）。
/// </para>
/// <para>
/// <b>重固化通道</b>：<c>GOLDEN_UPDATE=true</c> 环境下运行测试即用当前行为重写快照。
/// 该通道是<b>显式行为变更</b>的唯一合法路径——任何快照 diff 都必须先判定
/// 「是搬迁瑕疵还是刻意变更」，后者才允许重固化，并在提交信息里说明理由。
/// </para>
/// </remarks>
internal static class GoldenFiles
{
    /// <summary>快照目录（仓库根/Tests/Mud.AI.Agent.Tests/Golden）。</summary>
    public static string Directory { get; } = Path.Combine(
        FindRepositoryRoot(), "Tests", "Mud.AI.Agent.Tests", "Golden");

    /// <summary>
    /// 把实际输出与快照比对；<c>GOLDEN_UPDATE=true</c> 时重写快照并通过。
    /// </summary>
    /// <param name="name">快照名（不含扩展名）。</param>
    /// <param name="actual">实际输出（换行统一为 <c>\n</c>）。</param>
    public static void Verify(string name, string actual)
    {
        var path = Path.Combine(Directory, $"{name}.golden.txt");
        var normalized = Normalize(actual);

        if (string.Equals(Environment.GetEnvironmentVariable("GOLDEN_UPDATE"), "true", StringComparison.OrdinalIgnoreCase))
        {
            System.IO.Directory.CreateDirectory(Directory);
            File.WriteAllText(path, normalized);
            return;
        }

        File.Exists(path).Should().BeTrue(
            "黄金快照缺失——首次运行请以 GOLDEN_UPDATE=true 生成，"
            + "此后任何 diff 都是行为变更，必须显式重固化：{0}", path);

        var expected = Normalize(File.ReadAllText(path));
        expected.Should().Be(normalized,
            "黄金快照不一致——中立化迁移要求零行为变化。若为刻意变更，请以 GOLDEN_UPDATE=true 重固化并在提交信息中说明：{0}", path);
    }

    /// <summary>换行归一化（快照文件与断言两侧一致，杜绝 CRLF 误报）。</summary>
    private static string Normalize(string text)
        => text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').TrimEnd('\n');

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Mud.Feishu.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"未能从 {AppContext.BaseDirectory} 向上定位仓库根目录（Mud.Feishu.slnx）");
    }
}
