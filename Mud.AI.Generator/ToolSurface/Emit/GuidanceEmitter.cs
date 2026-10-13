// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Threading;
using Mud.AI.ToolSurface.Schema;

namespace Mud.AI.ToolSurface.Emit;

/// <summary>
/// 域级 guidance 资产发射器（<b>WP6 / AT-F09</b>）：把 <c>{GuidanceDirectory}{domain}.md</c>（AdditionalFiles）
/// 编译期固化为 <c>{P}Guidance.g.cs</c>。
/// </summary>
/// <remarks>
/// <para>
/// 移植自上游 <c>Mud.Feishu.AI.Tools.Emit.GuidanceEmitter</c>（设计文档 §5.1 第 6 条输出路径）。泛化点：
/// <list type="bullet">
/// <item>目录片段 <c>"/Guidance/"</c> → <see cref="SdkToolProfileModel.GuidanceDirectory"/> 槽；</item>
/// <item>owner 门槛 → <see cref="SdkToolProfileModel.OwnerAssembly"/>（与 Names/Contracts/Args 同款）；</item>
/// <item>产物命名空间 → <see cref="SdkToolProfileModel.GeneratedNamespace"/>，
/// 类名 / hintName → <c>{P}Guidance</c>。</item>
/// </list>
/// </para>
/// <para>
/// <b>为什么用 AdditionalFiles 而不是源码内 XML 文档（R4.1 评审 R-8）</b>：生成器工程受
/// <c>netstandard2.0</c> 约束，解析 XML doc 注释需要手写解析器（复杂且脆）；AdditionalFiles 的扫描、
/// 读取、比对全部复用既有管线（golden 快照即先例）。
/// </para>
/// <para>
/// <b>与工具面同源</b>：域 guidance 与该域工具同 repo、同一 pass 产出，且装配时只注入
/// <b>已启用工具</b>所属域的 guidance——因此不可能出现"md 说的和工具面不一致"。
/// 发射门槛与 <c>{P}Names</c> / <c>{P}Contracts</c> 一致（仅工具面实现包），
/// 否则其他声明工具特性样例接口的工程会多出一个无人消费的静态类。
/// </para>
/// <para>
/// <b>不新增诊断</b>：域 guidance 与工具面的一致性由测试守卫锁定——
/// 新增诊断必须同批补 driver 负例（WP1 的元守卫），而本能力的失败模式（多写一个 md 文件）不值得
/// 引入一条零容忍诊断。资产键冲突沿用上游"抛异常令构建失败"的取舍（由入口 Guard 兜底为槽位 026）。
/// </para>
/// </remarks>
internal static class GuidanceEmitter
{
    /// <summary>
    /// 判断 AdditionalFile 是否为该剖面的域 guidance 资产（正斜杠归一后按目录片段匹配）。
    /// </summary>
    /// <remarks>
    /// <see cref="SdkToolProfileModel.GuidanceDirectory"/> 是<b>非必填槽</b>：未声明（空串）时本方法恒为
    /// <see langword="false"/>。否则 <c>IndexOf("")</c> 恒为 0，任何 <c>.md</c> AdditionalFile
    /// （README、CHANGELOG、docs 下的任意片段）都会被吸收成"域名资产"，
    /// 进而在 <c>{P}Guidance</c> 产物里生成以路径前缀为键的垃圾条目。
    /// </remarks>
    public static bool IsGuidanceFile(string path, SdkToolProfileModel profile)
    {
        var directory = profile.GuidanceDirectory;
        if (directory.Length == 0)
        {
            return false;
        }

        var normalized = path.Replace('\\', '/');
        return normalized.EndsWith(".md", StringComparison.OrdinalIgnoreCase)
            && normalized.IndexOf(directory, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    /// <summary>发射域 guidance 表（无资产或非工具面实现包时静默跳过）。</summary>
    public static void Emit(
        SourceProductionContext context,
        SdkToolProfileModel profile,
        string? assemblyName,
        ImmutableArray<AdditionalText> files,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!string.Equals(assemblyName, profile.OwnerAssembly, StringComparison.Ordinal))
            {
                return;
            }

            // L1 = {dir}/{domain}.md（无子目录）；L2 = {dir}/{domain}/{topic}.md（子目录）。
            // 键：L1 用 "{domain}"，L2 用 "{domain}/{topic}"。
            // 用 SortedDictionary 保证产物确定（否则 golden 式的逐字节可比性无从谈起）。
            var blocks = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var references = new SortedDictionary<string, string>(StringComparer.Ordinal);
            var owners = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var file in files.OrderBy(static f => f.Path, StringComparer.Ordinal))
            {
                var key = AssetKeyOf(file.Path, profile);
                var text = file.GetText(cancellationToken)?.ToString();
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(text))
                {
                    continue;
                }

                // ⚠️ **冲突必须响亮失败，绝不静默覆盖**（R5 / F-9，评审点②）。
                // 旧实现是 blocks[domain] = text：两个文件映射到同一域时后者直接覆盖前者，
                // **没有任何报错**——而 guidance 是模型的行为依据，覆盖即等于"某域的避坑文本静默消失"。
                // 之所以用抛异常而非新增槽位诊断：新增零容忍诊断需同步描述符表 / 上报点守卫 /
                // 零容忍清单多处；此处抛异常已能让构建失败（入口 Guard 兜底为槽位 026），且消息更直接。
                if (owners.TryGetValue(key!, out var previousOwner))
                {
                    throw new InvalidOperationException(
                        $"guidance 资产键冲突：'{key}' 同时由 '{previousOwner}' 与 '{file.Path}' 提供。"
                        + "L1 键为 {domain}（{GuidanceDirectory}{domain}.md），L2 键为 {domain}/{topic}"
                        + "（{GuidanceDirectory}{domain}/{topic}.md）。请重命名其中一份——静默覆盖会让该域的"
                        + "行为依据凭空消失。");
                }

                owners[key!] = file.Path;

                // 显式窄化到独立非空局部变量（上游 R2-04 纪律）：`key!` 的后缀断言不随
                // 分支流动，直接 `references[key]` 会触发 CS8604。
                var assetKey = key!;
                var assetText = text!.Trim();
                if (assetKey.Contains('/'))
                {
                    references[assetKey] = assetText;
                }
                else
                {
                    blocks[assetKey] = assetText;
                }
            }

