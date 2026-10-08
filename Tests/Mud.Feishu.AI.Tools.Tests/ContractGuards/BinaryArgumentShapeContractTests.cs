// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法��纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// R5 / F-12 + F-6：<b>工具参数不得出现二进制/base64 语义</b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么必须机械锁死</b>：飞书的部分接口（OCR/STT）入参就是 <c>base64</c>，
/// 而 AIDocument 走本地文件 multipart —— 若有人图省事把 <c>file_base64</c> 暴露成工具参数，
/// 就会把<b>整段二进制塞进模型上下文</b>。这是"构建通过、Schema 合法、但灾难性"的一类，
/// 编译期无从拦截，只能靠守卫。
/// </para>
/// <para>
/// <b>与 B-2 的分工</b>：B-2 守"<c>Source</c> 不得指向二进制<b>返回</b>方法"（输出侧）；
/// 本守卫守"<b>参数</b>不得是二进制形态"（输入侧）。合起来才是完整的二进制防线。
/// </para>
/// <para>
/// <b>反向自证</b>：本守卫同时断言"确实扫到了工具接口文件"，否则空集合会假绿。
/// </para>
/// </remarks>
public class BinaryArgumentShapeContractTests
{
    /// <summary>被判定为非法的参数声明类型（二进制形态）。</summary>
    private static readonly string[] ForbiddenTypes =
    [
        "byte[]",
        "byte[]?",
        "ReadOnlyMemory<byte>",
        "ReadOnlyMemory<byte>?",
        "Stream",
        "Stream?",
        "MemoryStream",
        "MemoryStream?",
    ];

    /// <summary>参数名里出现即判非法的关键词（base64 语义）。</summary>
    private static readonly string[] ForbiddenNameFragments =
    [
        "base64",
        "base_64",
        "file_bytes",
        "filebytes",
        "raw_bytes",
    ];

    [Fact]
    public void ToolParameters_ShouldNeverBeBinaryOrBase64()
    {
        var violations = new List<string>();
        var scannedFiles = 0;

        foreach (var file in EnumerateCurationFiles())
        {
            scannedFiles++;
            var source = File.ReadAllText(file);

            foreach (var line in source.Split('\n'))
            {
                // 只看 [ToolParameter(...)] 所在行：参数声明与特性在同一行（本仓体例）。
                if (!line.Contains("[ToolParameter(", StringComparison.Ordinal))
                {
                    continue;
                }

                var tail = line.Contains(']', StringComparison.Ordinal) ? line[(line.LastIndexOf(']') + 1)..] : line;

                foreach (var forbidden in ForbiddenTypes)
                {
                    if (tail.Contains(forbidden, StringComparison.Ordinal))
                    {
                        violations.Add(
                            $"{Path.GetFileName(file)}: 参数声明为二进制类型 '{forbidden}' ⇒ {line.Trim()}");
                    }
                }

                var nameMatch = System.Text.RegularExpressions.Regex.Match(line, @"\[ToolParameter\(""(?<name>[^""]+)""");
                if (nameMatch.Success)
                {
                    var name = nameMatch.Groups["name"].Value;
                    foreach (var fragment in ForbiddenNameFragments)
                    {
                        if (name.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                        {
                            violations.Add(
                                $"{Path.GetFileName(file)}: 参数名 '{name}' 含 base64/字节语义 ⇒ 二进制绝不许进入模型上下文");
                        }
                    }
                }
            }
        }

        scannedFiles.Should().BeGreaterThan(
            0,
            "未扫到任何 Curation/ 下的工具接口文件——扫描路径错了（假绿），请先修守卫");

        violations.Should().BeEmpty(
            "工具参数不得是二进制或 base64 语义（二进制一旦进入模型上下文即灾难）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// <b>反向自证</b>：守卫的判据必须真的能识别非法形态。
    /// 否则"扫描器坏了"和"代码干净"无法区分（§13.3 的元教训）。
    /// </summary>
    [Fact]
    public void BinaryDetector_ShouldActuallyRecognizeBinaryDeclarations()
    {
        const string illegalLine =
            """[ToolParameter("file_base64", "文件内容（base64）")] byte[] file_base64,""";

        illegalLine.Should().Contain("byte[]", "样本必须含二进制类型声明");
        ForbiddenTypes.Should().Contain(t => illegalLine.Contains(t, StringComparison.Ordinal));

        var nameMatch = System.Text.RegularExpressions.Regex.Match(illegalLine, @"\[ToolParameter\(""(?<name>[^""]+)""");
        nameMatch.Success.Should().BeTrue("样本必须能解析出参数名");
        ForbiddenNameFragments.Should().Contain(
            f => nameMatch.Groups["name"].Value.Contains(f, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> EnumerateCurationFiles()
    {
        var directory = Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Curation");
        return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories);
    }

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
