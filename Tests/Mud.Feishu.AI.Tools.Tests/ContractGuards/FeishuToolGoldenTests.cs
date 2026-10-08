// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text;
using Mud.Feishu.AI.Tools.Generated;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// 工具描述符 golden 快照守卫：把「模型可见的工具契约」钉死为可审阅的文本，
/// 任何描述/参数/scope/风险的静默漂移都必须显式改写快照。
/// </summary>
/// <remarks>
/// <para>
/// <b>双重加锁</b>（对齐本仓库既有的"只断言诊断计数 = 假绿"教训）：
/// </para>
/// <list type="number">
/// <item>构建期：<c>Mud.Feishu.AI.FeishuTools.csproj</c> 以 <c>AdditionalFiles</c> 声明本快照，
/// 源生成器逐字节比对，漂移即 <c>MUDFT014</c> 并中断构建；</item>
/// <item>测试期：本用例再独立比对一次（TRX 可见，能进 <c>verify-build.ps1</c> 的
/// "空 TRX = 假绿"防呆），并在快照缺失时报错（防止有人删掉 AdditionalFiles 让门禁静默消失）。</item>
/// </list>
/// <para>
/// <b>重新固化</b>（仅当契约确实要变）：设置环境变量后运行本用例——
/// <c>$env:FeishuToolGoldenUpdate='true'; dotnet test Tests/Mud.Feishu.AI.FeishuTools.Tests --filter "FullyQualifiedName~FeishuToolGoldenTests"</c>。
/// 固化后的 diff 必须连同 CHANGELOG 一并提交评审（工具名/描述/scope/风险是模型可见契约）。
/// </para>
/// </remarks>
public class FeishuToolGoldenTests
{
    private const string UpdateFlag = "FeishuToolGoldenUpdate";
    private const string GoldenFileName = "FeishuToolSchemas.golden.txt";
    private const string OwnerProjectDirectory = "Mud.Feishu.AI.FeishuTools";

    /// <summary>
    /// 生成器产出的描述符集必须与 golden 快照逐字节一致。
    /// </summary>
    [Fact]
    public void ToolDescriptors_ShouldMatchGoldenSnapshot_ByteForByte()
    {
        var goldenPath = Path.Combine(FindRepositoryRoot(), OwnerProjectDirectory, GoldenFileName);
        var actual = BuildGoldenText();

        if (IsUpdateRequested())
        {
            File.WriteAllText(goldenPath, actual, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return;
        }

        File.Exists(goldenPath).Should().BeTrue(
            $"{OwnerProjectDirectory}/{GoldenFileName} 必须存在——它是工具描述符的门禁快照（缺失会让构建期的 MUDFT014 静默失效）。" +
            $"重新固化见 {nameof(FeishuToolGoldenTests)} 的 XML 注释。");

        var expected = File.ReadAllText(goldenPath, Encoding.UTF8).Replace("\r\n", "\n");
        actual.Should().Be(expected,
            "工具描述符与 golden 快照不一致（描述/参数/scope/风险漂移）——" +
            "确属有意变更时按测试类注释重新固化并同步 CHANGELOG");
    }

    /// <summary>
    /// 快照必须覆盖全部契约工具（防"快照只剩一条"的退化，使比对沦为恒真）。
    /// </summary>
    [Fact]
    public void GoldenSnapshot_ShouldCoverEveryContractTool()
    {
        var lines = BuildGoldenText()
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        lines.Should().HaveCount(FeishuToolSchemas.SchemaByToolName.Count,
            "快照行数必须等于 Schema 注册表条目数");

        lines.Should().OnlyContain(line => line.Contains('\t', StringComparison.Ordinal),
            "每行形如 <toolName>\\t<schema>（缺分隔符说明格式被破坏）");
    }

    /// <summary>构造与生成器完全一致的快照文本（按工具名序数排序，一行一工具）。</summary>
    private static string BuildGoldenText()
    {
        var builder = new StringBuilder();
        foreach (var pair in FeishuToolSchemas.SchemaByToolName.OrderBy(static p => p.Key, StringComparer.Ordinal))
        {
            builder.Append(pair.Key).Append('\t').Append(pair.Value).Append('\n');
        }

        return builder.ToString();
    }

    private static bool IsUpdateRequested()
        => string.Equals(
            Environment.GetEnvironmentVariable(UpdateFlag),
            "true",
            StringComparison.OrdinalIgnoreCase);

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
