// -----------------------------------------------------------------------
//  作者：Mud Studio  版权所有 (c) Mud Studio 2026
//  Mud.Feishu 项目的版权、商标、专利和其他相关权利均受相应法律法规的保护。使用本项目应遵守相关法律法规和许可证的要求。
//  本项目主要遵循 MIT 许可证进行分发和使用。许可证位于源代码树根目录中的 LICENSE-MIT 文件。
//  不得利用本项目从事危害国家安全、扰乱社会秩序、侵犯他人合法权益等法律法规禁止的活动！任何基于本项目开发而产生的一切法律纠纷和责任，我们不承担任何责任！
// -----------------------------------------------------------------------

using System.Text.RegularExpressions;

namespace Mud.Feishu.AI.Tools.Tests.ContractGuards;

/// <summary>
/// 执行器骨架收敛守卫（WP3 / R-C 根因）：机械断言"机械骨架只存在于
/// <see cref="ToolExecutor"/> 一处"，防止扩域时样板回潮。
/// </summary>
/// <remarks>
/// <para>
/// 三条断言（源码扫描体例，与本目录既有守卫一致）：
/// ① <c>if (!outcome.Ok)</c> 在执行器目录（除 ToolExecutor 自身）计数 = 0——解包/回填骨架收拢于
/// <c>FromApi*</c>/<c>FromPlainText</c>；
/// ② <c>catch (ArgumentException</c> 计数 = 0——参数校验失败回填收拢于 <c>RunAsync</c>；
/// ③ 每个执行器方法内 <c>FeishuToolNames.X</c> 出现 ≤ 1 次（工具名经
/// <c>new ToolExecutor(FeishuToolNames.X, …)</c> 绑定一次，体内用 <c>executor.ToolName</c>）。
/// </para>
/// <para>
/// <b>有意保留</b>（不在断言范围）：WriteTools 的两处 <c>catch (JsonException)</c> 是
/// 裸 JSON 参数的<b>校验逻辑</b>（转带修复指引的 ArgumentException），属策展面而非骨架。
/// </para>
/// </remarks>
public class ToolExecutorSkeletonGuards
{
    private const string ExecutorDirectory = "Mud.Feishu.AI.Tools/Internal";

    /// <summary>解包/回填骨架不得回潮到具体执行器。</summary>
    [Fact]
    public void OutcomeUnwrapSkeleton_ShouldLiveOnlyInToolExecutor()
    {
        var violations = ExecutorSources()
            .Where(kv => Regex.IsMatch(kv.Value, @"if\s*\(\s*!outcome\.Ok\s*\)"))
            .Select(static kv => kv.Key)
            .ToArray();

        violations.Should().BeEmpty(
            "以下执行器出现了 if (!outcome.Ok) 手写骨架——解包/回填/截断应经 ToolExecutor.FromApi*/FromPlainText（WP3 骨架收敛）：{0}",
            string.Join(", ", violations));
    }

    /// <summary>参数校验失败回填骨架不得回潮到具体执行器。</summary>
    [Fact]
    public void ArgumentValidationCatch_ShouldLiveOnlyInToolExecutor()
    {
        var violations = ExecutorSources()
            .Where(kv => kv.Value.Contains("catch (ArgumentException", StringComparison.Ordinal))
            .Select(static kv => kv.Key)
            .ToArray();

        violations.Should().BeEmpty(
            "以下执行器出现了 catch (ArgumentException) 手写骨架——参数校验失败回填应经 ToolExecutor.RunAsync（WP3 骨架收敛）：{0}",
            string.Join(", ", violations));
    }