            // 只有 L2 资产（{dir}/{domain}/{topic}.md）而没有 L1 域文件时也必须产出：
            // References / ReferenceKeys 是 guidance_read 元工具的唯一数据源，
            // 「无 L1 即整体丢弃」会让 L2 素材静默消失（与 F-9 修掉的静默覆盖同类）。
            if (blocks.Count == 0 && references.Count == 0)
            {
                return;
            }

            // CA1308 误报豁免（对齐本仓 QueryParameterBinder 先例）：SDK 名的小写形态只进
            // 产物注释文本（元工具名提示），不参与任何比较/哈希，InvariantCulture 风险不存在。
#pragma warning disable CA1308
            var guidanceToolName = profile.Name.ToLowerInvariant() + ".guidance_read";
#pragma warning restore CA1308
            var source = new StringBuilder();
            source.Line($"// <auto-generated> 由 {GeneratedCodeMarker.GeneratorName(profile)} 编译期产出，禁止手工修改 </auto-generated>");
            source.Line("#nullable enable");
            source.Line("#pragma warning disable CS1591 // 生成代码不逐一补 XML 注释");
            source.Line($"namespace {profile.GeneratedNamespace}");
            source.Line("{");
            source.Line("    /// <summary>");
            source.Line("    /// 域级 guidance 资产（WP6 / AT-F09）：域 = 工具名首个 '.' 之前的部分；");
            source.Line($"    /// 素材为 {profile.GuidanceDirectory.Trim('/')}/{{domain}}.md（生成器 AdditionalFiles），内容与该域工具同源同 pass。");
            source.Line("    /// </summary>");
            source.Line($"    {GeneratedCodeMarker.Attribute(profile)}");
            source.Line($"    public static class {profile.ProductPrefix}Guidance");
            source.Line("    {");
            source.Line("        /// <summary>域 → guidance 正文（装配时只注入已启用工具所属域，见宿主 GuidanceComposer）。</summary>");
            source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
            source.Line("        public static System.Collections.Generic.IReadOnlyDictionary<string, string> ByDomain { get; } =");
            source.Line("            new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal)");
            source.Line("            {");
            foreach (var block in blocks)
            {
                source.Line($"                [{JsonText.ToCSharpLiteral(block.Key)}] = {JsonText.ToCSharpLiteral(block.Value)},");
            }

