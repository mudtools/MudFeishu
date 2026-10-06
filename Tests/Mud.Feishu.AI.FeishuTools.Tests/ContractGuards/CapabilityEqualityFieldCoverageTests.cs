// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.FeishuTools.Tests.ContractGuards;

/// <summary>
/// <b>R5 / B-10</b>：增量管线值键的<b>相等性字段集元守卫</b> ——
/// 断言 <c>CapabilityEntry</c> / <c>CapabilityParameter</c> / <c>ToolSchemaModel</c>
/// 的<b>每一个构造参数都参与 <c>Equals</c> 与 <c>GetHashCode</c></b>。
/// </summary>
/// <remarks>
/// <para>
/// <b>缺陷背景（B-10 · 第三次漏字段风险）</b>：Roslyn 增量管线的正确性依赖
/// "相等字段集 ⊇ 影响产物的事实字段集"，这是<b>纯人工不变量</b>，且失败模式<b>静默</b>：
/// 漏字段 ⇒ 改了接口不重生成 ⇒ 产物过期 ⇒ Schema 与真实工具面不一致。
/// 本仓已因此漏过 <b>2 次</b>（<c>ToolSchemaModel</c> 曾漏 <c>Source</c>（AT-B16）、
/// <c>CapabilityParameter</c> 曾漏文档描述），修复时补了字段却<b>没补任何守卫</b>。
/// </para>
/// <para>
/// <b>本守卫的判据（为什么选"构造参数 ⊆ 相等字段"）</b>：构造参数是"一个值对象携带的全部事实"的
/// 唯一权威列举。若某个事实进了构造参数却没进 <c>Equals</c>，它就是静默漂移面；
/// 反向（进了 Equals 但不是构造参数）在本仓不存在，故只锁这一个方向即可覆盖全部风险。
/// </para>
/// <para>
/// <b>为什么用源码扫描而不是反射</b>：本仓生成器是 <c>netstandard2.0</c> +
/// <c>IsPackable=false</c> 的 Analyzer 工程，测试工程<b>无法</b>以符号方式引用它；
/// 且"字段是否入哈希"是<b>方法体</b>事实，反射看不到。源码扫描与本目录既有守卫体例一致。
/// </para>
/// </remarks>
public class CapabilityEqualityFieldCoverageTests
{
    private const string GeneratorDirectory = "Mud.Feishu.AI.Tools";

    /// <summary>
    /// <c>CapabilityEntry</c>：全部构造字段必须参与 <c>Equals</c> 与 <c>GetHashCode</c>。
    /// </summary>
    [Fact]
    public void CapabilityEntry_EveryConstructorParameter_ShouldParticipateInEqualityAndHash()
        => AssertCtorFieldsParticipate("CapabilityEntry", "CapabilityEntry.cs");

    /// <summary>
    /// <c>CapabilityParameter</c>：全部构造字段（含<b>文档描述</b>——AT-B16 曾漏的那个）必须参与。
    /// </summary>
    [Fact]
    public void CapabilityParameter_EveryConstructorParameter_ShouldParticipateInEqualityAndHash()
        => AssertCtorFieldsParticipate("CapabilityParameter", "CapabilityParameter.cs");

    /// <summary>
    /// <c>ToolSchemaModel</c>：全部构造字段必须参与（AT-B16 的教训：曾漏 <c>Source</c>）。
    /// </summary>
    [Fact]
    public void ToolSchemaModel_EveryConstructorParameter_ShouldParticipateInEqualityAndHash()
        => AssertCtorFieldsParticipate("ToolSchemaModel", "ToolSchemaModel.cs");

    // ────────── 断言实现 ──────────

