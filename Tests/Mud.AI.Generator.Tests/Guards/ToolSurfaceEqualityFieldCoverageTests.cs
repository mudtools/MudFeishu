// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.AI.Generator.Tests;

/// <summary>
/// ToolSurface 增量管线值键的<b>相等性字段集元守卫</b> ——
/// 断言 <c>CapabilityEntry</c> / <c>CapabilityParameter</c> / <c>ToolSchemaModel</c>
/// 的每一个构造参数都参与 <c>Equals</c> 与 <c>GetHashCode</c>。
/// </summary>
/// <remarks>
/// <para>
/// <b>为什么本守卫在组件侧（迁移台账）</b>：该守卫原先是下游仓
/// （<c>MudFeishu/Tests/Mud.Feishu.AI.FeishuTools.Tests/ContractGuards/CapabilityEqualityFieldCoverageTests.cs</c>）
/// 的资产，通过源码扫描下游工具面生成器（<c>Mud.Feishu.AI.Tools</c>）的这三个类型实现。工具面引擎
/// 上游化到本仓后，那三个类型<b>只存在于本仓</b>，下游既无源码也无符号可达
/// （生成器是 analyzer 包，下游无法符号引用）⇒ 若不在本仓承接，这条机械约束会随引擎上移而<b>静默消失</b>
/// （"守卫不是被删除，而是被遗忘"）。
/// </para>
/// <para>
/// <b>缺陷背景（AT-B16 及其泛化）</b>：Roslyn 增量管线的正确性依赖
/// "相等字段集 ⊇ 影响产物的事实字段集"，这是纯人工不变量，且失败模式<b>静默</b>：
/// 漏字段 ⇒ 改了接口不重生成 ⇒ 产物过期 ⇒ Schema 与真实工具面不一致。
/// 本仓已因此漏过两次（<c>ToolSchemaModel</c> 曾漏 <c>Source</c>、<c>CapabilityParameter</c> 曾漏文档描述），
/// 修复时补了字段却<b>没补任何守卫</b>——两处实现至今仍以"AT-B16"注释引用这次事故。
/// </para>
/// <para>
/// <b>判据（为什么选"构造参数 ⊆ 相等字段"）</b>：构造参数是"一个值对象携带的全部事实"的唯一权威列举。
/// 若某个事实进了构造参数却没进 <c>Equals</c>，它就是静默漂移面；反向（进了 <c>Equals</c> 但不是构造参数）
/// 在本仓不存在，故只锁这一个方向即可覆盖全部风险。
/// </para>
/// <para>
/// <b>为什么用源码扫描而不是反射</b>："字段是否入哈希"是<b>方法体</b>事实，反射看不到；
/// 且本仓这三个类型的方法体是本守卫的比对对象本身。源码扫描与 <c>Guards/</c> 目录既有守卫体例一致
/// （见 <see cref="GeneratorSourceContractTests"/>）。
/// </para>
/// </remarks>
public class ToolSurfaceEqualityFieldCoverageTests
{
    /// <summary>三值键所在的源码目录（相对仓库根）。</summary>
    private static readonly string[] ValueKeyDirectory = ["Mud.AI.Generator", "ToolSurface", "Extraction"];

    /// <summary><c>CapabilityEntry</c>：全部构造字段必须参与 <c>Equals</c> 与 <c>GetHashCode</c>。</summary>
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

    /// <summary>
    /// <b>反向自证</b>：三值键必须都被这套规则<b>真的解析到</b>（否则"解析不到 ⇒ 空集合 ⇒ 全绿"）。
    /// </summary>
    /// <remarks>
    /// 计数用精确基线：三个类型分别是 13 / 9 / 4 个构造参数。任一类型新增字段而本基线未同步，
    /// 说明"新增字段"这件事没有被显式评审（新增字段同时要进 <c>Equals</c> 与哈希）。
    /// </remarks>
    [Fact]
    public void SyntheticBaseline_ShouldSeeAllThreeValueKeysWithTheirFieldCounts()
    {
        var expected = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["CapabilityEntry"] = 13,
            ["CapabilityParameter"] = 9,
            ["ToolSchemaModel"] = 4,
        };

        foreach (var (typeName, count) in expected)
        {
            var parameters = ReadConstructorParameters(FindValueKeySource(typeName + ".cs"), typeName);

            parameters.Should().HaveCount(
                count,
                $"{typeName} 的构造参数个数与基线不符——新增/删除字段时必须同步评审 Equals 与 GetHashCode");
        }
    }

    // ────────── 断言实现 ──────────

    /// <summary>
    /// 断言指定类型的<b>全部构造参数</b>都出现在 <c>Equals</c> 与 <c>GetHashCode</c> 的方法体里。
    /// </summary>
    private static void AssertCtorFieldsParticipate(string typeName, string typeFileName)
    {
        var source = FindValueKeySource(typeFileName);

        var ctorParameters = ReadConstructorParameters(source, typeName);
        ctorParameters.Should().NotBeEmpty(
            $"未能从 {typeFileName} 解析出 {typeName} 的构造参数——解析规则已失效（假绿），请先修守卫");

        var equalsBody = ReadMethodBody(source, $"public bool Equals({typeName}? other)");
        var hashBody = ReadMethodBody(source, "public override int GetHashCode()");

        equalsBody.Should().NotBeNullOrEmpty($"{typeName}.Equals 方法体未找到（守卫解析失效）");
        hashBody.Should().NotBeNullOrEmpty($"{typeName}.GetHashCode 方法体未找到（守卫解析失效）");

        // 集合字段在 Equals/哈希里走 *Equal / Count + foreach 辅助方法，形如 "ParametersEqual(Parameters, …)"，
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
        // 形如：public CapabilityEntry(\n  string interfaceName,\n  … )\n    {
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
    /// <b>必须同时支持表达式体成员</b>：<c>ToolSchemaModel.Equals</c> 与 <c>CapabilityParameter.Equals</c>
    /// 是 <c>public bool Equals(X? other) => …;</c> 形态，<b>没有花括号</b>。若一律去找下一个 <c>{</c>，
    /// 会把<b>后面另一个方法（如 <c>GetHashCode</c>）的体</b>当成本方法的体 ⇒ 漏字段时守卫仍然全绿（假绿）。
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

    /// <summary>
    /// 定位值键源码。用 <c>SearchOption.AllDirectories</c> + 唯一性断言而非固定层级路径：
    /// 目录调整时守卫会<b>红</b>（"多份副本"或"零份"都让比对对象不明），而不是静默读到错误副本或空串。
    /// </summary>
    private static string FindValueKeySource(string fileName)
    {
        var root = TestRepoRoot.PathOf(ValueKeyDirectory);
        var files = Directory.GetFiles(root, fileName, SearchOption.AllDirectories);

        files.Should().HaveCount(
            1,
            $"{fileName} 应在 {string.Join('/', ValueKeyDirectory)} 下唯一存在（实际 {files.Length} 个）——多份会让守卫读到错误副本");

        return File.ReadAllText(files[0]);
    }
}