    /// <summary>同一工具名在单个执行器方法内最多出现 1 次（其余经 executor.ToolName）。</summary>
    [Fact]
    public void ToolName_ShouldAppearAtMostOncePerExecutorMethod()
    {
        var violations = new List<string>();
        foreach (var (fileName, source) in ExecutorSources())
        {
            var currentMethod = string.Empty;
            var count = 0;
            foreach (var line in File.ReadLines(fileName))
            {
                var methodMatch = Regex.Match(line, @"public\s+(?:async\s+)?Task<FeishuToolResult>\s+(\w+)");
                if (methodMatch.Success)
                {
                    if (count > 1)
                    {
                        violations.Add($"{Path.GetFileName(fileName)}::{currentMethod}（{count} 处）");
                    }

                    currentMethod = methodMatch.Groups[1].Value;
                    count = 0;
                }

                if (line.Contains("FeishuToolNames.", StringComparison.Ordinal))
                {
                    count++;
                }
            }

            if (count > 1)
            {
                violations.Add($"{Path.GetFileName(fileName)}::{currentMethod}（{count} 处）");
            }
        }

        violations.Should().BeEmpty(
            "以下执行器方法内 FeishuToolNames.X 出现超过 1 次——工具名应经 new ToolExecutor(FeishuToolNames.X, …) 绑定一次，体内用 executor.ToolName（防漂移面）：{0}",
            string.Join(", ", violations));
    }

    /// <summary>读取执行器目录全部源码（排除 ToolExecutor 自身与 obj/bin 产物）。</summary>
    private static Dictionary<string, string> ExecutorSources()
    {
        var directory = Path.Combine(FindRepositoryRoot(), ExecutorDirectory.Replace('/', Path.DirectorySeparatorChar));
        return Directory.GetFiles(directory, "*Tools.cs", SearchOption.TopDirectoryOnly)
            .Where(static path => !path.EndsWith("ToolExecutor.cs", StringComparison.Ordinal))
            .ToDictionary(static p => p, File.ReadAllText, StringComparer.Ordinal);
    }

    // ────────── R5 / B-3 + B-5：回填口径守卫（合并为一条，避免"守卫套守卫"）──

    /// <summary>
    /// <b>R5 / B-3</b>：消息正文字段（<c>content</c> / <c>body</c>）回填时<b>必须</b>经
    /// <c>ToolResultText.Truncate</c> 预截断。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>缺陷背景（B-3）</b>：<c>ImTools.ProjectHistory</c> 的 <c>content</c> 经
    /// <c>PageSizes.MessagePreviewLength</c> 预截断，而 <c>ProjectContent</c> 的 <c>body</c>
    /// 直接回填 —— 同一工具面内两个同类字段口径不一致，超长消息体会挤占上下文。
    /// 已修复（本项DoD 的"修复面"），本守卫锁住"修复不回潮"。
    /// </para>
    /// <para>
    /// <b>为什么是扫描式守卫</b>：截断是<b>逐调用点手写</b>的，没有可派生的类型系统约束；
    /// 扫描"消息正文类字段的直接回填"是唯一能在新增投影时立刻报红的手段（与本目录既有体例一致）。
    /// </para>
    /// </remarks>
    [Fact]
    public void MessageBodyBackfill_ShouldAlwaysBeTruncated()
    {
        var violations = new List<string>();
        foreach (var (fileName, source) in ExecutorSources())
        {
            // 形如 ["content"] = <expr>;  或 ["content"] = ToolResultText.Truncate(...);
            // 只看"消息正文语义"的键：content / body（不含 display_info 等其它域字段）。
            var matches = Regex.Matches(
                source,
                @"\[\s*""(?<key>content|body)""\s*\]\s*=\s*(?<expr>[^;,]+)",
                RegexOptions.None,
                TimeSpan.FromSeconds(5));

            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                var expr = match.Groups["expr"].Value.Trim();
                if (!expr.Contains("ToolResultText.Truncate", StringComparison.Ordinal))
                {
                    violations.Add(
                        $"{Path.GetFileName(fileName)}: [\"{match.Groups["key"].Value}\"] = {expr}（未预截断）");
                }
            }
        }