    /// <summary>
    /// 断言指定类型的<b>全部构造参数</b>都出现在 <c>Equals</c> 与 <c>GetHashCode</c> 的方法体里。
    /// </summary>
    private static void AssertCtorFieldsParticipate(string typeName, string typeFileName)
    {
        var source = FindGeneratorSource(typeFileName);

        var ctorParameters = ReadConstructorParameters(source, typeName);
        ctorParameters.Should().NotBeEmpty(
            $"未能从 {typeFileName} 解析出 {typeName} 的构造参数——解析规则已失效（假绿），请先修守卫");

        var equalsBody = ReadMethodBody(source, $"public bool Equals({typeName}? other)");
        var hashBody = ReadMethodBody(source, "public override int GetHashCode()");

        equalsBody.Should().NotBeNullOrEmpty($"{typeName}.Equals 方法体未找到（守卫解析失效）");
        hashBody.Should().NotBeNullOrEmpty($"{typeName}.GetHashCode 方法体未找到（守卫解析失效）");

        // 集合字段在 Equals/哈希里走 *Equal / Count + foreach 辅助方法，形如 "ParametersEqual(Parameters, ...)"，
        // 因此判据取"字段名在方法体中出现"——对标量与集合两种形态都成立。
        // 比较必须**忽略大小写**：构造参数是 camelCase（interfaceName），而方法体里用的是属性名（InterfaceName）。
        var missingFromEquals = ctorParameters
            .Where(f => !equalsBody.Contains(f, StringComparison.OrdinalIgnoreCase)).ToArray();
        var missingFromHash = ctorParameters
            .Where(f => !hashBody.Contains(f, StringComparison.OrdinalIgnoreCase)).ToArray();

        missingFromEquals.Should().BeEmpty(
            "{0} 的构造字段未参与 Equals —— 增量管线会漏重算（改了接口却不重新生成，产物静默过期）：{1}",
            typeName,
            string.Join(", ", missingFromEquals));

        missingFromHash.Should().BeEmpty(
            "{0} 的构造字段未参与 GetHashCode —— 仅在某字段上不同的两个条目会得到相同哈希，"
            + "使增量管线退化为逐条重跑：{1}",
            typeName,
            string.Join(", ", missingFromHash));
    }

    /// <summary>读取构造函数（或主构造器）的参数名列表。</summary>
    private static IReadOnlyList<string> ReadConstructorParameters(string source, string typeName)
    {
        // 形如：public CapabilityEntry(\n  string interfaceName,\n  ... )\n    {
        var block = Regex.Match(
            source,
            $@"public\s+{Regex.Escape(typeName)}\s*\((?<body>.*?)\)\s*(?:;|\{{)",
            RegexOptions.Singleline);

        if (!block.Success)
        {
            return [];
        }

        return
        [
            .. Regex.Matches(block.Groups["body"].Value, @"(?<name>\w+)\s*(?:=[^,]+)?\s*(?:,|$)")
                .Select(static m => m.Groups["name"].Value)
                .Where(static name => name.Length > 0)
        ];
    }

    /// <summary>按签名定位方法并返回其方法体（花括号配平）。</summary>
    /// <remarks>
    /// <b>必须同时支持表达式体成员</b>：<c>ToolSchemaModel.Equals</c> 是
    /// <c>public bool Equals(ToolSchemaModel? other) => ...;</c> 形态，<b>没有花括号</b>。
    /// 若一律去找下一个 <c>{</c>，会把<b>后面另一个方法（如 <c>GetHashCode</c>）的体</b>
    /// 当成本方法的体 ⇒ 漏字段时守卫仍然全绿（假绿）。
    /// 判据：签名之后若先遇到 <c>;</c> 而非 <c>{</c>，即为表达式体，取到分号为止。
    /// </remarks>
    private static string ReadMethodBody(string source, string signaturePrefix)
    {
        var start = source.IndexOf(signaturePrefix, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var open = source.IndexOf('{', start);
        var semicolon = source.IndexOf(';', start);

        // 表达式体成员：分号先于花括号（本仓 3 个值键的 Equals 里有 2 个是表达式体）。
        if (semicolon >= 0 && (open < 0 || semicolon < open))
        {
            return source[start..(semicolon + 1)];
        }

        if (open < 0)
        {
            return string.Empty;
        }

        var depth = 0;
        for (var i = open; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[open..(i + 1)];
                }
            }
        }

        return string.Empty;
    }

    private static string FindGeneratorSource(string fileName)
    {
        var root = Path.Combine(FindRepositoryRoot(), GeneratorDirectory.Replace('/', Path.DirectorySeparatorChar));
        var files = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);

        files.Should().HaveCount(
            1,
            $"{fileName} 应在 {GeneratorDirectory} 下唯一存在（实际 {files.Length} 个）——多份会让守卫读到错误副本");

        return File.ReadAllText(files[0]);
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