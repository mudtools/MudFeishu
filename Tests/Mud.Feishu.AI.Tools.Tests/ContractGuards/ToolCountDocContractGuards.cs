// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 文档真相源收敛守卫（T13）：锁定 <c>Readme.md</c> §1 表格中的合计数字与编译期产物
/// <c>FeishuToolNames.All.Count</c> 一致，防止文档与代码漂移。
/// </summary>
/// <remarks>
/// <para>
/// <b>背景</b>：<c>Readme.md</c> §1 表格曾长期写「88 只读 + 74 写 = 162」，而实际为 182
/// （缺 6 个域共 20 条）。本守卫解析 Readme 表格的合计行数字，与
/// <see cref="FeishuToolNames.All"/> 的实际计数机械比对。
/// </para>
/// <para>
/// <b>为什么不逐行校验</b>：域级分解（哪个域几条）会随策展演进而变，
/// 但合计数是稳定不变量——只要总数对，分域表的内部拆分不会造成「文档说有但代码没有」
/// 或「代码有但文档没提到」的误导。
/// </para>
/// </remarks>
public class ToolCountDocContractGuards
{
    /// <summary>
    /// Readme §1 表格的合计行数字必须等于 <see cref="FeishuToolNames.All"/> 的实际计数。
    /// </summary>
    [Fact]
    public void ReadmeToolCount_ShouldMatch_CompiledToolNames()
    {
        var readmePath = FindReadme();
        var content = File.ReadAllText(readmePath);

        var (readOnly, writeCount, total) = ParseReadmeTableTotals(content);

        readOnly.Should().Be(FeishuToolNames.ReadonlyAll.Length,
            "Readme §1 表格只读合计必须等于 FeishuToolNames.ReadonlyAll.Length");

        writeCount.Should().Be(FeishuToolNames.WriteAll.Length,
            "Readme §1 表格写类合计必须等于 FeishuToolNames.WriteAll.Length");

        total.Should().Be(FeishuToolNames.All.Length,
            "Readme §1 表格总计必须等于 FeishuToolNames.All.Length");
    }

    /// <summary>
    /// 定位 <c>Readme.md</c>——测试运行目录在 bin/ 下，回溯到项目根。
    /// </summary>
    private static string FindReadme()
    {
        var dir = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine(dir, "Mud.Feishu.AI.Tools", "Readme.md");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }

        throw new FileNotFoundException("无法定位 Mud.Feishu.AI.Tools/Readme.md（从测试运行目录回溯 8 级未找到）");
    }

    /// <summary>
    /// 解析 Readme §1 表格的合计行，提取只读/写类/总计三个数字。
    /// </summary>
    /// <remarks>
    /// 合计行形如 <c>| **合计** | **108** | **74** | **182** |</c>。
    /// </remarks>
    private static (int ReadOnly, int Write, int Total) ParseReadmeTableTotals(string content)
    {
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!trimmed.StartsWith("|") || !trimmed.Contains("合计"))
            {
                continue;
            }

            var parts = trimmed.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length < 4)
            {
                continue;
            }

            // parts[0] = "**合计**", parts[1] = "**108**", parts[2] = "**74**", parts[3] = "**182**"
            var readOnly = ParseBoldNumber(parts[1]);
            var write = ParseBoldNumber(parts[2]);
            var total = ParseBoldNumber(parts[3]);

            if (readOnly.HasValue && write.HasValue && total.HasValue)
            {
                return (readOnly.Value, write.Value, total.Value);
            }
        }

        throw new InvalidOperationException("无法从 Readme.md 解析合计行数字（查找含「合计」的表格行失败）");
    }

    /// <summary>解析 <c>**108**</c> 形态的加粗数字。</summary>
    private static int? ParseBoldNumber(string text)
    {
        var cleaned = text.Replace("*", "").Replace("—", "0").Trim();
        return int.TryParse(cleaned, out var value) ? value : null;
    }
}