        violations.Should().BeEmpty(
            "消息正文字段回填未预截断——content/body 必须经 ToolResultText.Truncate(…, PageSizes.MessagePreviewLength)，"
            + "否则超长消息体挤占上下文（R5 / B-3）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// <b>R5 / B-5</b>：<c>ContactTools</c> 内传给 SDK 的 <c>user_id_type</c> 必须引用域级常量
    /// <c>DefaultUserIdType</c>（或由它赋值的局部变量），<b>不得出现裸字面量</b>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>缺陷背景（B-5）</b>：参数是否暴露 <c>user_id_type</c> 原按"SDK 接口有这个 query 参数"决定，
    /// 而非按"这个工具有多种合理 id 形态"决定 ⇒ 同域同类工具 id 口径可能不一致。
    /// 修复面已到位（<c>resolve_user</c> 不暴露该参数——其语义是 email/mobile → id，输出恒为
    /// <c>open_id</c>，暴露即误导；其余调用点统一走 <c>DefaultUserIdType</c>）。
    /// </para>
    /// <para>
    /// <b>为什么这条守卫有长期价值</b>：口径靠"逐调用点自觉引用常量"是纯人工不变量，
    /// 新增工具时最容易退化成裸 <c>"open_id"</c> 字面量。本守卫让那种退化立刻变红。
    /// </para>
    /// </remarks>
    [Fact]
    public void ContactUserIdType_ShouldAlwaysReferenceDomainConstant()
    {
        var fileName = ExecutorSources()
            .Keys
            .Single(static path => Path.GetFileName(path).Equals("ContactTools.cs", StringComparison.Ordinal));
        var source = File.ReadAllText(fileName);

        // 收集由 DefaultUserIdType 赋值的局部变量名（允许 `var x = args.UserIdType ?? DefaultUserIdType` 形态）。
        var aliases = Regex.Matches(source, @"\bvar\s+(?<name>\w+)\s*=\s*[^;]*\bDefaultUserIdType\b")
            .Select(static m => m.Groups["name"].Value)
            .ToHashSet(StringComparer.Ordinal);

        var violations = new List<string>();
        foreach (System.Text.RegularExpressions.Match match in Regex.Matches(source, @"user_id_type:\s*(?<value>[A-Za-z_][\w]*)"))
        {
            var value = match.Groups["value"].Value;
            if (!value.Equals("DefaultUserIdType", StringComparison.Ordinal) && !aliases.Contains(value))
            {
                violations.Add($"user_id_type: {value}");
            }
        }

        // 反向自证：守卫必须真的看得到至少一个调用点，否则"扫不到"会被误当成"全绿"。
        Regex.Matches(source, @"user_id_type:\s*[A-Za-z_][\w]*").Count.Should().BeGreaterThan(
            0,
            "未在 ContactTools 中找到任何 user_id_type: 调用点——扫描正则坏了（假绿），请先修守卫");

        violations.Should().BeEmpty(
            "ContactTools 内传给 SDK 的 user_id_type 未引用域级常量 DefaultUserIdType——"
            + "同域同类工具的 id 口径必须一致（R5 / B-5）：{0}",
            string.Join(" | ", violations));
    }

    /// <summary>
    /// <b>R5 / F-4③</b>：<c>ImTools</c> 必须注入 <c>IFeishuToolContextAccessor</c>。
    /// </summary>
    /// <remarks>
    /// <para>
    /// 实施前实测：<c>Internal/ImTools.cs</c> 内 <c>IFeishuToolContextAccessor</c> <b>0 命中</b>
    /// ⇒ <c>reply_in_thread</c> 恒为 <c>args.ReplyInThread ?? false</c>，模型不显式传参就拿不到话题串。
    /// </para>
    /// <para>
    /// <b>为什么用源码扫描而非 DI 断言</b>：DI 层面"注入了但没被读过"与"没注入"行为完全一样
    /// （都是false），无法区分；而这里的失效模式恰恰是"接了线但没消费"。
    /// 扫描<b>读取点</b>（<c>.Current</c>）能同时锁住"注入了"与"真的用了"两件事。
    /// </para>
    /// </remarks>
    [Fact]
    public void ImTools_ShouldConsumeToolContext_ForAutomaticReplyInThread()
    {
        var source = File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), "Mud.Feishu.AI.Tools", "Internal", "ImTools.cs"));

        source.Should().Contain(
            "IFeishuToolContextAccessor",
            "ImTools 未注入 IFeishuToolContextAccessor ⇒ reply_in_thread 无法自动取自会话（R5 / F-4）");

        source.Should().Contain(
            ".Current?.ThreadId",
            "ImTools 注入了 accessor 却没有**读取** ThreadId ⇒ 又一次『接了线没消费』的静默失效");

        // 反向自证：不能因为"匹配不到任何东西"而全绿。
        Regex.Matches(source, @"\.Current\?\.ThreadId").Count.Should().BeGreaterThan(
            0, "ThreadId 读取点已消失——本守卫的正则会先于行为失效（假绿）");
    }

    /// <summary>
    /// <b>禁止"无预算的执行器 + 截断版出口"组合</b>（R5 实施期发现的真实缺陷类别）。
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>new ToolExecutor(name)</c>（单参）把上限置为 <b>0</b>；而 <c>FromApi(...)</c>
    /// 是<b>截断版</b>出口，内部 <c>Truncate(text, 0)</c> 会把 <c>maxLength &lt; 1</c>
    /// 钳到 <b>1</b> ⇒ 工具结果被截成 <b>1 个字符</b>。
    /// </para>
    /// <para>
    /// <b>这是本轮实测抓到的真实 bug（不是假想）</b>：<c>docx.import_markdown</c>
    /// 与 <c>calendar.add_event_attendees</c> 两处正是该组合，结果实为 1 字符。
    /// 二者已修（前者改走 <c>FromApiUntruncated</c>，后者补 <c>_maxResultLength</c>）。
    /// </para>
    /// <para>
    /// 组合本身编译期完全合法、测试若不断言内容也照样绿 ⇒ <b>只能靠守卫</b>。
    /// </para>
    /// </remarks>
    [Fact]
    public void ToolExecutor_WithoutBudget_ShouldNotUseTheTruncatingExit()
    {
        var violations = new List<string>();
        var scanned = 0;

        var directory = Path.Combine(
            FindRepositoryRoot(), ExecutorDirectory.Replace('/', Path.DirectorySeparatorChar));

        foreach (var file in Directory.GetFiles(directory, "*Tools.cs", SearchOption.TopDirectoryOnly))
        {
            var lines = File.ReadAllLines(file);
            for (var i = 0; i < lines.Length; i++)
            {
                var match = Regex.Match(lines[i], @"new ToolExecutor\((?<args>[^)]*)\)");
                if (!match.Success || match.Groups["args"].Value.Contains(','))
                {
                    continue; // 只关心单参（无预算）构造
                }

                scanned++;

                // 窗口必须**限定在本方法体内**：到下个 [FeishuToolHandler] 或下个
                // new ToolExecutor( 为止。若用固定行数，会跨到下一个方法而误判
                // （实测：delete_blocks 的窗口曾"看到"了 import_markdown 的出口）。
                var end = i + 1;
                while (end < lines.Length
                    && !lines[end].Contains("[FeishuToolHandler(", StringComparison.Ordinal)
                    && !lines[end].Contains("new ToolExecutor(", StringComparison.Ordinal))
                {
                    end++;
                }

                var window = string.Join("\n", lines.Skip(i + 1).Take(end - i - 1));
                if (Regex.IsMatch(window, @"(?<!Untruncated)FromApi\("))
                {
                    violations.Add(
                        $"{Path.GetFileName(file)}:{i + 1} —— 单参 ToolExecutor 配 FromApi(截断版)，"
                        + "结果会被截成 1 个字符；请改走 FromApiUntruncated 或传入 _maxResultLength");
                }
            }
        }

        scanned.Should().BeGreaterThan(0, "未扫到任何 ToolExecutor 构造——扫描正则坏了（假绿），请先修守卫");

        violations.Should().BeEmpty(
            "存在『无预算执行器 + 截断版出口』组合（结果会被截成 1 字符）：{0}",
            string.Join(" | ", violations));
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
