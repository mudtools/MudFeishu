// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// <b>A10 二进制防线</b>（参数侧）：工具契约面（<c>Curation/</c>）不得出现承载字节的类型。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么要独立于"返回值"守卫</b>：返回值侧已由
/// <c>BinaryDownloadToolExposureContractTests</c> 锁死（不得返回二进制 / Source 不得指向二进制方法）。
/// 但<b>参数侧</b>此前没有守门人：一个 <c>byte[]</c>/<c>Stream</c> 参数意味着"模型必须把 MB 级内容塞进
/// 工具调用 JSON"——上下文立刻爆掉，且这类参数在进程内库中根本无法由模型表达
/// （DP-C3-1 否决的正是这个形态：OCR base64 图 / 文档识别 FormContent / STT base64 音频均不策展）。
/// </para>
/// <para>
/// <b>历史（DP-R7-2，2026-10-10）</b>：本守卫的前身是把"契约类型名"（<c>BinaryArtifact</c> /
/// <c>StoredArtifact</c> / <c>ResolvedBinaryArtifact</c>）当作禁词扫描——那两个宿主契约（出入成对）
/// 因<b>零消费方且与 <c>IFeishuAttachmentStager</c> 职责重叠</b>被本轮裁定删除，随后本守卫改为
/// 直接锁"承载字节的类型"这一<b>与具体契约无关</b>的不变量：这样即使将来有人新造字节载体，
/// 只要它进了工具参数就被拦下。
/// </para>
/// </remarks>
public class ByteBearingSurfaceContractGuards
{
    private const string CurationDirectory = "Mud.Feishu.AI.Tools/Curation";

    /// <summary>
    /// 承载字节的类型形态（正则）。刻意<b>不含</b> <c>byte</c> 标量——单字节数值是合法的工具参数。
    /// </summary>
    private const string ByteBearingPattern =
        @"(?:byte\s*\[\s*\]|Memory\s*<\s*byte\s*>|Stream\b|IFormFile\b)";

    [Fact]
    public void ToolParameters_ShouldNeverBeByteBearing()
    {
        var root = FindRepositoryRoot();
        var curation = Path.Combine(root, CurationDirectory.Replace('/', Path.DirectorySeparatorChar));
        Directory.Exists(curation).Should().BeTrue($"工具契约目录必须存在：{CurationDirectory}");

        var files = Directory.GetFiles(curation, "*.cs", SearchOption.AllDirectories);
        files.Should().NotBeEmpty("契约面为空 ⇒ 扫描面失效（假绿）");

        var violations = new List<string>();
        foreach (var file in files)
        {
            // 只扫代码行：XML 注释里解释"为什么这些入参不可策展"是正确写法，一并扫会把合规文档判成违规。
            var code = ReadCodeLines(file);

            if (System.Text.RegularExpressions.Regex.IsMatch(code, ByteBearingPattern))
            {
                violations.Add(Path.GetFileName(file));
            }
        }

        violations.Should().BeEmpty(
            "以下契约文件出现了承载字节的类型（byte[] / Memory<byte> / Stream）——"
            + "字节不得穿越工具面（A10）：模型的工具入参在进程内库中无法表达 MB 级内容，"
            + "且会击穿上下文与 SSE 帧。正确形态是宿主侧转换（宿主把 URL/本地文件交给 SDK 的 "
            + "[FormContent]/base64 参数），工具面只接受文本：{0}",
            string.Join(" | ", violations));
    }

    [Fact]
    public void Scanner_ShouldDetectByteBearingParameters_OtherwiseGuardIsFalseGreen()
    {
        var synthetic = """
            public interface IFakeTool
            {
                Task<string> UploadAsync(byte[] payload, CancellationToken ct = default);
            }
            """;

        System.Text.RegularExpressions.Regex
            .IsMatch(synthetic, ByteBearingPattern)
            .Should().BeTrue("判据必须真的认得出 byte[] 参数，否则上面的断言恒绿（假门禁）");

        System.Text.RegularExpressions.Regex
            .IsMatch("""Task<string> QueryAsync(string token, int limit);""", ByteBearingPattern)
            .Should().BeFalse("普通参数不得被误判（误报会让守卫被绕过或删除）");
    }

    /// <summary>读取源码的代码行（丢弃 <c>///</c> 注释行与行内注释后的内容）。</summary>
    private static string ReadCodeLines(string path)
        => string.Join(
            '\n',
            File.ReadAllLines(path)
                .Where(static line => !line.TrimStart().StartsWith("///", StringComparison.Ordinal))
                .Select(static line =>
                {
                    var comment = line.IndexOf("//", StringComparison.Ordinal);
                    return comment >= 0 ? line[..comment] : line;
                }));

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