            source.Line("            };");

            // L2：按需读取的深层文本（guidance_read 元工具的唯一数据源）。
            source.Line();
            source.Line($"        /// <summary>L2 references：键为 \"{{domain}}/{{topic}}\"，供 {guidanceToolName} 按需读取。</summary>");
            source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
            source.Line("        public static System.Collections.Generic.IReadOnlyDictionary<string, string> References { get; } =");
            source.Line("            new System.Collections.Generic.Dictionary<string, string>(System.StringComparer.Ordinal)");
            source.Line("            {");
            foreach (var reference in references)
            {
                source.Line($"                [{JsonText.ToCSharpLiteral(reference.Key)}] = {JsonText.ToCSharpLiteral(reference.Value)},");
            }

            source.Line("            };");
            source.Line();
            source.Line("        /// <summary>可读的 L2 资产清单（形如 \"{domain}/{topic}\"），用于错误提示与守卫。</summary>");
            source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
            source.Line("        public static string[] ReferenceKeys { get; } = new string[]");
            source.Line("        {");
            foreach (var reference in references)
            {
                source.Line($"            {JsonText.ToCSharpLiteral(reference.Key)},");
            }

            source.Line("        };");
            source.Line();
            source.Line("        /// <summary>按键取 L2 文本；不存在返回 false（不抛异常，调用方负责给可执行提示）。</summary>");
            source.Line($"        {GeneratedCodeMarker.Attribute(profile)}");
            source.Line("        public static bool TryGetReference(string key, out string text)");
            source.Line("            => References.TryGetValue(key, out text!);");
            source.Line("    }");
            source.Line("}");

            TransitiveCodeGenerator.AddSourceValidated(context, $"{profile.ProductPrefix}Guidance.g.cs", source.ToString());
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // 资产键冲突（InvalidOperationException）是**有意的构建失败**（上游行为）：
            // 放行给入口 Guard 转槽位 026，不得在此吞掉——静默跳过等于"某域行为依据凭空消失"复发。
            GeneratorDebugLogger.LogError(nameof(GuidanceEmitter), ex);
        }
    }

    /// <summary>
    /// 从资产路径取<b>资产键</b>（R5 / F-9）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 旧实现只取<b>文件名</b>（<c>Guidance/bitable.md</c> → <c>"bitable"</c>），因此一旦引入
    /// 子目录 <c>Guidance/im/replies.md</c>，键会变成 <c>"replies"</c>——与真正的域名混在一起，
    /// 且不同域下的同名 topic 会<b>静默互相覆盖</b>。
    /// </para>
    /// <para>
    /// 现改为：取剖面 guidance 目录（<see cref="SdkToolProfileModel.GuidanceDirectory"/>）之后的
    /// 相对路径去扩展名——无子目录 ⇒ L1 键 <c>{domain}</c>（保持既有键不变，向后兼容）；
    /// 有子目录 ⇒ L2 键 <c>{domain}/{topic}</c>。
    /// </para>
    /// <para>
    /// <b>去扩展名必须换算到 relative 的下标</b>：<c>dot</c> 是相对 <c>tail</c>（末段文件名）的下标，
    /// 直接 <c>relative.Substring(0, dot)</c> 会在有子目录时截错位置
    /// （<c>bitable/faq.md</c> ⇒ <c>"bit"</c>，既污染 L1 的 ByDomain 又丢掉真正的 L2 引用）。
    /// 正确下标为 <c>relative.Length - tail.Length + dot</c>。
    /// </para>
    /// </remarks>
    private static string? AssetKeyOf(string path, SdkToolProfileModel profile)
    {
        var directory = profile.GuidanceDirectory;
        if (directory.Length == 0)
        {
            return null;
        }

        var normalized = path.Replace('\\', '/');
        var root = directory.Trim('/') + "/";
        var start = normalized.IndexOf(root, StringComparison.OrdinalIgnoreCase);
        var relative = start >= 0
            ? normalized.Substring(start + root.Length)
            : normalized;

        var lastSlash = relative.LastIndexOf('/');
        var tail = lastSlash >= 0 ? relative.Substring(lastSlash + 1) : relative;
        var dot = tail.LastIndexOf('.');
        if (dot <= 0)
        {
            return null;
        }

        var key = relative.Substring(0, relative.Length - tail.Length + dot);
        return key.Length == 0 ? null : key;
    }
}